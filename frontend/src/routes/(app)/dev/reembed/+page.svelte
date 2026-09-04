<script lang="ts">
	import { api } from '$lib/api';
	import { getContext, onMount } from 'svelte';
	import type { HubConnection } from '@microsoft/signalr';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import { Button } from '$lib/components/ui/button';
	import Checkbox from '$lib/components/ui/checkbox/checkbox.svelte';
	import { RefreshCw } from '@lucide/svelte';
	import { toast } from 'svelte-sonner';

	type StatusSummary = {
		isRunning: boolean;
		resourceCounts: Record<string, number>;
		entityCounts: Record<string, number>;
		incompleteItems: { id: string; name: string; type: string; status: string; error: string | null }[];
	};

	let summary = $state<StatusSummary | null>(null);
	let loading = $state(true);
	let submitting = $state(false);
	let includeResources = $state(true);
	let includeEntities = $state(true);

	let busy = $derived(submitting || summary?.isRunning === true);

	const STATUS_ORDER = ['Pending', 'Processing', 'Completed', 'Failed'];

	let scopedIncompleteCount = $derived(
		(summary?.incompleteItems ?? []).filter((item) =>
			item.type === 'resource' ? includeResources : includeEntities
		).length
	);

	async function fetchStatus() {
		try {
			summary = await api.get<StatusSummary>('/api/admin/reembed/status');
		} finally {
			loading = false;
		}
	}

	async function reembed(onlyIncomplete: boolean) {
		submitting = true;
		try {
			await api.post(
				`/api/admin/reembed?onlyIncomplete=${onlyIncomplete}&includeResources=${includeResources}&includeEntities=${includeEntities}`
			);
			toast.success(onlyIncomplete ? 'Retrying incomplete items...' : 'Re-embedding everything...');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to start re-embed.');
		} finally {
			submitting = false;
			await fetchStatus();
		}
	}

	async function cancelReembed() {
		submitting = true;
		try {
			await api.post('/api/admin/reembed/cancel');
			toast.success('Cancelling...');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to cancel.');
		} finally {
			submitting = false;
			await fetchStatus();
		}
	}

	let refreshTimer: ReturnType<typeof setTimeout> | null = null;
	function scheduleRefresh() {
		if (refreshTimer) clearTimeout(refreshTimer);
		refreshTimer = setTimeout(fetchStatus, 1000);
	}

	const chat = getContext<{ connection: HubConnection | null }>('chatConnection');

	$effect(() => {
		const conn = chat.connection;
		if (!conn) return;

		conn.on('EmbeddingStatusChanged', scheduleRefresh);
		return () => conn.off('EmbeddingStatusChanged', scheduleRefresh);
	});

	onMount(fetchStatus);
</script>

<div class="mx-auto flex h-full max-w-4xl flex-col gap-6 overflow-y-auto p-8">
	<div class="flex items-center justify-between">
		<h1 class="text-lg font-semibold">Embedding Status</h1>
		<button
			onclick={fetchStatus}
			class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
			title="Refresh"
		>
			<RefreshCw size={16} />
		</button>
	</div>

	{#if loading}
		<div class="flex h-full items-center justify-center">
			<Spinner class="h-8 w-8" />
		</div>
	{:else if summary}
		<!-- Counts -->
		<div class="grid grid-cols-2 gap-4">
			<div class="flex flex-col gap-2 rounded-md border p-4">
				<span class="text-sm font-medium text-muted-foreground">Resources</span>
				<div class="flex flex-col gap-1">
					{#each STATUS_ORDER as status (status)}
						<div class="flex items-center justify-between text-sm">
							<span class={status === 'Failed' ? 'text-destructive' : ''}>{status}</span>
							<span class="font-mono">{summary.resourceCounts[status] ?? 0}</span>
						</div>
					{/each}
				</div>
			</div>

			<div class="flex flex-col gap-2 rounded-md border p-4">
				<span class="text-sm font-medium text-muted-foreground">Persons + Organisations</span>
				<div class="flex flex-col gap-1">
					{#each STATUS_ORDER as status (status)}
						<div class="flex items-center justify-between text-sm">
							<span class={status === 'Failed' ? 'text-destructive' : ''}>{status}</span>
							<span class="font-mono">{summary.entityCounts[status] ?? 0}</span>
						</div>
					{/each}
				</div>
			</div>
		</div>

		<!-- Scope -->
		<div class="flex items-center gap-4">
			<label class="flex items-center gap-2 text-sm {busy ? '' : 'cursor-pointer'}">
				<Checkbox bind:checked={includeResources} disabled={busy} class="cursor-pointer" />
				Resources
			</label>
			<label class="flex items-center gap-2 text-sm {busy ? '' : 'cursor-pointer'}">
				<Checkbox bind:checked={includeEntities} disabled={busy} class="cursor-pointer" />
				Persons + Organisations
			</label>
		</div>

		<!-- Actions -->
		<div class="flex gap-2">
			{#if summary.isRunning}
				<Button variant="destructive" onclick={cancelReembed} disabled={submitting} class="cursor-pointer">
					Cancel Re-embed
				</Button>
			{:else}
				<Button
					variant="outline"
					onclick={() => reembed(false)}
					disabled={busy || (!includeResources && !includeEntities)}
					class="cursor-pointer"
				>
					Re-embed All
				</Button>
				<Button onclick={() => reembed(true)} disabled={busy || scopedIncompleteCount === 0} class="cursor-pointer">
					Retry Incomplete Only ({scopedIncompleteCount})
				</Button>
			{/if}
		</div>

		<!-- Incomplete items -->
		{#if summary.incompleteItems.length > 0}
			<div class="flex flex-col gap-2">
				<span class="text-sm font-medium text-muted-foreground">Incomplete items</span>
				<div class="flex flex-col divide-y rounded-md border">
					{#each summary.incompleteItems as item (item.id)}
						<div class="flex flex-col gap-1 p-3">
							<div class="flex items-center gap-2">
								<span class="rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">{item.type}</span>
								<span
									class="rounded-full px-2 py-0.5 text-xs {item.status === 'Failed'
										? 'bg-destructive/10 text-destructive'
										: 'bg-muted text-muted-foreground'}">{item.status}</span
								>
								<span class="text-sm font-medium">{item.name}</span>
							</div>
							{#if item.error}
								<span class="truncate text-xs text-destructive" title={item.error}>{item.error}</span>
							{/if}
						</div>
					{/each}
				</div>
			</div>
		{/if}
	{/if}
</div>
