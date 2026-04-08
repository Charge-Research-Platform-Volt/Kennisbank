<script lang="ts">
	import { api } from "$lib/api";
	import Spinner from "$lib/components/ui/spinner/spinner.svelte";
	import type { ResourceItem } from "$lib/types/resource";
	import { getFileIcon } from "$lib/utils/icons";
    import { Search, ArrowRight, X } from "lucide-svelte";
    import { onMount, getContext } from "svelte";
	import { fade, fly } from "svelte/transition";
    import { formatDate } from "$lib/utils/date";
    import { getParam, setParams } from "$lib/utils/urlState";
	import { debounce } from "$lib/utils/debounce";

    const openInspector: (item: ResourceItem) => void = getContext('openInspector');
    const debouncedSearch = debounce(search);

    let inputElement = $state<HTMLInputElement>();
    let searchBlockElement = $state<HTMLDivElement>();

    let paddingTop = $derived(searchBlockElement ? `calc(50vh - ${searchBlockElement.offsetHeight / 2}px)` : '50vh');
    
    let searchInput = $state("");

    let searched = $state(false);
    let loading = $state(false);

    let currentPage = $state(1);
    let totalCount = $state(0);
    let items = $state<ResourceItem[]>([]);

    async function fetchItems() {
        loading = true;

        try {
            const result = await api.post<{ items: ResourceItem[]; totalCount: number }>('/api/resources/grid', {
                pageIndex: currentPage,
                pageSize: 20,
                searchQuery: searchInput,
            });

            items = items.concat(result.body.items);
            totalCount = result.body.totalCount;
            currentPage++;
        } finally {
            loading = false;
        }
    }

    async function search() {
        if (!searchInput.trim()) return;

        searched = true;
        currentPage = 1;
        items = [];
        inputElement?.blur()

        setParams({ q: searchInput });

        await fetchItems();
    }

    function clearSearch() {
        searchInput = "";
        items = [];
        currentPage = 1;
        setParams({ q: null })
        inputElement?.focus();
    }

    onMount(() => {
        if (!searchInput) inputElement?.focus();

        if (getParam('q')) {
            searchInput = getParam('q');
            searched = true;
            search();
        }
    });
</script>

{#snippet result(item: ResourceItem)}
    {@const Icon = getFileIcon(item.fileType)}
    {@const snippet = item.chunks?.length ? item.chunks[0] : null}
    <button
        class="group flex w-full text-left items-start gap-4 px-4 py-4 cursor-pointer hover:bg-accent/60 transition-colors"
        onclick={() => openInspector(item)}
    >
        <div class="shrink-0 mt-0.5 rounded-md bg-muted p-2 group-hover:bg-background transition-colors">
            <Icon size={16} class="text-muted-foreground" />
        </div>
        <div class="flex flex-col gap-1 flex-1 min-w-0">
            <div class="flex items-start justify-between gap-4">
                <span class="font-semibold text-sm leading-snug">{item.name}</span>
                {#if item.publicationDate}
                    <span class="text-xs text-muted-foreground shrink-0 mt-0.5">
                        {formatDate(item.publicationDate, item.publicationDatePrecision)}
                    </span>
                {/if}
            </div>
            {#if snippet}
                <p class="text-xs text-muted-foreground line-clamp-2 mt-1 leading-relaxed">{snippet}</p>
            {/if}
        </div>
    </button>
{/snippet}

<div class="flex flex-col flex-1 h-full overflow-hidden items-center transition-[padding-top] duration-500 ease-in-out" style="padding-top: {searched ? '1rem' : paddingTop}">
    <div bind:this={searchBlockElement} class="flex flex-col items-center gap-6 w-full px-4 transition-[max-width] duration-500 ease-in-out {searched ? 'max-w-5xl' : 'max-w-2xl'}">
        {#if !searched}
            <div transition:fade={{ duration: 200 }}>
                <!-- Logo + Tagline -->
                <div class="flex flex-col items-center gap-2">
                    <div class="flex items-center gap-2">
                        <img src="/img/charge-icon.webp" alt="Charge Icon" class="w-8 h-8" />
                        <span class="text-4xl font-medium">Grid Search</span>
                    </div>
                    <p class="text-base text-muted-foreground">Search the grid for resources, people, and organisations</p>
                </div>
            </div>
        {/if}

        <!-- Search bar -->
        <div class="flex w-full items-center gap-2 border border-input rounded-4xl shadow-sm bg-background pl-3 pr-1 focus-within:ring-1 focus-within:ring-ring/20">
            <Search size={20} class="text-muted-foreground shrink-0" />
            <input
                class="flex-1 py-2.5 text-base bg-transparent outline-none placeholder:text-muted-foreground"
                bind:this={inputElement}
                bind:value={searchInput}
                oninput={() => { if (searched) debouncedSearch(); }}
                onkeydown={(e) => { if (e.key === 'Enter' && !searched) search(); }}
            />
            
            {#if searched}
                {#if searchInput.trim()}
                    <button transition:fade={{ duration: 50 }} class="rounded-full p-2 cursor-pointer" onclick={clearSearch}>
                        <X size={20} class="text-muted-foreground shrink-0" />
                    </button>
                {/if}
            {:else}
                <button transition:fade={{ duration: 50 }} class="rounded-full bg-primary p-2 cursor-pointer" onclick={search}>
                    <ArrowRight size={20} class="text-primary-foreground shrink-0" />
                </button>
            {/if}
        </div>
    </div>

    {#if searched}
        <div class="w-full px-4 mt-6 overflow-y-auto pb-10" transition:fly={{ y: 10, duration: 300, delay:200 }}>
            {#if loading && items.length === 0}
                <div class="w-full h-screen flex justify-center items-center">
                    <Spinner class="shrink-0 w-10 h-10" />
                </div>
            {:else}
                <div class="max-w-4xl mx-auto divide-y divide-border">
                    {#if items.length === 0}
                        <p class="text-sm text-muted-foreground text-center py-12">No results found.</p>
                    {:else}
                        {#each items as item (item.id)}
                            {@render result(item)}
                        {/each}
                    {/if}
                </div>

                <div class="max-w-4xl mx-auto w-full flex justify-center py-5">
                    {#if loading}
                        <Spinner class="shrink-0 w-5 h-5" />
                    {:else if items.length < totalCount && items.length > 0}
                        <button class="text-muted-foreground text-sm cursor-pointer" onclick={fetchItems}>
                            Load more
                        </button>
                    {/if}
                </div>
            {/if}
        </div>
    {/if}
</div>