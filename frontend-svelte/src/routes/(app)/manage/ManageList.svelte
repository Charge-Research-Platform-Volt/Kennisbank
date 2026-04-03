<script lang="ts">
    /*
     * TODO:
     * Checkboxes in the table → select multiple items directly
     * When 2+ selected, a "Merge X selected" button appears in the header
     * Dialog opens just for choosing the target (radio among selected items + optional new name)
     * SSuggestions panel pre-checks the pair and opens the dialog
     */

    import { api } from "$lib/api";
    import { debounce } from "$lib/utils/debounce";
    import { confirm } from "$lib/state/confirm.svelte";
    import { toast } from "svelte-sonner";
	import { Plus, Search, Check, X, Pencil, Trash2, Merge, Tag, MapPin, Layers } from "lucide-svelte";
	import { Button } from "$lib/components/ui/button";
	import { Input } from "$lib/components/ui/input";
	import Spinner from "$lib/components/ui/spinner/spinner.svelte";
	import * as Table from "$lib/components/ui/table";
	import * as Pagination from "$lib/components/ui/pagination";

    let { type }: { type: 'tags' | 'regions' | 'resourceTypes' } = $props();

    type ListItem = { id: string; name: string; };
    type TagsListResponse = { tags: ListItem[]; pageCount: number; };
    type FlatListResponse = ListItem[];

    const config = {
        tags: {
            label: 'Tag',
            icon: Tag,
            listUrl: (search: string, page: number) =>
                `/api/tags/tag-page?pageIndex=${page}&pageSize=50&searchQuery=${encodeURIComponent(search)}`,
            createUrl: '/api/tags/add-standard-tag',
            createBody: (name: string) => ({ name }),
            renameUrl: (id: string, name: string) => `/api/tags/rename-tag/${id}/${encodeURIComponent(name)}`,
            renameBody: undefined as undefined,
            deleteUrl: (id: string) => `/api/tags/delete-tag/${id}`,
            mergeUrl: (id1: string, id2: string) => `/api/tags/merge/${id1}/${id2}`,
            suggestionsUrl: '/api/tags/suggestions',
            extractItems: (body: unknown) => { const b = body as TagsListResponse; return { items: b.tags, pageCount: b.pageCount }; },
        },
        regions: {
            label: 'Region',
            icon: MapPin,
            listUrl: (search: string) =>
                `/api/regions/list?searchQuery=${encodeURIComponent(search)}`,
            createUrl: '/api/regions/new',
            createBody: (name: string) => ({ name }),
            renameUrl: (id: string) => `/api/regions/update/${id}`,
            renameBody: (name: string) => ({ name }),
            deleteUrl: (id: string) => `/api/regions/delete/${id}`,
            mergeUrl: (id1: string, id2: string) => `/api/regions/merge/${id1}/${id2}`,
            suggestionsUrl: '/api/regions/suggestions',
            extractItems: (body: unknown) => ({ items: body as FlatListResponse, pageCount: 1 }),
        },
        resourceTypes: {
            label: 'Resource Type',
            icon: Layers,
            listUrl: (search: string) =>
                `/api/resources/types/list?search=${encodeURIComponent(search)}`,
            createUrl: '/api/resources/types/new',
            createBody: (name: string) => ({ name }),
            renameUrl: (id: string) => `/api/resources/types/rename/${id}`,
            renameBody: (name: string) => ({ name }),
            deleteUrl: (id: string) => `/api/resources/types/delete/${id}`,
            mergeUrl: (id1: string, id2: string) => `/api/resources/types/merge/${id1}/${id2}`,
            suggestionsUrl: '/api/resources/types/suggestions',
            extractItems: (body: unknown) => ({ items: body as FlatListResponse, pageCount: 1 }),
        },
    };

    const cfg = $derived(config[type]);

    type MergeSuggestion = { id1: string; name1: string; id2: string; name2: string; score: number; };

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

    let mergeSource = $state<ListItem | null>(null);
    let submitting = $state(false);

    async function fetchItems(page = currentPage) {
        loading = true;

        try {
            const result = await api.get(cfg.listUrl(search, page));
            const { items: fetched, pageCount: pc } = cfg.extractItems(result.body);

            items = fetched;
            pageCount = pc;
            totalCount = fetched.length;
        } finally {
            loading = false;
        }
    }

    async function fetchSuggestions() {
        suggestionsLoading = true;

        try {
            const result = await api.get<MergeSuggestion[]>(cfg.suggestionsUrl);

            suggestions = result.body;
        } finally {
            suggestionsLoading = false;
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
        if (!creatingName.trim()) return;
        submitting = true;
        try {
            await api.put(cfg.createUrl, cfg.createBody(creatingName.trim()));
            creatingName = '';
            creating = false;
            await fetchItems();
            await fetchSuggestions();
            toast.success(`${cfg.label} created.`);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : `Failed to create ${cfg.label}.`);
        } finally {
            submitting = false;
        }
    }

    async function renameItem() {
        if (!editingId) return;
        const original = items.find(i => i.id === editingId)?.name;
        if (editingName.trim() === original) { editingId = null; return; }
        submitting = true;
        try {
            const url = cfg.renameUrl(editingId, editingName.trim());
            const body = cfg.renameBody?.(editingName.trim());
            await (body ? api.patch(url, body) : api.patch(url));
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
            await api.delete(cfg.deleteUrl(item.id));
            await fetchItems();
            await fetchSuggestions();
            toast.success(`${cfg.label} deleted.`);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : `Failed to delete ${cfg.label}.`);
        }
    }

    async function mergeItems(target: ListItem) {
        if (!mergeSource) return;
        const ok = await confirm(`Merge "${mergeSource.name}" into "${target.name}"? This cannot be undone.`);
        if (!ok) return;
        try {
            await api.patch(cfg.mergeUrl(target.id, mergeSource.id));
            mergeSource = null;
            await fetchItems();
            await fetchSuggestions();
            toast.success(`${cfg.label}s merged.`);
        } catch (e) {
            toast.error(e instanceof Error ? e.message : `Failed to merge.`);
        }
    }

    $effect(() => {
        fetchItems(currentPage);
        fetchSuggestions();
    });
