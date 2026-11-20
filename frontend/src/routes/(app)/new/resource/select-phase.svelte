<script lang="ts">
	import { api } from '#lib/api.js';
	import Button from '#lib/components/ui/button/button.svelte';
	import Input from '#lib/components/ui/input/input.svelte';
	import Label from '#lib/components/ui/label/label.svelte';
	import { hashFile, uploadFile } from '#lib/upload.js';
	import { toast } from 'svelte-sonner';
	import FileDropzone from '#lib/components/ui/file-dropzone.svelte';

	type Mode = 'file' | 'url';
	type SelectResult = {
		mode: Mode;
		url: string;
		jobId: string;
		fileId: string | null;
		fileHash: string | null;
		fileExtension: string;
	};

	let {
		oncomplete,
		onduplicate
	}: {
		oncomplete: (result: SelectResult) => void;
		onduplicate: (id: string) => void;
	} = $props();

	let mode = $state<Mode>('file');
	let selectedFile = $state<File | null>(null);
	let supportedExtensions = $state<string[]>([]);
	let url = $state('');
	let uploadStep = $state<'hashing' | 'uploading' | 'starting' | null>(null);
	let isStarting = $state(false);

	const canProceed = $derived(mode === 'file' ? selectedFile !== null : url.trim().length > 0);

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
				const dup = await api.get<string | null>(
					`/api/resources/exists?hash=${encodeURIComponent(hash)}`
				);

				if (dup) {
					onduplicate(dup);
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
				const dup = await api.get<string | null>(
					`/api/resources/exists?url=${encodeURIComponent(url)}`
				);

				if (dup) {
					onduplicate(dup);
					return;
				}

				extractType = 'web';
				extractValue = url;
			}

			uploadStep = 'starting';
			const result = await api.post<{ jobId: string }>(
				`/api/ai/extract-metadata/start?type=${extractType}&value=${encodeURIComponent(extractValue)}`
			);

			oncomplete({ mode, url, jobId: result.jobId, fileId, fileHash, fileExtension });
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Something went wrong');
		} finally {
			isStarting = false;
			uploadStep = null;
		}
	}

	api
		.get<{ document: string[] }>('/api/library/supported-extensions')
		.then((r) => (supportedExtensions = r.document))
		.catch(() => {});
</script>

<!-- Mode toggle -->
<div class="flex rounded-lg bg-muted p-1">
	<button
		type="button"
		onclick={() => (mode = 'file')}
		class="transistion-colors flex-1 cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium {mode ===
		'file'
			? 'bg-backround shadow-sm'
			: 'text-muted-foreground hover:text-foreground'}"
	>
		File
	</button>
	<button
		type="button"
		onclick={() => (mode = 'url')}
		class="transistion-colors flex-1 cursor-pointer rounded-md px-3 py-1.5 text-sm font-medium {mode ===
		'url'
			? 'bg-backround shadow-sm'
			: 'text-muted-foreground hover:text-foreground'}"
	>
		URL
	</button>
</div>

{#if mode === 'file'}
	<!-- Drop zone -->
	<FileDropzone bind:file={selectedFile} extensions={supportedExtensions} />
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
