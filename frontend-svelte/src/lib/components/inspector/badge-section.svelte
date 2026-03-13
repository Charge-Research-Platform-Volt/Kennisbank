<script lang="ts">
    import type { RelationItem } from "$lib/types/resource";
    import { getContext, untrack } from "svelte";
    import { X } from "lucide-svelte";

    let { label, items, search, onadd, oncreate, onremove }: {
        label: string;
        items: RelationItem[];
        search?: (q: string) => Promise<{ id: string; name: string }[]>;
        onadd?: (id: string, name: string) => Promise<void>;
        oncreate?: (name: string) => Promise<{ id: string; name: string } | null>;
        onremove?: (item: RelationItem) => Promise<void>;
    } = $props();

    const getEditMode = getContext<() => boolean>('getEditMode');
    let editMode = $derived(getEditMode());
    const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

    let localItems = $state<RelationItem[]>(untrack(() => items.map(i => ({ ...i }))));

    let searchQuery = $state('');
    let searchResults = $state<{ id: string; name: string }[]>([]);
    let searchOpen = $state(false);
    let debounceTimer: ReturnType<typeof setTimeout>;

    function onSearchInput(q: string) {
        searchQuery = q;
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            search?.(q).then(r => { searchResults = r; searchOpen = true; });
        }, 200);
    }

    function addItem(id: string, name: string) {
        if (localItems.some(i => i.id === id)) return;
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
            if (result && !localItems.some(i => i.id === result.id)) {
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
        localItems = localItems.filter(i => i.id !== item.id);
        registerSave?.(onremove!(item));
    }
</script>

{#if localItems.length || editMode}
    <div class="flex flex-col gap-2 px-3 py-3 border-b">
        <span class="text-xs font-medium text-muted-foreground">{label}</span>

        {#if localItems.length}
            <div class="flex flex-wrap gap-1">
                {#each localItems as item (item.id)}
                    {#if editMode && onremove}
                        <span class="flex items-center gap-1 text-xs pl-2 pr-1 py-0.5 rounded-full bg-muted border border-border text-muted-foreground">
                            {item.name}
                            <button onclick={() => removeItem(item)} class="cursor-pointer hover:text-foreground transition-colors">
                                <X size={10} />
                            </button>
                        </span>
                    {:else}
                        <span class="text-xs px-2 py-0.5 rounded-full bg-muted border border-border text-muted-foreground">{item.name}</span>
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
                    onfocus={() => search?.(searchQuery).then(r => { searchResults = r; searchOpen = true; })}
                    onblur={() => setTimeout(() => { searchOpen = false; }, 150)}
                    placeholder="Search to add..."
                    class="w-full text-xs bg-transparent border border-input rounded-sm px-2 py-1 focus:outline-none focus:border-ring placeholder:text-muted-foreground"
                />
                {#if searchOpen}
                    {@const filtered = searchResults.filter(r => !localItems.some(i => i.id === r.id))}
                    {@const showCreate = !!oncreate && !!searchQuery.trim() && !searchResults.some(r => r.name.toLowerCase() === searchQuery.trim().toLowerCase())}
                    {#if filtered.length || showCreate}
                        <div class="absolute top-full left-0 right-0 mt-0.5 z-10 bg-popover border border-border rounded-sm shadow-md max-h-36 overflow-y-auto">
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
