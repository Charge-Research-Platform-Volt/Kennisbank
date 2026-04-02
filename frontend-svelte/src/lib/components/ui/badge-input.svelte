<script lang="ts">
    import { X } from 'lucide-svelte';
    import { debounce } from '$lib/utils/debounce';

    let { items = $bindable<{ id: string; name: string }[]>([]), search, oncreate, placeholder = 'Search...' }: {
        items: { id: string; name: string }[];
        search: (q: string) => Promise<{ id: string; name: string }[]>;
        oncreate?: (name: string) => Promise<{ id: string; name: string } | null>;
        placeholder?: string;
    } = $props();

    let searchQuery = $state('');
    let searchResults = $state<{ id: string; name: string }[]>([]);
    let searchOpen = $state(false);

    const debouncedSearch = debounce((q: string) => {
        search(q).then(r => { searchResults = r; searchOpen = true; });
    }, 200);

    function onSearchInput(q: string) {
        searchQuery = q;
        debouncedSearch(q);
    }

    function addItem(id: string, name: string) {
        if (items.some(i => i.id === id)) return;
        items = [{ id, name }, ...items];
        searchQuery = '';
        searchResults = [];
        searchOpen = false;
    }

    async function createItem() {
        const name = searchQuery.trim();
        if (!name || !oncreate) return;
        const result = await oncreate(name);
        if (result && !items.some(i => i.id === result.id)) {
            items = [{ id: result.id, name: result.name }, ...items];
        }
        searchQuery = '';
        searchResults = [];
        searchOpen = false;
    }
</script>

<div class="flex flex-col gap-2">
    <div class="relative border-b border-transparent focus-within:border-border transition-colors pb-1">
        <input
            type="text"
            value={searchQuery}
            oninput={(e) => onSearchInput(e.currentTarget.value)}
            onfocus={() => search(searchQuery).then(r => { searchResults = r; searchOpen = true; })}
            onblur={() => setTimeout(() => { searchOpen = false; }, 150)}
            {placeholder}
            class="w-full text-sm bg-transparent outline-none placeholder:text-muted-foreground/50"
        />
        {#if searchOpen}
            {@const filtered = searchResults.filter(r => !items.some(i => i.id === r.id))}
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

    {#if items.length > 0}
        <div class="flex flex-wrap gap-1.5">
            {#each items as item (item.id)}
                <span class="flex items-center gap-1 rounded-md bg-secondary px-2 py-0.5 text-xs">
                    {item.name}
                    <button onclick={() => items = items.filter(i => i.id !== item.id)} class="cursor-pointer text-muted-foreground hover:text-foreground px-0.5 -mr-1">
                        <X size={10} />
                    </button>
                </span>
            {/each}
        </div>
    {/if}
</div>
