<script lang="ts">
	import { api } from '$lib/api';
	import Button from '$lib/components/ui/button/button.svelte';
	import { renderChangelog } from '$lib/changelog';

	type ChangelogEntry = { id: number; title: string; body: string; createdAt: string };

	let entries = $state<ChangelogEntry[]>([]);
	let hasMore = $state(true);
	let loading = $state(false);

	async function loadMore() {
		if (loading || !hasMore) return;
		loading = true;

		const before = entries.at(-1)?.id;
		const query = before !== undefined ? `?before=${before}` : '';

		try {
			const result = await api.get<{ items: ChangelogEntry[]; hasMore: boolean }>(
				`/api/changelog${query}`
			);

			entries = [...entries, ...(result?.items ?? [])];
			hasMore = result?.hasMore ?? false;
		} finally {
			loading = false;
		}
	}

	loadMore();
</script>

<div class="mx-auto flex w-full max-w-3xl flex-col gap-4 p-8">
	<h1 class="text-lg font-semibold">Changelog</h1>

	<div class="flex flex-col">
		{#each entries as entry, i (entry.id)}
			{#if i > 0}
				<hr class="my-3" />
			{/if}

			<div class="flex flex-col gap-1.5">
				<div class="flex items-center justify-between">
					<span class="font-medium">{entry.title}</span>
					<span class="text-xs text-muted-foreground"
						>{new Date(entry.createdAt).toLocaleDateString()}</span
					>
				</div>
				<div class="prose prose-sm max-w-none text-sm text-muted-foreground">
					<!-- eslint-disable-next-line svelte/no-at-html-tags -->
					{@html renderChangelog(entry.body)}
				</div>
			</div>
		{/each}
	</div>

	{#if hasMore}
		<Button
			variant="outline"
			class="cursor-pointer self-center"
			onclick={loadMore}
			disabled={loading}
		>
			{loading ? 'Loading...' : 'Load more'}
		</Button>
	{/if}
</div>
