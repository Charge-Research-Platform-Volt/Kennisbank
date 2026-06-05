<script lang="ts">
	import { api } from '$lib/api';
	import { debounce } from '$lib/utils/debounce';
	import { confirm } from '$lib/state/confirm.svelte';
	import { toast } from 'svelte-sonner';
	import {
		Plus,
		Search,
		Check,
		X,
		Pencil,
		Trash2,
		Merge,
		Tag,
		MapPin,
		Layers,
		User,
		Building2,
		Newspaper
	} from '@lucide/svelte';
	import { Button } from '$lib/components/ui/button';
	import { Input } from '$lib/components/ui/input';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import * as Table from '$lib/components/ui/table';
	import * as Pagination from '$lib/components/ui/pagination';
	import * as Dialog from '$lib/components/ui/dialog';
	import { SvelteSet } from 'svelte/reactivity';
	import { untrack } from 'svelte';
	import Checkbox from '$lib/components/ui/checkbox/checkbox.svelte';
	import type { ListItem, PagedResult } from '$lib/types/results';

	let {
		type
	}: { type: 'tags' | 'regions' | 'resourceTypes' | 'persons' | 'organisations' | 'journals' } =
		$props();

	type EntityConfig = { label: string; icon: typeof Tag; basePath: string; canCreate: boolean; renameViaBody?: boolean };

	const configs: Record<string, EntityConfig> = {
		tags:          { label: 'Tag',           icon: Tag,       basePath: 'tags',           canCreate: true },
		regions:       { label: 'Region',        icon: MapPin,    basePath: 'regions',        canCreate: true },
		resourceTypes: { label: 'Resource Type', icon: Layers,    basePath: 'resource-types', canCreate: true },
		persons:       { label: 'Person',        icon: User,      basePath: 'persons',        canCreate: false, renameViaBody: true },
		organisations: { label: 'Organisation',  icon: Building2, basePath: 'organisations',  canCreate: false, renameViaBody: true },
		journals:      { label: 'Journal',       icon: Newspaper, basePath: 'journals',       canCreate: true },
	};

	const cfg = $derived(configs[type]);
	const base = $derived(`/api/${cfg.basePath}`);

	type MergeSuggestion = { id1: string; name1: string; id2: string; name2: string; score: number };

	let items = $state<ListItem[]>([]);
	let loading = $state(true);
	let search = $state('');
	let currentPage = $state(1);
	let pageCount = $state(1);
	let totalCount = $state(0);

	let suggestions = $state<MergeSuggestion[]>([]);
	let suggestionsLoading = $state(false);

	let editingId = $state<string | null>(null);
	let editingName = $state('');

	let creatingName = $state('');
	let creating = $state(false);

	let submitting = $state(false);

	let selectedIds = new SvelteSet<string>();

	const allSelected = $derived(items.length > 0 && selectedIds.size === items.length);
	const someSelected = $derived(selectedIds.size > 0 && selectedIds.size < items.length);
	const selectedItems = $derived(items.filter((i) => selectedIds.has(i.id)));

	let mergeDialogOpen = $state(false);
	let mergeDialogItems = $state<ListItem[]>([]);
	let mergeSurvivorId = $state('');
	let mergeSurvivorName = $state('');
	let mergeNameEditedManually = $state(false);
	let mergeConfirmText = $state('');

	let fetchItemsController: AbortController | null = null;
	let fetchSuggestionsController: AbortController | null = null;

	async function fetchItems(page = currentPage) {
		fetchItemsController?.abort();
		fetchItemsController = new AbortController();
		const signal = fetchItemsController.signal;

		loading = true;
		selectedIds.clear();

		try {
			const result = await api.get<PagedResult<ListItem>>(
				`${base}?page=${page}&pageSize=50&search=${encodeURIComponent(search)}`,
				{ signal }
			);
			items = result.items;
			totalCount = result.totalCount;
			pageCount = Math.ceil(result.totalCount / 50);
		} catch (e) {
			if (!(e instanceof DOMException && e.name === 'AbortError')) throw e;
		} finally {
			if (!signal.aborted) loading = false;
		}
	}

	async function fetchSuggestions() {
		fetchSuggestionsController?.abort();
		fetchSuggestionsController = new AbortController();
		const signal = fetchSuggestionsController.signal;

		suggestionsLoading = true;
		suggestions = [];

		try {
			suggestions = await api.get<MergeSuggestion[]>(`${base}/suggestions`, { signal });
		} catch (e) {
			if (!(e instanceof DOMException && e.name === 'AbortError')) throw e;
		} finally {
			if (!signal.aborted) suggestionsLoading = false;
		}
	}

	const debouncedSearch = debounce(onSearch);
	function onSearch() {
		currentPage = 1;
		pageCount = 1;
		items = [];
		fetchItems();
	}

	async function createItem() {
		if (!creatingName.trim() || !cfg.canCreate) return;
		submitting = true;
		try {
			await api.put(base, { name: creatingName.trim() });
			creatingName = '';
			creating = false;
			await fetchItems();
			await fetchSuggestions();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : `Failed to create ${cfg.label}.`);
		} finally {
			submitting = false;
		}
	}

	async function renameItem() {
		if (!editingId) return;
		const original = items.find((i) => i.id === editingId)?.name;
		if (editingName.trim() === original) {
			editingId = null;
			return;
		}
		submitting = true;
		try {
			cfg.renameViaBody
				? await api.patch(`${base}/${editingId}`, { name: editingName.trim() })
				: await api.patch(`${base}/${editingId}/name`, { name: editingName.trim() });
			editingId = null;
			await fetchItems();
			toast.success(`${cfg.label} renamed.`);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : `Failed to rename ${cfg.label}.`);
		} finally {
			submitting = false;
		}
	}

	async function deleteItem(item: ListItem) {
		const ok = await confirm(`Delete "${item.name}"? This cannot be undone.`);
		if (!ok) return;
		try {
			await api.delete(`${base}/${item.id}`);
			await fetchItems();
			await fetchSuggestions();
			toast.success(`${cfg.label} deleted.`);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : `Failed to delete ${cfg.label}.`);
		}
	}

	async function deleteSelected() {
		const count = selectedIds.size;
		const ok = await confirm(
			`Delete ${count} ${cfg.label.toLowerCase()}${count !== 1 ? 's' : ''}? This cannot be undone.`,
			count >= 5 ? 'DELETE' : undefined
		);

		if (!ok) return;

		try {
			await Promise.all([...selectedIds].map((id) => api.delete(`${base}/${id}`)));
			await fetchItems();
			await fetchSuggestions();

			toast.success(`${count} ${cfg.label.toLowerCase()}${count !== 1 ? 's' : ''} deleted.`);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to delete.');
		}
	}

	async function executeMerge() {
		submitting = true;
		const otherIds = mergeDialogItems.filter((i) => i.id !== mergeSurvivorId).map((i) => i.id);

		try {
			for (const id of otherIds) {
				await api.patch(`${base}/merge/${mergeSurvivorId}/${id}`);
			}

			const survivorOriginalName = mergeDialogItems.find((i) => i.id === mergeSurvivorId)?.name;
			mergeSurvivorName = mergeSurvivorName.trim();

			if (mergeSurvivorName && mergeSurvivorName !== survivorOriginalName) {
				cfg.renameViaBody
					? await api.patch(`${base}/${mergeSurvivorId}`, { name: mergeSurvivorName })
					: await api.patch(`${base}/${mergeSurvivorId}/name`, { name: mergeSurvivorName });
			}

			mergeDialogOpen = false;
			selectedIds.clear();

			await fetchItems();
			await fetchSuggestions();

			toast.success(`${cfg.label}s merged.`);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to merge.');
		} finally {
			submitting = false;
		}
	}

	function openMergeDialog(itemsToMerge: ListItem[]) {
		mergeDialogItems = itemsToMerge;
		mergeSurvivorId = itemsToMerge[0].id;
		mergeSurvivorName = itemsToMerge[0].name;
		mergeNameEditedManually = false;
		mergeConfirmText = '';
		mergeDialogOpen = true;
	}

	$effect(() => {
		if (type)
			untrack(() => {
				currentPage = 1;
				search = '';
				selectedIds.clear();
				suggestions = [];
				editingId = null;
				creating = false;
			});
		fetchSuggestions();
	});

	$effect(() => {
		fetchItems(currentPage);
	});