</script>

<div class="flex flex-1 h-full overflow-hidden">
    <!-- List -->
    <div class="flex flex-col flex-1 p-4 gap-4 overflow-hidden min-w-0">
        <!-- Header -->
        <div class="flex flex-col gap-1">
            <div class="flex items-center justify-between shrink-0 gap-5">
                <div class="flex flex-1 items-center gap-2 border border-input rounded-md bg-background px-3 focus-within:ring-2 focus-within:ring-ring/50 focus-within:border-ring">
                    <Search size={16} class="text-muted-foreground shrink-0" />

                    <input
                        class="flex-1 py-1.5 text-sm bg-transparent outline-none placeholder:text-muted-foreground"
                        bind:value={search}
                        oninput={debouncedSearch}
                        placeholder="Search {cfg.label.toLowerCase()}s..."
                    />
                </div>

                <Button onclick={() => creating = true}><Plus size={14} class="shrink-0" /> New {cfg.label}</Button>
            </div>
            <span class="pl-3 text-sm text-muted-foreground">{totalCount} entries</span>
        </div>

        <!-- Table -->
        <div class="flex-1 overflow-y-auto">
            {#if loading}
                <div class="flex h-full items-center justify-center">
                    <Spinner class="w-8 h-8" />
                </div>
            {:else}
                <Table.Root>
                    <Table.Header>
                        <Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
                            <Table.Head>Name</Table.Head>
                            <Table.Head class="w-px"></Table.Head>
                        </Table.Row>
                    </Table.Header>

                    <Table.Body>
                        {#if creating}
                            <Table.Row>
                                <Table.Cell colspan={2} class="py-2">
                                    <div class="flex items-center gap-2">
                                        <Input
                                            bind:value={creatingName}
                                            placeholder="Name..."
                                            class="h-8"
                                            autofocus
                                            onkeydown={(e) => { if (e.key === 'Enter') createItem(); else if (e.key === 'Escape') { creating = false; creatingName = ''; } }}
                                        />
                                        <button onclick={createItem} class="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-accent-foreground transition-colors cursor-pointer"><Check size={14} /></button>
                                        <button onclick={() => { creating = false; creatingName = ''; }} class="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-accent-foreground transition-colors cursor-pointer"><X size={14} /></button>
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
                            <Table.Row class="group {mergeSource && mergeSource.id !== item.id ? 'cursor-pointer' : ''}" onclick={() => { if (mergeSource && mergeSource.id !== item.id) mergeItems(item); }}>
                                <Table.Cell class="py-2">
                                    {#if editingId === item.id}
                                        <Input
                                            bind:value={editingName}
                                            class="h-8"
                                            autofocus
                                            onkeydown={(e) => { if (e.key === 'Enter') renameItem(); else if (e.key === 'Escape') editingId = null; }}
                                        />
                                    {:else}
                                        <div class="flex items-center gap-2">
                                            <ItemIcon size={14} class="text-muted-foreground shrink-0" />
                                            <span class="text-sm {mergeSource?.id === item.id ? 'text-primary font-medium' : ''}">{item.name}</span>
                                        </div>
                                    {/if}
                                </Table.Cell>

                                <Table.Cell class="py-2 text-right">
                                    <div class="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
                                        {#if editingId === item.id}
                                            <button onclick={(e) => { e.stopPropagation(); renameItem(); }} class="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-accent-foreground transition-colors cursor-pointer"><Check size={14} /></button>
                                            <button onclick={(e) => { e.stopPropagation(); editingId = null; }} class="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-accent-foreground transition-colors cursor-pointer"><X size={14} /></button>
                                        {:else}
                                            <button onclick={(e) => { e.stopPropagation(); editingId = item.id; editingName = item.name; }} class="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-accent-foreground transition-colors cursor-pointer"><Pencil size={14} /></button>
                                            <button onclick={(e) => { e.stopPropagation(); mergeSource = mergeSource?.id === item.id ? null : item; }} class="p-1.5 rounded-md hover:bg-accent transition-colors cursor-pointer {mergeSource?.id === item.id ? 'text-primary' : 'text-muted-foreground hover:text-accent-foreground'}"><Merge size={14} /></button>
                                            <button onclick={(e) => { e.stopPropagation(); deleteItem(item); }} class="p-1.5 rounded-md hover:bg-accent text-muted-foreground hover:text-destructive transition-colors cursor-pointer"><Trash2 size={14} /></button>
                                        {/if}
                                    </div>
                                </Table.Cell>
                            </Table.Row>
                        {/each}
                    </Table.Body>
                </Table.Root>
            {/if}
        </div>

        {#if pageCount > 1}
            <Pagination.Root bind:page={currentPage} count={totalCount} perPage={50} class="shrink-0">
                {#snippet children({ pages, currentPage: cp })}
                    <Pagination.Content>
                        <Pagination.Item><Pagination.PrevButton /></Pagination.Item>
                        {#each pages as page (page.key)}
                            {#if page.type === 'ellipsis'}
                                <Pagination.Item><Pagination.Ellipsis /></Pagination.Item>
                            {:else}
                                <Pagination.Item>
                                    <Pagination.Link {page} isActive={cp === page.value} />
                                </Pagination.Item>
                            {/if}
                        {/each}
                        <Pagination.Item><Pagination.NextButton /></Pagination.Item>
                    </Pagination.Content>
                {/snippet}
            </Pagination.Root>
        {/if}
    </div>

    <!-- Suggestions panel -->
    <div class="w-96 border-l bg-background flex flex-col shrink-0 overflow-hidden">
        <div class="p-4 border-b shrink-0 flex items-center justify-between">
            <span class="font-medium text-sm">Merge Suggestions</span>
            {#if suggestions.length > 0}
                <span class="text-xs text-muted-foreground">{suggestions.length} pair{suggestions.length !== 1 ? 's' : ''}</span>
            {/if}
        </div>

        <div class="flex-1 overflow-y-auto">
            {#if suggestionsLoading}
                <div class="flex h-full items-center justify-center">
                    <Spinner class="w-6 h-6" />
                </div>
            {:else if suggestions.length === 0}
                <div class="flex h-full items-center justify-center">
                    <span class="text-sm text-muted-foreground">No suggestions.</span>
                </div>
            {:else}
                <div class="flex flex-col divide-y">
                    {#each suggestions as s (s.id1 + s.id2)}
                        <div class="flex flex-col gap-2 p-3">
                            <!-- Names + score -->
                            <div class="flex items-start justify-between gap-2">
                                <div class="flex flex-col gap-0.5 min-w-0">
                                    <span class="text-sm font-medium break-words leading-tight">{s.name1}</span>
                                    <span class="text-xs text-muted-foreground">vs</span>
                                    <span class="text-sm font-medium break-words leading-tight">{s.name2}</span>
                                </div>
                                <span class="text-xs font-medium text-muted-foreground bg-muted rounded-full px-2 py-0.5 shrink-0">{Math.round(s.score * 100)}%</span>
                            </div>
                            <!-- Merge direction buttons -->
                            <div class="flex gap-1.5">
                                <Button size="sm" variant="outline" class="flex-1 text-xs" title="Keep {s.name1}, delete {s.name2}"
                                    onclick={async () => { mergeSource = { id: s.id2, name: s.name2 }; await mergeItems({ id: s.id1, name: s.name1 }); }}>
                                    Keep first
                                </Button>
                                <Button size="sm" variant="outline" class="flex-1 text-xs" title="Keep {s.name2}, delete {s.name1}"
                                    onclick={async () => { mergeSource = { id: s.id1, name: s.name1 }; await mergeItems({ id: s.id2, name: s.name2 }); }}>
                                    Keep second
                                </Button>
                            </div>
                        </div>
                    {/each}
                </div>
            {/if}
        </div>
    </div>
</div>