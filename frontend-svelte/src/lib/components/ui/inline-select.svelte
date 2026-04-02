<script lang="ts">
    import { debounce } from '$lib/utils/debounce';

    let {
        value = $bindable<string | null>(null),
        displayValue = $bindable<string | null>(null),
        search,
        oncreate,
        placeholder = 'Search or create...',
    }: {
        value: string | null;
        displayValue: string | null;
        search: (q: string) => Promise<{ id: string; name: string }[]>;
        oncreate?: (name: string) => Promise<{ id: string; name: string } | null>;
        placeholder?: string;
    } = $props();

    let searchQuery = $state(displayValue ?? '');
    let results = $state<{ id: string; name: string }[]>([]);
    let open = $state(false);
    let highlightedIndex = $state(-1);

    // Sync input when displayValue changes externally (e.g. AI fill)
    $effect(() => {
        if (!open) searchQuery = displayValue ?? '';
    });

    // Reset highlight when results change
    $effect(() => {
        results;
        highlightedIndex = -1;
    });

    const debouncedSearch = debounce((q: string) => {
        search(q).then(r => { results = r; });
    }, 200);

    function onInput(q: string) {
        searchQuery = q;
        debouncedSearch(q);
    }

    function select(option: { id: string; name: string }) {
        value = option.id;
        displayValue = option.name;
        searchQuery = option.name;
        open = false;
        highlightedIndex = -1;
    }

    async function commitNew() {
        const name = searchQuery.trim();
        if (!name) return;
        if (oncreate) {
            const result = await oncreate(name);
            if (result) { value = result.id; displayValue = result.name; searchQuery = result.name; }
        } else {
            value = name;
            displayValue = name;
        }
        open = false;
        highlightedIndex = -1;
    }

    function onKeydown(e: KeyboardEvent) {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            open = true;
            highlightedIndex = Math.min(highlightedIndex + 1, results.length - 1);
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            highlightedIndex = Math.max(highlightedIndex - 1, -1);
        } else if (e.key === 'Enter') {
            e.preventDefault();
            if (highlightedIndex >= 0 && highlightedIndex < results.length) select(results[highlightedIndex]);
            else if (results.length > 0) select(results[0]);
            else if (searchQuery.trim()) commitNew();
        } else if (e.key === 'Escape') {
            open = false;
            searchQuery = displayValue ?? '';
            highlightedIndex = -1;
        }
    }

    function onFocus() {
        search(searchQuery).then(r => { results = r; open = true; });
    }

    function onBlur() {
        setTimeout(() => {
            open = false;
            highlightedIndex = -1;
            const q = searchQuery.trim();
            if (!q) {
                value = null;
                displayValue = null;
            } else {
                const exact = results.find(r => r.name.toLowerCase() === q.toLowerCase());
                if (exact) {
                    value = exact.id;
                    displayValue = exact.name;
                    searchQuery = exact.name;
                } else {
                    value = q;
                    displayValue = q;
                }
            }
        }, 150);
    }
</script>

<div class="relative border-b border-transparent focus-within:border-border transition-colors">
    <input
        type="text"
        value={searchQuery}
        oninput={(e) => onInput(e.currentTarget.value)}
        onkeydown={onKeydown}
        onfocus={onFocus}
        onblur={onBlur}
        {placeholder}
        class="w-full text-sm bg-transparent outline-none placeholder:text-muted-foreground/50"
    />
    {#if open && results.length > 0}
        <div class="absolute top-full left-0 right-0 mt-0.5 z-10 bg-popover border border-border rounded-sm shadow-md max-h-40 overflow-y-auto">
            {#each results as option, idx (option.id)}
                <button onmousedown={() => select(option)} class="w-full text-left text-xs px-2 py-1.5 cursor-pointer {idx === highlightedIndex ? 'bg-accent' : 'hover:bg-accent'}">
                    {option.name}
                </button>
            {/each}
            {#if oncreate && searchQuery.trim() && !results.some(r => r.name.toLowerCase() === searchQuery.trim().toLowerCase())}
                <button onmousedown={commitNew} class="w-full text-left text-xs px-2 py-1.5 hover:bg-accent cursor-pointer text-muted-foreground italic">
                    Create "{searchQuery.trim()}"
                </button>
            {/if}
        </div>
    {:else if open && oncreate && searchQuery.trim()}
        <div class="absolute top-full left-0 right-0 mt-0.5 z-10 bg-popover border border-border rounded-sm shadow-md">
            <button onmousedown={commitNew} class="w-full text-left text-xs px-2 py-1.5 hover:bg-accent cursor-pointer text-muted-foreground italic">
                Create "{searchQuery.trim()}"
            </button>
        </div>
    {/if}
</div>