</script>

<div class="flex h-full flex-1 overflow-hidden">
	<!-- List -->
	<div class="relative flex min-w-0 flex-1 flex-col gap-4 overflow-hidden p-4">
		<!-- Header -->
		<div class="flex flex-col gap-1">
			<div class="flex shrink-0 items-center justify-between gap-5">
				<div
					class="flex flex-1 items-center gap-2 rounded-md border border-input bg-background px-3 focus-within:border-ring focus-within:ring-2 focus-within:ring-ring/50"
				>
					<Search size={16} class="shrink-0 text-muted-foreground" />

					<input
						class="flex-1 bg-transparent py-1.5 text-sm outline-none placeholder:text-muted-foreground"
						bind:value={search}
						oninput={debouncedSearch}
						placeholder="Search {cfg.label.toLowerCase()}s..."
					/>
				</div>

				{#if cfg.canCreate}
					<Button onclick={() => (creating = true)} class="cursor-pointer">
						<Plus size={14} class="shrink-0" /> New {cfg.label}
					</Button>
				{/if}
			</div>
			<span class="pl-3 text-sm text-muted-foreground">{totalCount} entries</span>
		</div>

		<!-- Table -->
		<div class="flex-1 overflow-y-auto">
			{#if loading}
				<div class="flex h-full items-center justify-center">
					<Spinner class="h-8 w-8" />
				</div>
			{:else}
				<Table.Root>
					<Table.Header>
						<Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
							<Table.Head class="w-px">
								<Checkbox
									class="cursor-pointer"
									checked={allSelected}
									indeterminate={someSelected}
									onCheckedChange={(v) => {
										if (v) items.forEach((i) => selectedIds.add(i.id));
										else selectedIds.clear();
									}}
								/>
							</Table.Head>
							<Table.Head>Name</Table.Head>
							<Table.Head class="w-px"></Table.Head>
						</Table.Row>
					</Table.Header>

					<Table.Body>
						{#if creating}
							<Table.Row>
								<Table.Cell colspan={3} class="py-2">
									<div class="flex items-center gap-2">
										<Input
											bind:value={creatingName}
											placeholder="Name..."
											class="h-8"
											autofocus
											onkeydown={(e) => {
												if (e.key === 'Enter') createItem();
												else if (e.key === 'Escape') {
													creating = false;
													creatingName = '';
												}
											}}
										/>
										<button
											onclick={createItem}
											class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
											><Check size={14} /></button
										>
										<button
											onclick={() => {
												creating = false;
												creatingName = '';
											}}
											class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
											><X size={14} /></button
										>
									</div>
								</Table.Cell>
							</Table.Row>
						{/if}

						{#if items.length === 0 && !creating}
							<Table.Row>
								<Table.Cell colspan={2} class="py-12 text-center text-sm text-muted-foreground">
									No {cfg.label.toLowerCase()}s found.
								</Table.Cell>
							</Table.Row>
						{/if}

						{#each items as item (item.id)}
							{@const ItemIcon = cfg.icon}
							<Table.Row class="group">
								<Table.Cell class="w-px py-2">
									<Checkbox
										class="cursor-pointer"
										checked={selectedIds.has(item.id)}
										onCheckedChange={(v) => {
											if (v) selectedIds.add(item.id);
											else selectedIds.delete(item.id);
										}}
										onclick={(e) => e.stopPropagation()}
									/>
								</Table.Cell>

								<Table.Cell class="py-2">
									{#if editingId === item.id}
										<Input
											bind:value={editingName}
											class="h-8"
											autofocus
											onkeydown={(e) => {
												if (e.key === 'Enter') renameItem();
												else if (e.key === 'Escape') editingId = null;
											}}
										/>
									{:else}
										<div class="flex items-center gap-2">
											<ItemIcon size={14} class="shrink-0 text-muted-foreground" />
											<span class="text-sm">{item.name}</span>
										</div>
									{/if}
								</Table.Cell>

								<Table.Cell class="py-2 text-right">
									<div
										class="flex items-center justify-end gap-1 opacity-0 transition-opacity group-hover:opacity-100"
									>
										{#if editingId === item.id}
											<button
												onclick={(e) => {
													e.stopPropagation();
													renameItem();
												}}
												class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
												><Check size={14} /></button
											>
											<button
												onclick={(e) => {
													e.stopPropagation();
													editingId = null;
												}}
												class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
												><X size={14} /></button
											>
										{:else}
											<button
												onclick={(e) => {
													e.stopPropagation();
													editingId = item.id;
													editingName = item.name;
												}}
												class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
												><Pencil size={14} /></button
											>
											<button
												onclick={(e) => {
													e.stopPropagation();
													deleteItem(item);
												}}
												class="cursor-pointer rounded-md p-1.5 text-muted-foreground transition-colors hover:bg-accent hover:text-destructive"
												><Trash2 size={14} /></button
											>
										{/if}
									</div>
								</Table.Cell>
							</Table.Row>
						{/each}
					</Table.Body>
				</Table.Root>
			{/if}

			{#if pageCount > 1}
				<Pagination.Root bind:page={currentPage} count={totalCount} perPage={50} class="py-2">
					{#snippet children({ pages, currentPage: cp })}
						<Pagination.Content>
							<Pagination.Item><Pagination.PrevButton class="cursor-pointer" /></Pagination.Item>
							{#each pages as page (page.key)}
								{#if page.type === 'ellipsis'}
									<Pagination.Item><Pagination.Ellipsis /></Pagination.Item>
								{:else}
									<Pagination.Item>
										<Pagination.Link {page} isActive={cp === page.value} class="cursor-pointer" />
									</Pagination.Item>
								{/if}
							{/each}
							<Pagination.Item><Pagination.NextButton class="cursor-pointer" /></Pagination.Item>
						</Pagination.Content>
					{/snippet}
				</Pagination.Root>
			{/if}
		</div>

		<!-- Bulk action pill -->
		<div
			class="absolute bottom-4 left-1/2 -translate-x-1/2 transition-all duration-200 {selectedIds.size >
			0
				? 'translate-y-0 opacity-100'
				: 'pointer-events-none translate-y-3 opacity-0'}"
		>
			<div
				class="flex items-center gap-1 rounded-full border bg-popover px-2 py-1.5 text-sm shadow-lg"
			>
				<span class="px-2 text-muted-foreground">{selectedIds.size} selected</span>
				<div class="mx-1 h-4 w-px bg-border"></div>
				<button
					onclick={deleteSelected}
					class="flex cursor-pointer items-center gap-1.5 rounded-full px-3 py-1 text-muted-foreground transition-colors hover:bg-destructive/10 hover:text-destructive"
				>
					<Trash2 size={13} /> Delete
				</button>
				{#if selectedIds.size >= 2}
					<button
						onclick={() => openMergeDialog(selectedItems)}
						class="flex cursor-pointer items-center gap-1.5 rounded-full px-3 py-1 text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
					>
						<Merge size={13} /> Merge
					</button>
				{/if}
			</div>
		</div>
	</div>

	<!-- Suggestions panel -->
	<div class="flex w-96 shrink-0 flex-col overflow-hidden border-l bg-background">
		<div class="flex shrink-0 items-center justify-between border-b p-4">
			<span class="text-sm font-medium">Merge Suggestions</span>
			{#if suggestions.length > 0}
				<span class="text-xs text-muted-foreground"
					>{suggestions.length} pair{suggestions.length !== 1 ? 's' : ''}</span
				>
			{/if}
		</div>

		<div class="flex-1 overflow-y-auto">
			{#if suggestionsLoading}
				<div class="flex h-full items-center justify-center">
					<Spinner class="h-6 w-6" />
				</div>
			{:else if suggestions.length === 0}
				<div class="flex h-full items-center justify-center">
					<span class="text-sm text-muted-foreground">No suggestions.</span>
				</div>
			{:else}
				<div class="flex flex-col divide-y">
					{#each suggestions as s (s.id1 + s.id2)}
						<div class="flex items-center gap-2 p-3">
							<!-- Names -->
							<div class="flex min-w-0 flex-1 flex-col gap-0.5">
								<span class="text-sm leading-tight font-medium break-words">{s.name1}</span>
								<span class="text-xs text-muted-foreground">vs</span>
								<span class="text-sm leading-tight font-medium break-words">{s.name2}</span>
							</div>

							<div class="flex flex-col items-end justify-between gap-5">
								<!-- Score -->
								<span
									class="shrink-0 rounded-full bg-muted px-2 py-0.5 text-xs font-medium text-muted-foreground"
									>{Math.round(s.score * 100)}%</span
								>

								<!-- Merge button -->
								<button
									class="flex cursor-pointer items-center gap-1.5 rounded-md px-2 py-1 text-xs text-muted-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
									onclick={() =>
										openMergeDialog([
											{ id: s.id1, name: s.name1 },
											{ id: s.id2, name: s.name2 }
										])}
								>
									<Merge size={14} /> Merge
								</button>
							</div>
						</div>
					{/each}
				</div>
			{/if}
		</div>
	</div>
</div>

<Dialog.Root bind:open={mergeDialogOpen}>
	<Dialog.Content class="max-w-md">
		<Dialog.Header>
			<Dialog.Title
				>Merge {mergeDialogItems.length}
				{cfg.label}{mergeDialogItems.length !== 1 ? 's' : ''}</Dialog.Title
			>
			<Dialog.Description
				>Choose which {cfg.label.toLowerCase()} to keep. All others will be absorbed into it.</Dialog.Description
			>
		</Dialog.Header>

		<!-- Survivor picker -->
		<div class="flex flex-col gap-1">
			<span class="text-sm font-medium">Keep</span>
			<div class="flex max-h-48 flex-col gap-1 overflow-y-auto">
				{#each mergeDialogItems as item (item.id)}
					{@const ItemIcon = cfg.icon}
					<button
						onclick={() => {
							mergeSurvivorId = item.id;
							if (!mergeNameEditedManually) mergeSurvivorName = item.name;
						}}
						class="flex cursor-pointer items-center gap-3 rounded-md border px-3 py-2 text-left text-sm transition-colors
                            {mergeSurvivorId === item.id
							? 'border-primary bg-primary/5'
							: 'border-transparent hover:bg-accent'}"
					>
						<div
							class="flex size-4 shrink-0 items-center justify-center rounded-full border-2
                            {mergeSurvivorId === item.id
								? 'border-primary'
								: 'border-muted-foreground'}"
						>
							{#if mergeSurvivorId === item.id}
								<div class="size-2 rounded-full bg-primary"></div>
							{/if}
						</div>

						<ItemIcon size={14} class="shrink-0 text-muted-foreground" />
						<span>{item.name}</span>
					</button>
				{/each}
			</div>
		</div>

		<!-- Optional rename -->
		<div class="flext flex-col gap-1.5">
			<span class="text-sm font-medium"
				>Name <span class="font-normal text-muted-foreground">(optional rename)</span></span
			>
			<Input
				bind:value={mergeSurvivorName}
				oninput={() => (mergeNameEditedManually = true)}
				placeholder="Name..."
			/>
		</div>

		<!-- Confirm input for 5+ -->
		{#if mergeDialogItems.length >= 5}
			<div class="flex flex-col gap-1.5">
				<span class="text-sm text-muted-foreground"
					>Type <span class="font-mono font-medium text-foreground">MERGE</span> to confirm</span
				>
				<Input bind:value={mergeConfirmText} placeholder="MERGE" />
			</div>
		{/if}

		<Dialog.Footer>
			<Button variant="outline" onclick={() => (mergeDialogOpen = false)} class="cursor-pointer"
				>Cancel</Button
			>
			<Button
				onclick={executeMerge}
				disabled={submitting || (mergeDialogItems.length >= 5 && mergeConfirmText !== 'MERGE')}
				class="cursor-pointer"
			>
				<Merge size={14} /> Merge {mergeDialogItems.length}
			</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
