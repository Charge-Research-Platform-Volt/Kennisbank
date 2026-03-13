<script lang="ts">
    import { api } from "$lib/api";
    import type { ResourceItem, TrashItem } from "$lib/types/resource";
    import { userState } from "$lib/state/user.svelte";
    import Spinner from "$lib/components/ui/spinner/spinner.svelte";
    import { RotateCcw, ArrowLeft } from "lucide-svelte";
    import * as Table from "$lib/components/ui/table";
    import { getFileIcon } from "$lib/utils/icons";
    import { formatDate } from "$lib/utils/date";
	import { getContext } from "svelte";

    let items = $state<TrashItem[]>([]);
    let loading = $state(false);
    
    const openInspector: (item: ResourceItem) => void = getContext('openInspector');
    const closeInspector: () => void = getContext('closeInspector');
    
    async function fetchItems() 
    {
        loading = true;
        
        try 
        {
            const result = await api.get<TrashItem[]>('/api/resources/trash-grid');
            items = result.body;
        }
        finally 
        {
            loading = false;
        }
    }
    
    async function restore(item: TrashItem) 
    {
        await api.patch(`/api/${item.type}s/untrash/${encodeURIComponent(item.id)}`);
        items = items.filter(i => i.id !== item.id);
        closeInspector();
    }
    
    // Fetch items on page load
    fetchItems();
</script>

<div class="flex flex-col h-full flex-1 overflow-hidden p-4 gap-4">
    <!-- Header -->
    <div class="flex items-center gap-3">
        <a href="/library" class="text-muted-foreground hover:text-foreground transition-colors">
            <ArrowLeft size={16} />
        </a>
        <h1 class="text-lg font-semibold">Trash</h1>
    </div>
    
    {#if userState.loading}
        <div class="flex w-full h-full justify-center items-center">
            <Spinner size={32} />
        </div>
    {:else if userState.role !== 'admin'}
        <div class="flex flex-col items-center justify-center h-full gap-2 text-muted-foreground">
            <p class="text-sm">You don't have permission to view this page.</p>
        </div>
    {:else if loading}
        <div class="flex w-full h-full justify-center items-center">
            <Spinner size={32} />
        </div>
    {:else if items.length === 0}
        <div class="flex w-full h-full justify-center items-center text-muted-foreground">
            <p class="text-sm">Trash is empty.</p>
        </div>
    {:else}
        <Table.Root>
            <Table.Header>
                <tr class="border-b">
                    <Table.Head class="w-full">Name</Table.Head>
                    <Table.Head class="w-px whitespace-nowrap text-center">Trashed on</Table.Head>
                    <Table.Head class="w-px"></Table.Head>
                </tr>
            </Table.Header>
            <Table.Body>
                {#each items as item (item.id)}
                    {@const Icon = getFileIcon(item.fileType)}
                    <Table.Row class="cursor-pointer" onclick={() => openInspector({ ...item, trashed: true }) }>
                        <Table.Cell class="py-3 flex gap-3">
                            <Icon size={16} class="text-muted-foreground shrink-0" />
                            {item.name}
                        </Table.Cell>
                        <Table.Cell class="whitespace-nowrap text-center px-5 py-3">{formatDate(item.trashDate)}</Table.Cell>
                        <Table.Cell class="p-3 text-center flex items-center">
                            <button class="cursor-pointer text-muted-foreground hover:text-foreground transition-colors" title="Restore" onclick={(e) => { e.stopPropagation(); restore(item); }}>
                                <RotateCcw size={16} />
                            </button>
                        </Table.Cell>
                    </Table.Row>
                {/each}
            </Table.Body>
        </Table.Root>
    {/if}
</div>