<script lang="ts">
	import { api } from '$lib/api';
	import { debounce } from '$lib/utils/debounce';
	import type { ResourceItem } from '$lib/types/resource';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import {
		Search,
		ListFilter,
		ExternalLink,
		Download,
		X,
		ArrowUp,
		ArrowDown,
		ArrowUpDown,
		Trash2,
		LoaderCircle,
		CircleAlert
	} from '@lucide/svelte';
	import { userState } from '$lib/state/user.svelte';
	import * as Table from '$lib/components/ui/table';
	import { getFileAction, getFileIcon } from '$lib/utils/icons';
	import { openFile } from '$lib/utils/openFile';
	import * as Pagination from '$lib/components/ui/pagination';
	import type { PageItem } from 'bits-ui';
	import { getParam, getParamInt, getParamArray, setParams } from '$lib/utils/urlState';
	import * as ToggleGroup from '$lib/components/ui/toggle-group';
	import Input from '$lib/components/ui/input/input.svelte';
	import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';
	import { formatDate } from '$lib/utils/date';
	import { getContext, onMount } from 'svelte';
	import { afterNavigate } from '$app/navigation';
	import type { PagedResult, ListItem } from '$lib/types/results';
	import { onHubEvent, type HubConnectionContext } from '$lib/state/hub-connection.svelte';
	import * as Tooltip from '$lib/components/ui/tooltip';

	// Search
	let searchInput = $state('');
	let filtersOpen = $state(false);

	// Filters
	let typeFilter = $state<string[]>([]);
	let dateMin = $state('');
	let dateMax = $state('');
	let tagFilter = $state<string[]>([]);
	let regionFilter = $state<string[]>([]);
	let resourceTypeFilter = $state<string[]>([]);
	let journalFilter = $state<string[]>([]);

	let activeFilterCount = $derived(
		(typeFilter.length ? 1 : 0) +
			(dateMin || dateMax ? 1 : 0) +
			(tagFilter.length ? 1 : 0) +
			(regionFilter.length ? 1 : 0) +
			(resourceTypeFilter.length ? 1 : 0) +
			(journalFilter.length ? 1 : 0)
	);

	// Sorting
	let sortBy = $state('');
	let sortDirection = $state<'asc' | 'desc'>('asc');

	// Pagination
	const PAGE_SIZE = 50;
	let currentPage = $state(1);
	let totalItems = $state(0);

	// Results
	let items = $state<ResourceItem[]>([]);
	let loading = $state(false);
	let typeCounts = $state<Record<string, number>>({});

	const debouncedFetchItems = debounce(fetchItems);

	const openInspector: (item: ResourceItem) => void = getContext('openInspector');
	const registerRefresh: (fn: () => void) => void = getContext('registerRefresh');

	async function fetchItems() {
		loading = true;
		try {
			const result = await api.post<
				PagedResult<ResourceItem> & { typeCounts?: Record<string, number> }
			>('/api/library', {
				page: currentPage,
				pageSize: PAGE_SIZE,
				search: searchInput || undefined,
				filterOptions: {
					typeFilter: typeFilter.length === 0 ? undefined : typeFilter,
					pubdateMin: dateMin ? `${dateMin}-01-01` : undefined,
					pubdateMax: dateMax ? `${dateMax}-12-31` : undefined,
					tagFilter: tagFilter.length ? tagFilter : undefined,
					regionFilter: regionFilter.length ? regionFilter : undefined,
					resourceTypeFilter: resourceTypeFilter.length ? resourceTypeFilter : undefined,
					journalFilter: journalFilter.length ? journalFilter : undefined
				},
				sortBy: sortBy || undefined,
				sortDirection
			});
			items = result.items;
			totalItems = result.totalCount;
			typeCounts = result.typeCounts ?? {};
		} finally {
			loading = false;
		}
	}

	function onSearchInput(value: string) {
		searchInput = value;
		currentPage = 1;
		// Cancel previous timer and start a new one
		setParams({ q: searchInput || null, page: currentPage });
		debouncedFetchItems();
	}

	function handlePageChange() {
		setParams({ page: currentPage });
		fetchItems();
	}

	function handleDateChange() {
		currentPage = 1;
		setParams({ dateMin: dateMin || null, dateMax: dateMax || null, page: null });
		debouncedFetchItems();
	}

	async function searchTags(q: string) {
		const result = await api.get<PagedResult<ListItem>>(
			`/api/tags?page=1&pageSize=20&search=${encodeURIComponent(q)}`
		);
		return result.items;
	}

	async function searchRegions(q: string) {
		const result = await api.get<PagedResult<ListItem>>(
			`/api/regions?page=1&pageSize=20&search=${encodeURIComponent(q)}`
		);
		return result.items;
	}

	function handleTagFilterChange() {
		currentPage = 1;
		if (tagFilter.length) typeFilter = ['resource'];
		setParams({
			tags: tagFilter.length ? tagFilter.join(',') : null,
			type: typeFilter.length === 0 ? null : typeFilter.join(','),
			page: null
		});
		fetchItems();
	}

	function handleRegionFilterChange() {
		currentPage = 1;
		if (regionFilter.length) typeFilter = ['resource'];
		setParams({
			regions: regionFilter.length ? regionFilter.join(',') : null,
			type: typeFilter.length === 0 ? null : typeFilter.join(','),
			page: null
		});
		fetchItems();
	}

	async function searchResourceTypes(q: string) {
		const result = await api.get<PagedResult<ListItem>>(
			`/api/resource-types?q=${encodeURIComponent(q)}`
		);
		return result.items;
	}

	function handleResourceTypeFilterChange() {
		currentPage = 1;
		if (resourceTypeFilter.length) typeFilter = ['resource'];
		setParams({
			resourceTypes: resourceTypeFilter.length ? resourceTypeFilter.join(',') : null,
			type: typeFilter.length === 0 ? null : typeFilter.join(','),
			page: null
		});
		fetchItems();
	}

	async function searchJournals(q: string) {
		const result = await api.get<PagedResult<ListItem>>(
			`/api/journals?page=1&pageSize=20&search=${encodeURIComponent(q)}`
		);
		return result.items;
	}

	function handleJournalFilterChange() {
		currentPage = 1;
		if (journalFilter.length) typeFilter = ['resource'];
		setParams({
			journals: journalFilter.length ? journalFilter.join(',') : null,
			type: typeFilter.length === 0 ? null : typeFilter.join(','),
			page: null
		});
		fetchItems();
	}

	function resetFilters() {
		typeFilter = [];
		dateMin = '';
		dateMax = '';
		tagFilter = [];
		regionFilter = [];
		resourceTypeFilter = [];
		journalFilter = [];
		currentPage = 1;
		setParams({
			type: null,
			dateMin: null,
			dateMax: null,
			tags: null,
			regions: null,
			resourceTypes: null,
			journals: null,
			page: null
		});
		fetchItems();
	}

	function handleTypeFilterChange(v: string[]) {
		typeFilter = v;
		currentPage = 1;
		setParams({ type: v.length === 0 ? null : v.join(','), page: null });
		fetchItems();
	}

	function handleSort(column: string) {
		if (sortBy === column) {
			if (sortDirection === 'asc') {
				sortDirection = 'desc';
			} else {
				sortBy = '';
				sortDirection = 'asc';
			}
		} else {
			sortBy = column;
			sortDirection = 'asc';
		}

		setParams({ sort: sortBy || null, sortDir: sortDirection === 'asc' ? null : sortDirection });
		fetchItems();
	}

	function syncFromUrl() {
		searchInput = getParam('q');
		typeFilter = getParamArray('type');
		dateMin = getParam('dateMin');
		dateMax = getParam('dateMax');
		tagFilter = getParamArray('tags');
		regionFilter = getParamArray('regions');
		resourceTypeFilter = getParamArray('resourceTypes');
		journalFilter = getParamArray('journals');
		sortBy = getParam('sort');
		sortDirection = (getParam('sortDir') as 'asc' | 'desc') || 'asc';
		currentPage = getParamInt('page');
		filtersOpen = false;
		fetchItems();
	}

	const hub = getContext<HubConnectionContext>('hubConnection');

	// Embedding status updates
	onHubEvent(hub, 'EmbeddingStatusChanged', (id, status) => {
		const item = items.find((i) => i.id === id);
		if (item) item.embeddingStatus = status as ResourceItem['embeddingStatus'];
	});

	registerRefresh(fetchItems);

	onMount(() => syncFromUrl());
	afterNavigate(() => syncFromUrl());
