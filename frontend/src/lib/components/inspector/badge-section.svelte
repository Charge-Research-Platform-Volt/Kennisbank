<script lang="ts">
	import type { ListItem } from '$lib/types/results';
	import type { RelationItem } from '$lib/types/resource';
	import { getContext, untrack } from 'svelte';
	import { X } from '@lucide/svelte';
	import { debounce } from '$lib/utils/debounce';

	let {
		label,
		items,
		search,
		onadd,
		oncreate,
		onremove
	}: {
		label: string;
		items: RelationItem[];
		search?: (q: string) => Promise<ListItem[]>;
		onadd?: (id: string, name: string) => Promise<void>;
		oncreate?: (name: string) => Promise<ListItem | null>;
		onremove?: (item: RelationItem) => Promise<void>;
	} = $props();

	const getEditMode = getContext<() => boolean>('getEditMode');
	let editMode = $derived(getEditMode());
	const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

	let localItems = $state<RelationItem[]>(untrack(() => items.map((i) => ({ ...i }))));

	let searchQuery = $state('');
	let searchResults = $state<ListItem[]>([]);
	let searchOpen = $state(false);
	let filtered = $derived(searchResults.filter((r) => !localItems.some((i) => i.id === r.id)));
	const debouncedSearch = debounce((q: string) => {
		if (!search) return;
		search?.(q).then((r) => {
			searchResults = r;
			searchOpen = true;
		});
	}, 200);

	function onSearchInput(q: string) {
		searchQuery = q;
		debouncedSearch(q);
	}

	function addItem(id: string, name: string) {
		if (localItems.some((i) => i.id === id)) return;
		localItems = [{ id, name }, ...localItems];
		registerSave?.(onadd!(id, name));
		searchQuery = '';
		searchResults = [];
		searchOpen = false;
	}

	function createItem() {
		const name = searchQuery.trim();
		if (!name || !oncreate) return;
		const p = (async () => {
			const result = await oncreate(name);
			if (result && !localItems.some((i) => i.id === result.id)) {
				localItems = [{ id: result.id, name: result.name }, ...localItems];
				await onadd?.(result.id, result.name);
			}
		})();
		registerSave?.(p);
		searchQuery = '';
		searchResults = [];
		searchOpen = false;
	}

	function removeItem(item: RelationItem) {
		localItems = localItems.filter((i) => i.id !== item.id);
		registerSave?.(onremove!(item));
	}
</script>

{#if localItems.length || editMode}
	<div class="flex flex-col gap-2 border-b px-3 py-3">
		<span class="text-xs font-medium text-muted-foreground">{label}</span>

		{#if localItems.length}
			<div class="flex flex-wrap gap-1">
				{#each localItems as item (item.id)}
					{#if editMode && onremove}
						<span
							class="flex items-center gap-1 rounded-full border border-border bg-muted py-0.5 pr-1 pl-2 text-xs text-muted-foreground"
						>
							{item.name}
							<button
								onclick={() => removeItem(item)}
								class="cursor-pointer transition-colors hover:text-foreground"
							>
								<X size={10} />
							</button>
						</span>
					{:else}
						<span
							class="rounded-full border border-border bg-muted px-2 py-0.5 text-xs text-muted-foreground"
							>{item.name}</span
						>
					{/if}
				{/each}
			</div>
		{/if}

		{#if editMode && (search || oncreate)}
			<div class="relative">
				<input
					type="text"
					value={searchQuery}
					oninput={(e) => onSearchInput(e.currentTarget.value)}
					onkeydown={(e) => {
						if (e.key === 'Enter') {
							e.preventDefault();
							if (searchOpen && filtered.length) addItem(filtered[0].id, filtered[0].name);
							else if (searchQuery.trim() && oncreate) createItem();
						}
					}}
					onfocus={() => {
						if (!search) return;
						search?.(searchQuery).then((r) => {
							searchResults = r;
							searchOpen = true;
						});}}
					onblur={() =>
						setTimeout(() => {
							searchOpen = false;
						}, 150)}
					placeholder={!search ? 'Add' : oncreate ? 'Search or create...' : 'Search to add...'}
					class="w-full rounded-sm border border-input bg-transparent px-2 py-1 text-xs placeholder:text-muted-foreground focus:border-ring focus:outline-none"
				/>
				{#if searchOpen}
					
					{@const showCreate =
						!!oncreate &&
						!!searchQuery.trim() &&
						!searchResults.some((r) => r.name.toLowerCase() === searchQuery.trim().toLowerCase())}
					{#if filtered.length || showCreate}
						<div
							class="absolute top-full right-0 left-0 z-10 mt-0.5 max-h-36 overflow-y-auto rounded-sm border border-border bg-popover shadow-md"
						>
							{#each filtered as result (result.id)}
								<button
									onmousedown={() => addItem(result.id, result.name)}
									class="w-full cursor-pointer px-2 py-1.5 text-left text-xs hover:bg-accent"
								>
									{result.name}
								</button>
							{:else}
								{#if !showCreate}
									<p class="text-xs text-muted-foreground px-2 py-1.5">No results.</p>
								{/if}
							{/each}
							{#if showCreate}
								<button
									onmousedown={createItem}
									class="w-full cursor-pointer px-2 py-1.5 text-left text-xs text-muted-foreground italic hover:bg-accent"
								>
									Create "{searchQuery.trim()}"
								</button>
							{/if}
						</div>
					{/if}
				{/if}
			</div>
		{/if}
	</div>
{/if}
