<script lang="ts">
	import { api } from '$lib/api';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import type { ExtractedMetadata } from '$lib/types/resource';

	let {
		jobId,
		oncomplete,
		onskip
	}: {
		jobId: string;
		oncomplete: (metadata: ExtractedMetadata | null) => void;
		onskip: () => void;
	} = $props();

	let statusMessage = $state('');
	let progressPercentage = $state(0);
	let processingError = $state<string | null>(null);
	let errorCount = 0;

	$effect(() => {
		const interval = setInterval(async () => {
			try {
				const res = await api.get<{
					status: string;
					statusMessage: string;
					progressPercentage: number;
					result: ExtractedMetadata | null;
					errorMessage: string | null;
				}>(`/api/ai/extract-metadata/status/${jobId}`);

				errorCount = 0;
				statusMessage = res.statusMessage;
				progressPercentage = res.progressPercentage;

				if (res.status === 'Completed') {
					clearInterval(interval);
					oncomplete(res.result);
				} else if (res.status === 'Failed') {
					processingError = res.errorMessage ?? 'Processing failed';
					clearInterval(interval);
				}
			} catch {
				errorCount++;
				if (errorCount >= 3) {
					processingError = 'Lost connection to server';
					clearInterval(interval);
				}
			}
		}, 2000);

		return () => clearInterval(interval);
	});
</script>

<div class="flex flex-col items-center gap-4 py-8">
	{#if processingError}
		<div
			class="rounded-lg border border-destructive/50 bg-destructive/10 px-4 py-3 text-sm text-destructive"
		>
			{processingError}
		</div>
	{:else}
		<Spinner class="h-8 w-8" />
		<p class="text-sm font-medium">{statusMessage || 'Processing...'}</p>
		<p class="text-xs text-muted-foreground">{progressPercentage}%</p>
	{/if}
</div>

<div class="flex justify-center gap-5">
	{#if processingError}
		<Button class="cursor-pointer" onclick={() => location.reload()}>Go back</Button>
	{/if}

	<Button variant="ghost" class="cursor-pointer" onclick={onskip}>Skip to manual entry</Button>
</div>
