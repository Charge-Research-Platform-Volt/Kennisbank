<script lang="ts">
    import { Popover } from "bits-ui";
    import { debounce } from '$lib/utils/debounce';
    import { Check, ChevronDown, Search, X } from "lucide-svelte";

    let {
        value = $bindable<string | null>(null),
        displayValue = $bindable<string | null>(null),
        search,
        placeholder = 'Select...',
        class: className = '',
        variant = 'default' as 'default' | 'ghost' | 'flat',
        onchange,
        oncreate,
        allowClear = true,
    }: {
        value: string | null;
        displayValue: string | null;
        search: (q: string) => Promise<{ id: string; name: string }[]>;
        placeholder?: string;
        class?: string;
        variant?: 'default' | 'ghost' | 'flat';
        onchange?: (id: string | null, name: string | null) => void;
        oncreate?: (name: string) => Promise<{ id: string; name: string } | null>;
        allowClear?: boolean;
    } = $props();

    let open = $state(false);
    let options = $state<{ id: string; name: string }[]>([]);
    let searchQuery = $state('');
    const debouncedSearch = debounce((q: string) => search(q).then(r => options = r));

    function onSearchInput(q: string) {
        searchQuery = q;
        debouncedSearch(q);
    }

    $effect(() => {
        if (open) {
            searchQuery = '';
            search('').then(r => options = r);
        }
    });

    function select(option: { id: string; name: string }) {
        value = option.id;
        displayValue = option.name;
        open = false;
        onchange?.(option.id, option.name);
    }

    async function create() {
        if (!oncreate || !searchQuery.trim()) return;
        const result = await oncreate(searchQuery.trim());
        if (result) {
            value = result.id;
            displayValue = result.name;
            open = false;
            onchange?.(result.id, result.name);
        }
    }

    function clear(e: MouseEvent) {
        e.stopPropagation();
        value = null;
        displayValue = null;
        onchange?.(null, null);
    }
</script>

<Popover.Root bind:open>
    <Popover.Trigger class="
        {variant === 'ghost' ? 'flex items-center gap-1.5 text-sm bg-transparent cursor-pointer w-full'
        : variant === 'flat' ? 'flex items-center gap-1.5 text-sm bg-transparent cursor-pointer w-full border-b transition-colors ' + (open ? 'border-border' : 'border-transparent')
        : 'flex items-center gap-1.5 h-9 text-sm border border-input rounded-md px-3 bg-background hover:bg-accent transition-colors cursor-pointer w-full'}
        {className}">
        <span class="flex-1 truncate min-w-0 text-left {displayValue ? '' : variant === 'flat' ? 'text-muted-foreground/50' : 'text-muted-foreground'}">{displayValue ?? placeholder}</span>
        {#if displayValue && allowClear}
            <button onclick={clear} class="text-muted-foreground hover:text-foreground transition-colors shrink-0 cursor-pointer">
                <X size={12} />
            </button>
        {:else}
            <ChevronDown size={12} class="text-muted-foreground shrink-0" />
        {/if}
    </Popover.Trigger>
    <Popover.Portal>
        <Popover.Content sideOffset={4} class="z-50 w-52 rounded-md border border-border bg-popover shadow-md p-1 outline-none">
            <div class="flex items-center gap-1.5 border border-input rounded-sm px-2 mx-1 mb-1 focus-within:ring-1 focus-within:ring-ring">
                <Search size={12} class="text-muted-foreground shrink-0" />
                <input
                    class="flex-1 py-1 text-xs bg-transparent outline-none placeholder:text-muted-foreground"
                    value={searchQuery}
                    oninput={(e) => onSearchInput(e.currentTarget.value)}
                    placeholder={oncreate ? 'Search or type to create...' : 'Search...'}
                />
            </div>
            <div class="max-h-64 overflow-y-auto">
                {#each options as option (option.id)}
                    <button
                        class="w-full flex items-center gap-2 px-2 py-1.5 text-xs rounded-sm hover:bg-accent text-left cursor-pointer"
                        onclick={() => select(option)}
                    >
                        <div class="w-3.5 h-3.5 shrink-0 flex items-center justify-center">
                            {#if value === option.id}
                                <Check size={12} class="text-primary" />
                            {/if}
                        </div>
                        {option.name}
                    </button>
                {:else}
                    <p class="text-xs text-muted-foreground px-2 py-1.5">No results.</p>
                {/each}
                {#if oncreate && searchQuery.trim()}
                    <button
                        class="w-full flex items-center gap-2 px-2 py-1.5 text-xs rounded-sm hover:bg-accent text-left cursor-pointer text-muted-foreground"
                        onclick={create}
                    >
                        + Create "{searchQuery.trim()}"
                    </button>
                {/if}
            </div>
        </Popover.Content>
    </Popover.Portal>
</Popover.Root>
