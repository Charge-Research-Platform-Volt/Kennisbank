<script lang="ts">
    import { Popover } from "bits-ui";
    import { debounce } from '$lib/utils/debounce';
    import { Check, ChevronDown, Search } from "@lucide/svelte";

    let {
        value = $bindable<string[]>([]),
        search,
        placeholder = 'Select...',
        class: className = '',
        onchange,
    }: {
        value: string[];
        search: (q: string) => Promise<{ id: string; name: string }[]>;
        placeholder?: string;
        class?: string;
        onchange?: () => void;
    } = $props();

    let open = $state(false);
    let options = $state<{ id: string; name: string }[]>([]);
    let searchQuery = $state('');
    async function doSearch(q: string) {
        options = await search(q);
    }

    const debouncedSearch = debounce((q: string) => doSearch(q));

    function onSearchInput(q: string) {
        searchQuery = q;
        debouncedSearch(q);
    }

    function toggle(id: string) {
        value = value.includes(id) ? value.filter(v => v !== id) : [...value, id];
        onchange?.();
    }

    $effect(() => {
        if (open) {
            searchQuery = '';
            doSearch('');
        }
    });
</script>

<Popover.Root bind:open>
    <Popover.Trigger class="flex items-center gap-1.5 h-9 text-sm border border-input rounded-md px-3 bg-background hover:bg-accent transition-colors cursor-pointer {className}">
        <span class="flex-1 truncate min-w-0 text-left">{placeholder}{value.length > 0 ? ` · ${value.length}` : ''}</span>
        <ChevronDown size={12} class="text-muted-foreground shrink-0" />
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
                        onclick={() => toggle(option.id)}
                    >
                        <div class="w-3.5 h-3.5 shrink-0 flex items-center justify-center rounded-sm border border-input {value.includes(option.id) ? 'bg-primary border-primary' : ''}">
                            {#if value.includes(option.id)}
                                <Check size={10} class="text-primary-foreground" />
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