</script>

<div class="flex h-full flex-1 flex-col gap-4 overflow-hidden p-4">
	<!-- Header -->
	<div class="flex flex-col gap-2">
		<!-- Search bar -->
		<div class="flex items-center gap-2">
			<div
				class="flex flex-1 items-center gap-2 rounded-md border border-input bg-background px-3 focus-within:border-ring focus-within:ring-2 focus-within:ring-ring/50"
			>
				<Search size={16} class="shrink-0 text-muted-foreground" />
				<input
					class="flex-1 bg-transparent py-1.5 text-sm outline-none placeholder:text-muted-foreground"
					value={searchInput}
					oninput={(e) => onSearchInput(e.currentTarget.value)}
					placeholder="Search..."
				/>
				{#if searchInput}
					<X
						size={16}
						class="shrink-0 cursor-pointer text-zinc-600"
						onclick={() => onSearchInput('')}
					/>
				{/if}

				<div class="relative shrink-0">
					<ListFilter
						size={16}
						class="cursor-pointer text-zinc-600"
						onclick={() => {
							filtersOpen = !filtersOpen;
						}}
					/>

					{#if activeFilterCount > 0}
						<span
							class="absolute -top-1.5 -right-1.5 flex h-3.5 w-3.5 items-center justify-center rounded-full bg-primary text-[9px] font-medium text-primary-foreground"
						>
							{activeFilterCount}
						</span>
					{/if}
				</div>
			</div>
			{#if userState.role === 'admin'}
				<a
					href="/library/trash"
					class="px-2 text-zinc-600 transition-colors hover:text-foreground"
					title="Trash"
				>
					<Trash2 size={18} class="shrink-0" />
				</a>
			{/if}
		</div>

		<!-- Filter section -->
		<div
			class="overflow-hidden transition-all duration-300 ease-in-out {filtersOpen
				? 'max-h-125'
				: 'max-h-0'}"
		>
			<div class="flex flex-wrap gap-x-8 gap-y-3 border-b border-border px-1 py-3">
				<!-- Type filter -->
				<div class="flex items-center gap-3">
					<span class="w-20 shrink-0 text-sm font-medium text-muted-foreground">Category</span>
					<ToggleGroup.Root
						variant="outline"
						class="flex-wrap"
						type="multiple"
						bind:value={typeFilter}
						onValueChange={handleTypeFilterChange}
					>
						<ToggleGroup.Item value="resource" class="cursor-pointer text-xs"
							>Resources{typeCounts.resource !== undefined
								? ` (${typeCounts.resource})`
								: ''}</ToggleGroup.Item
						>
						<ToggleGroup.Item value="person" class="cursor-pointer text-xs"
							>Persons{typeCounts.person !== undefined
								? ` (${typeCounts.person})`
								: ''}</ToggleGroup.Item
						>
						<ToggleGroup.Item value="organisation" class="cursor-pointer text-xs"
							>Organisations{typeCounts.organisation !== undefined
								? ` (${typeCounts.organisation})`
								: ''}</ToggleGroup.Item
						>
					</ToggleGroup.Root>
				</div>

				<!-- Date filter -->
				<div class="flex items-center gap-3">
					<span class="w-20 shrink-0 text-sm font-medium text-muted-foreground">Published</span>
					<div class="flex items-center gap-2">
						<Input
							type="number"
							placeholder="From"
							class="w-24 text-xs"
							bind:value={dateMin}
							oninput={handleDateChange}
							min={1500}
							max={dateMax || 3000}
						/>
						<span class="text-xs text-muted-foreground">-</span>
						<Input
							type="number"
							placeholder="To"
							class="w-24 text-xs"
							bind:value={dateMax}
							oninput={handleDateChange}
							min={dateMin || 1500}
							max={3000}
						/>
					</div>
				</div>

				<!-- Tag filter -->
				<div class="flex items-center gap-3">
					<span class="w-20 shrink-0 text-sm font-medium text-muted-foreground">Tags</span>
					<AsyncMultiSelect
						class="w-50"
						bind:value={tagFilter}
						search={searchTags}
						placeholder="Tags"
						onchange={handleTagFilterChange}
					/>
				</div>

				<!-- Region filter -->
				<div class="flex items-center gap-3">
					<span class="w-20 shrink-0 text-sm font-medium text-muted-foreground">Regions</span>
					<AsyncMultiSelect
						class="w-50"
						bind:value={regionFilter}
						search={searchRegions}
						placeholder="Regions"
						onchange={handleRegionFilterChange}
					/>
				</div>

				<!-- Resource type filter -->
				{#if typeFilter.length === 0 || typeFilter.includes('resource')}
					<div class="flex items-center gap-3">
						<span class="w-20 shrink-0 text-sm font-medium text-muted-foreground">Types</span>
						<AsyncMultiSelect
							class="w-50"
							bind:value={resourceTypeFilter}
							search={searchResourceTypes}
							placeholder="Resource types"
							onchange={handleResourceTypeFilterChange}
						/>
					</div>
				{/if}

				<!-- Journal filter -->
				{#if typeFilter.length === 0 || typeFilter.includes('resource')}
					<div class="flex items-center gap-3">
						<span class="w-20 shrink-0 text-sm font-medium text-muted-foreground">Journal</span>
						<AsyncMultiSelect
							class="w-50"
							bind:value={journalFilter}
							search={searchJournals}
							placeholder="Journals"
							onchange={handleJournalFilterChange}
						/>
					</div>
				{/if}

				<!-- Reset -->
				<button
					class="cursor-pointer self-center text-xs text-muted-foreground underline underline-offset-2 hover:text-foreground"
					onclick={resetFilters}
				>
					Reset filters
				</button>
			</div>
		</div>
	</div>

	<!-- List -->
	{#if loading}
		<div class="flex h-full w-full items-center justify-center">
			<Spinner class="h-10 w-10" />
		</div>
	{:else}
		<Table.Root>
			<Table.Caption>
				<Pagination.Root
					count={totalItems}
					perPage={PAGE_SIZE}
					bind:page={currentPage}
					onPageChange={handlePageChange}
				>
					{#snippet children({ pages, currentPage }: { pages: PageItem[]; currentPage: number })}
						<Pagination.Content>
							<Pagination.Item>
								<Pagination.Previous class="cursor-pointer" />
							</Pagination.Item>

							{#each pages as page (page.key)}
								{#if page.type === 'ellipsis'}
									<Pagination.Item>
										<Pagination.Ellipsis />
									</Pagination.Item>
								{:else}
									<Pagination.Item>
										<Pagination.Link
											class="cursor-pointer"
											{page}
											isActive={currentPage === page.value}
										>
											{page.value}
										</Pagination.Link>
									</Pagination.Item>
								{/if}
							{/each}

							<Pagination.Item>
								<Pagination.Next class="cursor-pointer" />
							</Pagination.Item>
						</Pagination.Content>
					{/snippet}
				</Pagination.Root>
			</Table.Caption>

			<Table.Header>
				<tr class="border-b">
					<Table.Head class="w-full cursor-pointer select-none" onclick={() => handleSort('name')}>
						<div class="flex items-center gap-1">
							Name

							{#if sortBy === 'name'}
								{#if sortDirection === 'asc'}
									<ArrowUp size={12} />
								{:else}
									<ArrowDown size={12} />
								{/if}
							{:else}
								<ArrowUpDown size={12} class="text-muted-foreground/50" />
							{/if}
						</div>
					</Table.Head>
					<Table.Head
						class="w-px cursor-pointer whitespace-nowrap select-none"
						onclick={() => handleSort('publicationDate')}
					>
						<div class="flex items-center justify-center gap-1">
							Published

							{#if sortBy === 'publicationDate'}
								{#if sortDirection === 'asc'}
									<ArrowUp size={12} />
								{:else}
									<ArrowDown size={12} />
								{/if}
							{:else}
								<ArrowUpDown size={12} class="text-muted-foreground/50" />
							{/if}
						</div>
					</Table.Head>
					<Table.Head class="w-px whitespace-nowrap"></Table.Head>
				</tr>
			</Table.Header>

			<Table.Body>
				{#if items.length === 0}
					<Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
						<Table.Cell colspan={3} class="py-12 text-center text-sm text-muted-foreground">
							{searchInput ? 'No results found.' : 'Nothing here yet.'}
						</Table.Cell>
					</Table.Row>
				{/if}
				{#each items as item (item.id)}
					{@const Icon = getFileIcon(item.fileType)}
					{@const action = getFileAction(item.fileType)}

					<Table.Row class="cursor-pointer" onclick={() => openInspector(item)}>
						<Table.Cell class="py-3">
							<div class="flex items-center gap-3">
								<Icon size={16} class="shrink-0 text-muted-foreground" />
								<span class="min-w-0 flex-1 truncate">{item.name}</span>

								<!-- Embedding status badge -->
								{#if item.embeddingStatus === 'Pending' || item.embeddingStatus === 'Processing'}
									<div class="shrink-0">
										<Tooltip.Root>
											<Tooltip.Trigger>
												<LoaderCircle size={13} class="shrink-0 animate-spin text-muted-foreground" />
											</Tooltip.Trigger>
											<Tooltip.Content>
												Still processing...
											</Tooltip.Content>
										</Tooltip.Root>
									</div>
								{:else if item.embeddingStatus === 'Failed'}
									<div class="shrink-0">
										<Tooltip.Root>
											<Tooltip.Trigger>
												<CircleAlert size={13} class="shrink-0 text-destructive" />
											</Tooltip.Trigger>
											<Tooltip.Content>
												Embedding failed
											</Tooltip.Content>
										</Tooltip.Root>
									</div>
								{/if}
							</div>
						</Table.Cell>
						<Table.Cell class="px-5 py-3 text-center whitespace-nowrap"
							>{formatDate(item.publicationDate, item.publicationDatePrecision)}</Table.Cell
						>
						<Table.Cell class="p-3">
							<div class="flex items-center justify-center">
								{#if action === 'open'}
									<button
										class="cursor-pointer"
										onclick={(e) => {
											e.stopPropagation();
											openFile(item.id, item.fileType, item.sourceUrl);
										}}
									>
										<ExternalLink size={14} />
									</button>
								{:else if action === 'download'}
									<button
										class="cursor-pointer"
										onclick={(e) => {
											e.stopPropagation();
											openFile(item.id, item.fileType, item.sourceUrl);
										}}
									>
										<Download size={14} />
									</button>
								{/if}
							</div>
						</Table.Cell>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	{/if}
</div>
