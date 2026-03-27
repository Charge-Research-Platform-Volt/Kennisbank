<script lang="ts">
    import type { RelationItem, NavigationTarget, EntityType } from "$lib/types/resource";
    import { getFileIcon } from "$lib/utils/icons";
    import { getContext, untrack } from "svelte";
    import { X } from "lucide-svelte";
    import { debounce } from '$lib/utils/debounce';

    let { label, items, itemType, search, onadd, oncreate, onremove, hasRole = false, onupdaterole }: {
        label: string;
        items: RelationItem[];
        itemType?: EntityType;
        search?: (q: string) => Promise<{ id: string; name: string }[]>;
        onadd?: (id: string, name: string) => Promise<void>;
        oncreate?: (name: string) => Promise<{ id: string; name: string } | null>;
        onremove?: (item: RelationItem) => Promise<void>;
        hasRole?: boolean;
        onupdaterole?: (item: RelationItem, newRole: string) => Promise<void>;
    } = $props();

    const navigate = getContext<(target: NavigationTarget) => void>('navigate');
    const getEditMode = getContext<() => boolean>('getEditMode');
    let editMode = $derived(getEditMode());
    const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

    let localItems = $state<RelationItem[]>(untrack(() => items.map(i => ({ ...i }))));

    let expanded = $state(false);
    const LIMIT = 3;

    let searchQuery = $state('');
    let searchResults = $state<{ id: string; name: string }[]>([]);
    let searchOpen = $state(false);
    const debouncedSearch = debounce((q: string) => {
        search?.(q).then(r => { searchResults = r; searchOpen = true; });
    }, 200);

    function onSearchInput(q: string) {
        searchQuery = q;
        debouncedSearch(q);
    }

    function addItem(id: string, name: string) {
        if (localItems.some(i => i.id === id)) return;
        localItems = [{ id, name, fileType: itemType ?? '', authorType: '' }, ...localItems];
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
            if (result && !localItems.some(i => i.id === result.id)) {
                localItems = [{ id: result.id, name: result.name, fileType: itemType ?? '', authorType: '' }, ...localItems];
                await onadd?.(result.id, result.name);
            }
        })();
        registerSave?.(p);
        searchQuery = '';
        searchResults = [];
        searchOpen = false;
    }

    function removeItem(item: RelationItem) {
        localItems = localItems.filter(i => i.id !== item.id);
        registerSave?.(onremove!(item));
    }
</script>

{#if localItems.length || editMode}
    <div class="flex flex-col gap-1 px-3 py-3 border-b">
        <span class="text-xs font-medium text-muted-foreground mb-1">{label}</span>

        {#each editMode ? localItems : expanded ? localItems : localItems.slice(0, LIMIT) as relItem (relItem.id)}
            {@const Icon = getFileIcon(relItem.fileType ?? relItem.authorType ?? itemType ?? '')}
            <div class="flex items-start gap-2 text-sm py-0.5 min-w-0">
                {#if editMode && onremove}
                    <button onclick={() => removeItem(relItem)} class="text-muted-foreground/60 hover:text-destructive transition-colors shrink-0 cursor-pointer mt-0.5">
                        <X size={12} />
                    </button>
                {/if}
                <div class="flex-1 min-w-0">
                    <button
                        onclick={() => !editMode && navigate({ id: relItem.id, name: relItem.name, type: (itemType ?? relItem.authorType as EntityType) })}
                        title={relItem.name}
                        class="flex w-full min-w-0 items-center gap-2 text-muted-foreground text-left {editMode ? 'cursor-default' : 'cursor-pointer hover:text-foreground'}"
                    >
                        <Icon size={13} class="shrink-0" />
                        <span class="truncate">{relItem.name}</span>
                    </button>
                    {#if hasRole}
                        {#if editMode && onupdaterole}
                            <input
                                value={relItem.role ?? relItem.relation ?? ''}
                                onblur={(e) => {
                                    const newRole = e.currentTarget.value;
                                    relItem.role = newRole;
                                    registerSave?.(onupdaterole(relItem, newRole));
                                }}
                                placeholder="Role"
                                class="text-xs text-muted-foreground/60 bg-transparent border-b border-input focus:outline-none focus:border-ring w-full pl-[21px] mt-0.5"
                            />
                        {:else if relItem.role ?? relItem.relation}
                            <span class="text-xs text-muted-foreground/60 pl-[21px] block">{relItem.role ?? relItem.relation}</span>
                        {/if}
                    {/if}
                </div>
            </div>
        {/each}

        {#if !editMode && localItems.length > LIMIT}
            <button onclick={() => expanded = !expanded} class="cursor-pointer mt-1 text-xs text-muted-foreground/60 hover:text-muted-foreground transition-colors text-left">
                {expanded ? 'Show less' : `Show ${localItems.length - LIMIT} more`}
            </button>
        {/if}

        {#if editMode && (search || oncreate)}
            <div class="relative mt-1">
                <input
                    type="text"
                    value={searchQuery}
                    oninput={(e) => onSearchInput(e.currentTarget.value)}
                    onfocus={() => search?.(searchQuery).then(r => { searchResults = r; searchOpen = true; })}
                    onblur={() => setTimeout(() => { searchOpen = false; }, 150)}
                    placeholder="Search to add..."
                    class="w-full text-xs bg-transparent border border-input rounded-sm px-2 py-1 focus:outline-none focus:border-ring placeholder:text-muted-foreground"
                />
                {#if searchOpen}
                    {@const filtered = searchResults.filter(r => !localItems.some(i => i.id === r.id))}
                    {@const showCreate = !!oncreate && !!searchQuery.trim() && !searchResults.some(r => r.name.toLowerCase() === searchQuery.trim().toLowerCase())}
                    {#if filtered.length || showCreate}
                        <div class="absolute top-full left-0 right-0 mt-0.5 z-10 bg-popover border border-border rounded-sm shadow-md max-h-40 overflow-y-auto">
                            {#each filtered as result (result.id)}
                                <button onmousedown={() => addItem(result.id, result.name)} class="w-full text-left text-xs px-2 py-1.5 hover:bg-accent cursor-pointer">
                                    {result.name}
                                </button>
                            {:else}
                                {#if !showCreate}
                                    <p class="text-xs text-muted-foreground px-2 py-1.5">No results.</p>
                                {/if}
                            {/each}
                            {#if showCreate}
                                <button onmousedown={createItem} class="w-full text-left text-xs px-2 py-1.5 hover:bg-accent cursor-pointer text-muted-foreground italic">
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
