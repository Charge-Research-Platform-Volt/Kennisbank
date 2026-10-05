<script lang="ts">
	import { api } from '$lib/api';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import type { ResourceItem } from '$lib/types/resource';
	import { getFileIcon } from '$lib/utils/icons';
	import { Search, ArrowRight, X } from '@lucide/svelte';
	import { onMount, getContext } from 'svelte';
	import { fade, fly } from 'svelte/transition';
	import { formatDate } from '$lib/utils/date';
	import { getParam, setParams } from '$lib/utils/urlState';
	import { debounce } from '$lib/utils/debounce';
	import { toast } from 'svelte-sonner';

	const openInspector: (item: ResourceItem) => void = getContext('openInspector');
	const debouncedSearch = debounce(search);

	let inputElement = $state<HTMLInputElement>();
	let searchBlockElement = $state<HTMLDivElement>();

	let paddingTop = $derived(
		searchBlockElement ? `calc(50vh - ${searchBlockElement.offsetHeight / 2}px)` : '50vh'
	);

	let searchInput = $state('');

	let searched = $state(false);
	let loading = $state(false);

	let hasMore = $state(false);
	let items = $state<ResourceItem[]>([]);

	async function fetchItems() {
		loading = true;

		try {
			const result = await api.post<{ items: ResourceItem[]; hasMore: boolean }>(
				'/api/library/search-content',
				{
					search: searchInput,
					pageSize: 20,
					excludeIds: items.map((i) => i.id)
				}
			);

			items = items.concat(result.items);
			hasMore = result.hasMore;
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Search failed.');
		} finally {
			loading = false;
		}
	}

	async function search(blurInput = false) {
		if (!searchInput.trim()) return;

		searched = true;
		items = [];
		hasMore = false;
		if (blurInput) inputElement?.blur();

		setParams({ q: searchInput });

		await fetchItems();
	}

	function clearSearch() {
		searchInput = '';
		items = [];
		hasMore = false;
		setParams({ q: null });
		inputElement?.focus();
	}

	function renderSnippet(raw: string) {
		const escaped = raw.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');

		return escaped.replaceAll('\u0001', '<mark>').replaceAll('\u0002', '</mark>');
	}

	onMount(() => {
		if (!searchInput) inputElement?.focus();

		if (getParam('q')) {
			searchInput = getParam('q');
			searched = true;
			search();
		}
	});
</script>

{#snippet result(item: ResourceItem)}
	{@const Icon = getFileIcon(item.fileType)}
	{@const snippet = item.chunks?.length ? item.chunks[0] : null}
	<button
		class="group flex w-full cursor-pointer items-start gap-4 px-4 py-4 text-left transition-colors hover:bg-accent/60"
		onclick={() => openInspector(item)}
	>
		<div
			class="mt-0.5 shrink-0 rounded-md bg-muted p-2 transition-colors group-hover:bg-background"
		>
			<Icon size={16} class="text-muted-foreground" />
		</div>
		<div class="flex min-w-0 flex-1 flex-col gap-1">
			<div class="flex items-start justify-between gap-4">
				<span class="text-sm leading-snug font-semibold">{item.name}</span>
				{#if item.publicationDate}
					<span class="mt-0.5 shrink-0 text-xs text-muted-foreground">
						{formatDate(item.publicationDate, item.publicationDatePrecision)}
					</span>
				{/if}
			</div>
			{#if snippet}
				<p class="mt-1 line-clamp-2 text-xs leading-relaxed text-muted-foreground">
					<!-- eslint-disable-next-line svelte/no-at-html-tags -->
					{@html renderSnippet(snippet)}
				</p>
			{/if}
		</div>
	</button>
{/snippet}

<div
	class="flex h-full flex-1 flex-col items-center overflow-hidden transition-[padding-top] duration-500 ease-in-out"
	style="padding-top: {searched ? '1rem' : paddingTop}"
>
	<div
		bind:this={searchBlockElement}
		class="flex w-full flex-col items-center gap-6 px-4 transition-[max-width] duration-500 ease-in-out {searched
			? 'max-w-5xl'
			: 'max-w-2xl'}"
	>
		{#if !searched}
			<div transition:fade={{ duration: 200 }}>
				<!-- Logo + Tagline -->
				<div class="flex flex-col items-center gap-2">
					<div class="flex items-center gap-2">
						<img src="/img/charge-icon.webp" alt="Charge Icon" class="h-8 w-8" />
						<span class="text-4xl font-medium">Grid Search</span>
					</div>
					<p class="text-base text-muted-foreground">
						Search the grid for resources, people, and organisations
					</p>
				</div>
			</div>
		{/if}

		<!-- Search bar -->
		<div
			class="flex w-full items-center gap-2 rounded-4xl border border-input bg-background pr-1 pl-3 shadow-sm focus-within:ring-1 focus-within:ring-ring/20"
		>
			<Search size={20} class="shrink-0 text-muted-foreground" />
			<input
				class="flex-1 bg-transparent py-2.5 text-base outline-none placeholder:text-muted-foreground"
				bind:this={inputElement}
				bind:value={searchInput}
				oninput={() => {
					if (searched) debouncedSearch();
				}}
				onkeydown={(e) => {
					if (e.key === 'Enter' && !searched) search(true);
				}}
			/>

			{#if searched}
				{#if searchInput.trim()}
					<button
						transition:fade={{ duration: 50 }}
						class="cursor-pointer rounded-full p-2"
						onclick={clearSearch}
					>
						<X size={20} class="shrink-0 text-muted-foreground" />
					</button>
				{/if}
			{:else}
				<button
					transition:fade={{ duration: 50 }}
					class="cursor-pointer rounded-full bg-primary p-2"
					onclick={() => search(true)}
				>
					<ArrowRight size={20} class="shrink-0 text-primary-foreground" />
				</button>
			{/if}
		</div>
	</div>

	{#if searched}
		<div
			class="mt-6 w-full overflow-y-auto px-4 pb-10"
			transition:fly={{ y: 10, duration: 300, delay: 200 }}
		>
			{#if loading && items.length === 0}
				<div class="flex h-screen w-full items-center justify-center">
					<Spinner class="h-10 w-10 shrink-0" />
				</div>
			{:else}
				<div class="mx-auto max-w-4xl divide-y divide-border">
					{#if items.length === 0}
						<p class="py-12 text-center text-sm text-muted-foreground">No results found.</p>
					{:else}
						{#each items as item (item.id)}
							{@render result(item)}
						{/each}
					{/if}
				</div>

				<div class="mx-auto flex w-full max-w-4xl justify-center py-5">
					{#if loading}
						<Spinner class="h-5 w-5 shrink-0" />
					{:else if hasMore}
						<button class="cursor-pointer text-sm text-muted-foreground" onclick={fetchItems}>
							Load more
						</button>
					{/if}
				</div>
			{/if}
		</div>
	{/if}
</div>
