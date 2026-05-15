<script lang="ts">
    import { api } from '$lib/api';
    import Button from '$lib/components/ui/button/button.svelte';
    import Input from '$lib/components/ui/input/input.svelte';
    import Label from '$lib/components/ui/label/label.svelte';
    import { Upload, FileText } from '@lucide/svelte';
    import { hashFile, uploadFile } from '$lib/upload';
    import { toast } from 'svelte-sonner';

    type Mode = 'file' | 'url';
    type SelectResult = { mode: Mode; url: string; jobId: string; fileId: string | null; fileHash: string | null; fileExtension: string };

    let { oncomplete, onduplicate }: {
        oncomplete: (result: SelectResult) => void;
        onduplicate: (id: string) => void;
    } = $props();

    let mode = $state<Mode>('file');
    let selectedFile = $state<File | null>(null);
    let isDragging = $state(false);
    let supportedExtensions = $state<string[]>([]);
    let fileInput = $state<HTMLInputElement | null>(null);
    let fileError = $state<string | null>(null);
    let url = $state('');
    let uploadStep = $state<'hashing' | 'uploading' | 'starting' | null>(null);
    let isStarting = $state(false);

    const canProceed = $derived(mode === 'file' ? selectedFile !== null : url.trim().length > 0);

    function selectFile(file: File | undefined) {
        fileError = null;
        if (!file) return;
        const ext = file.name.split('.').pop()?.toLowerCase() ?? '';
        if (supportedExtensions.length > 0 && !supportedExtensions.includes(ext)) {
            fileError = 'This file is not supported.';
            return;
        }
        selectedFile = file;
    }

    async function handleNext() {
        isStarting = true;

        try {
            let extractType: 'file' | 'web';
            let extractValue: string;
            let fileId: string | null = null;
            let fileHash: string | null = null;
            let fileExtension = '';

            if (mode === 'file') {
                uploadStep = 'hashing';
                const hash = await hashFile(selectedFile!);
                const dup = await api.get<{ exists: boolean; id: string }>(
                    `/api/resources/exists?hash=${encodeURIComponent(hash)}`
                );

                if (dup.body.exists) {
                    onduplicate(dup.body.id);
                    return;
                }

                uploadStep = 'uploading';
                const objectName = await uploadFile(selectedFile!);
                fileId = objectName;
                fileHash = hash;
                fileExtension = selectedFile!.name.split('.').pop()?.toLowerCase() ?? '';
                extractValue = objectName;
                extractType = 'file';
            } else {
                const dup = await api.get<{ exists: boolean; id: string }>(
                    `/api/resources/exists?url=${encodeURIComponent(url)}`
                );

                if (dup.body.exists) {
                    onduplicate(dup.body.id);
                    return;
                }

                extractType = 'web';
                extractValue = url;
            }

            uploadStep = 'starting';
            const result = await api.post<{ jobId: string }>(
                `/api/ai/extract-metadata/start?type=${extractType}&value=${encodeURIComponent(extractValue)}`
            );

            oncomplete({ mode, url, jobId: result.body.jobId, fileId, fileHash, fileExtension });
        } catch (e) {
            toast.error(e instanceof Error ? e.message : 'Something went wrong');
        } finally {
            isStarting = false;
            uploadStep = null;
        }
    }

    api.get<{ document: string[] }>('/api/resources/supported_extensions')
        .then((r) => (supportedExtensions = r.body.document))
        .catch(() => {});
</script>

<!-- Mode toggle -->
<div class="flex rounded-lg bg-muted p-1">
    <button
        type="button"
        onclick={() => (mode = 'file')}
        class="transistion-colors flex-1 cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium {mode === 'file' ? 'bg-backround shadow-sm' : 'text-muted-foreground hover:text-foreground'}"
    >
        File
    </button>
    <button
        type="button"
        onclick={() => (mode = 'url')}
        class="transistion-colors flex-1 cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium {mode === 'url' ? 'bg-backround shadow-sm' : 'text-muted-foreground hover:text-foreground'}"
    >
        URL
    </button>
</div>

{#if mode === 'file'}
    <!-- Drop zone -->
    <div
        role="button"
        tabindex="0"
        onclick={() => fileInput?.click()}
        onkeydown={(e) => e.key === 'Enter' && fileInput?.click()}
        ondragover={(e) => { e.preventDefault(); isDragging = true; }}
        ondragleave={() => (isDragging = false)}
        ondrop={(e) => { e.preventDefault(); isDragging = false; selectFile(e.dataTransfer?.files[0]); }}
        class="flex min-h-48 cursor-pointer flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed border-border p-8 transition-colors {isDragging ? 'border-primary bg-accent' : 'hover:bg-accent/50'}"
    >
        {#if selectedFile}
            <FileText class="h-8 w-8 text-primary" />
            <p class="text-sm font-medium">{selectedFile.name}</p>
            <p class="text-xs text-muted-foreground">{(selectedFile.size / 1024 / 1024).toFixed(2)} MB</p>
            <button
                onclick={(e) => { e.stopPropagation(); selectedFile = null; }}
                class="text-xs text-muted-foreground underline hover:text-foreground">Remove</button>
        {:else}
            <Upload class="h-8 w-8 text-muted-foreground" />
            <p class="text-sm font-medium">Drop a file or click to browse</p>
            <p class="text-xs text-muted-foreground">{supportedExtensions.join(', ')}</p>
        {/if}
    </div>

    {#if fileError}
        <p class="text-xs text-destructive">{fileError}</p>
    {/if}

    <input
        bind:this={fileInput}
        type="file"
        class="hidden"
        accept={supportedExtensions.map((ext) => `.${ext}`).join(',')}
        onchange={() => selectFile(fileInput?.files?.[0])}
    />
{:else}
    <!-- URL input -->
    <div class="flex flex-col gap-1.5">
        <Label for="url">URL</Label>
        <Input id="url" type="url" bind:value={url} placeholder="https://example.com/article" />
    </div>
{/if}

<!-- Next -->
<div class="flex justify-end">
    <Button disabled={!canProceed || isStarting} onclick={handleNext} class="cursor-pointer">
        {#if uploadStep === 'hashing'}Hashing...
        {:else if uploadStep === 'uploading'}Uploading...
        {:else if uploadStep === 'starting'}Starting...
        {:else}Next{/if}
    </Button>
</div>
