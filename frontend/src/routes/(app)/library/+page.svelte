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
		Trash2
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

	// Search
	let searchInput = $state('');
	let filtersOpen = $state(false);

	// Filters
	const ALL_TYPES = ['resource', 'person', 'organisation'];
	let typeFilter = $state<string[]>(ALL_TYPES);
	let dateMin = $state('');
	let dateMax = $state('');
	let tagFilter = $state<string[]>([]);
	let regionFilter = $state<string[]>([]);
	let resourceTypeFilter = $state<string[]>([]);
	let journalFilter = $state<string[]>([]);

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

	const debouncedFetchItems = debounce(fetchItems);

	const openInspector: (item: ResourceItem) => void = getContext('openInspector');
	const registerRefresh: (fn: () => void) => void = getContext('registerRefresh');

	async function fetchItems() {
		loading = true;
		try {
			const result = await api.post<PagedResult<ResourceItem>>(
				'/api/library',
				{
					page: currentPage,
					pageSize: PAGE_SIZE,
					searchQuery: searchInput || undefined,
					filterOptions: {
						typeFilter: typeFilter.length === 3 ? undefined : typeFilter,
						pubdateMin: dateMin ? `${dateMin}-01-01` : undefined,
						pubdateMax: dateMax ? `${dateMax}-12-31` : undefined,
						tagFilter: tagFilter.length ? tagFilter : undefined,
						regionFilter: regionFilter.length ? regionFilter : undefined,
						resourceTypeFilter: resourceTypeFilter.length ? resourceTypeFilter : undefined,
						journalFilter: journalFilter.length ? journalFilter : undefined
					},
					sortBy: sortBy || undefined,
					sortDirection
				}
			);
			items = result.items;
			totalItems = result.totalCount;
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
		const result = await api.get<PagedResult<ListItem>>(`/api/tags?page=1&pageSize=20&search=${encodeURIComponent(q)}`);
		return result.items;
	}

	async function searchRegions(q: string) {
		const result = await api.get<PagedResult<ListItem>>(`/api/regions?page=1&pageSize=20&search=${encodeURIComponent(q)}`);
		return result.items;
	}

	function handleTagFilterChange() {
		currentPage = 1;
		if (tagFilter.length) typeFilter = ['resource'];
		setParams({
			tags: tagFilter.length ? tagFilter.join(',') : null,
			type: typeFilter.length === 3 ? null : typeFilter.join(','),
			page: null
		});
		fetchItems();
	}

	function handleRegionFilterChange() {
		currentPage = 1;
		if (regionFilter.length) typeFilter = ['resource'];
		setParams({
			regions: regionFilter.length ? regionFilter.join(',') : null,
			type: typeFilter.length === 3 ? null : typeFilter.join(','),
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
			type: typeFilter.length === 3 ? null : typeFilter.join(','),
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
			type: typeFilter.length === 3 ? null : typeFilter.join(','),
			page: null
		});
		fetchItems();
	}

	function resetFilters() {
		typeFilter = ALL_TYPES;
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
		const next = v.length === 0 ? ALL_TYPES : v;
		typeFilter = next;
		currentPage = 1;
		setParams({ type: next.length === 3 ? null : next.join(','), page: null });
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
		typeFilter = getParamArray('type', ALL_TYPES);
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
				<ListFilter
					size={16}
					class="shrink-0 cursor-pointer text-zinc-600"
					onclick={() => {
						filtersOpen = !filtersOpen;
					}}
				/>
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
				? 'max-h-[500px]'
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
						<ToggleGroup.Item value="resource" class="text-xs">Resources</ToggleGroup.Item>
						<ToggleGroup.Item value="person" class="text-xs">Persons</ToggleGroup.Item>
						<ToggleGroup.Item value="organisation" class="text-xs">Organisations</ToggleGroup.Item>
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
				{#if typeFilter.includes('resource')}
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
				{#if typeFilter.includes('resource')}
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
								{item.name}
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
