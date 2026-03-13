<script lang="ts">
	import { afterNavigate } from "$app/navigation";
    import Inspector from "$lib/components/inspector/inspector.svelte";
    import type { ResourceItem, EntityType } from "$lib/types/resource";
	import { getParam, setParams } from "$lib/utils/urlState";
	import { onMount, setContext } from "svelte";

    let { children } = $props();
    
    let selectedItem = $state<ResourceItem | null>(null);
    
    function openInspector(item: ResourceItem) 
    {
        selectedItem = item;
        setParams({ inspectorId: item.id, inspectorType: item.type });
    }
    
    function closeInspector() 
    {
        selectedItem = null;
        setParams({ inspectorId: null, inspectorType: null });
    }
    
    setContext('openInspector', openInspector);
    setContext('closeInspector', closeInspector);

    let refreshTable: (() => void) | null = null;
    setContext('registerTableRefresh', (fn: () => void) => { refreshTable = fn; });
    
    let inspector: ReturnType<typeof Inspector>;
    onMount(() => 
    {
        const id = getParam('inspectorId');
        const rawType = getParam('inspectorType');
        const urlType = ['resource', 'person', 'organisation'].includes(rawType) ? rawType as EntityType : null;
        
        if (id && urlType)
            inspector.navigate({ id, type: urlType, name: ''})
        else
            selectedItem = null;
    });
    
    afterNavigate(() => 
    {
        if (!getParam('inspectorId')) closeInspector();
    });
</script>

<div class="flex h-full overflow-hidden">
    {@render children()}

    <Inspector bind:this={inspector} bind:item={selectedItem} onaftersave={() => refreshTable?.()} />
</div>