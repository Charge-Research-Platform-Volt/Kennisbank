<script lang="ts">
    import { Popover } from "bits-ui";
    import { Check, ChevronDown, Search, X } from "lucide-svelte";

    let {
        value = $bindable<string | null>(null),
        displayValue = $bindable<string | null>(null),
        search,
        placeholder = 'Select...',
        class: className = '',
        onchange,
    }: {
        value: string | null;
        displayValue: string | null;
        search: (q: string) => Promise<{ id: string; name: string }[]>;
        placeholder?: string;
        class?: string;
        onchange?: (id: string | null, name: string | null) => void;
    } = $props();

    let open = $state(false);
    let options = $state<{ id: string; name: string }[]>([]);
    let searchQuery = $state('');
    let debounceTimer: ReturnType<typeof setTimeout>;

    function onSearchInput(q: string) {
        searchQuery = q;
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => search(q).then(r => options = r), 300);
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

    function clear(e: MouseEvent) {
        e.stopPropagation();
        value = null;
        displayValue = null;
        onchange?.(null, null);
    }
</script>

<Popover.Root bind:open>
    <Popover.Trigger class="flex items-center gap-1.5 h-6 text-sm border-b border-input bg-transparent px-0 cursor-pointer min-w-0 flex-1 {className}">
        <span class="flex-1 truncate min-w-0 text-left {displayValue ? '' : 'text-muted-foreground'}">{displayValue ?? placeholder}</span>
        {#if displayValue}
            <button onclick={clear} class="text-muted-foreground hover:text-foreground transition-colors shrink-0">
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
                    placeholder="Search..."
                />
            </div>
            <div class="max-h-48 overflow-y-auto">
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
            </div>
        </Popover.Content>
    </Popover.Portal>
</Popover.Root>
