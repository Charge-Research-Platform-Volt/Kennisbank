<script lang="ts">
    import { api } from '$lib/api';
    import Spinner from '$lib/components/ui/spinner/spinner.svelte';
    import Button from '$lib/components/ui/button/button.svelte';
    import type { ExtractedMetadata } from '$lib/types/resource';

    let { jobId, oncomplete, onskip }: {
        jobId: string;
        oncomplete: (metadata: ExtractedMetadata | null) => void;
        onskip: () => void;
    } = $props();

    let statusMessage = $state('');
    let progressPercentage = $state(0);
    let processingError = $state<string | null>(null);

    $effect(() => {
        const interval = setInterval(async () => {
            try {
                const res = await api.get<{ status: string; statusMessage: string; progressPercentage: number; result: ExtractedMetadata | null; errorMessage: string | null; }>(`/api/ai/extract-metadata/status/${jobId}`);

                statusMessage = res.body.statusMessage;
                progressPercentage = res.body.progressPercentage;

                if (res.body.status === 'Completed') {
                    clearInterval(interval);
                    oncomplete(res.body.result);
                } else if (res.body.status === 'Failed') {
                    processingError = res.body.errorMessage ?? 'Processing Failed';
                }
            } catch { /* Ignore errors */ }
        }, 2000);

        return () => clearInterval(interval);
    });
</script>

<div class="flex flex-col items-center gap-4 py-8">
    {#if processingError}
        <p class="text-destructive text-xs">{processingError}</p>
    {:else}
        <Spinner class="h-8 w-8" />
        <p class="text-sm font-medium">{statusMessage || 'Processing...'}</p>
        <p class="text-muted-foreground text-xs">{progressPercentage}%</p>
    {/if}
</div>

<div class="flex justify-center gap-5">
    {#if processingError}
        <Button class="cursor-pointer" onclick={() => location.reload()}>
            Go back
        </Button>
    {/if}

    <Button variant="ghost" class="cursor-pointer" onclick={onskip}>
        Skip to manual entry
    </Button>
</div>
