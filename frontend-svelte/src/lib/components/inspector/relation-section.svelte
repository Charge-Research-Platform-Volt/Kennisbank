<script lang="ts">
    import type { RelationItem, NavigationTarget, ResourceType } from "$lib/types/resource";
	import { getFileIcon } from "$lib/utils/icons";
    import { getContext } from "svelte";

    let { label, items, itemType }: { label: string; items: RelationItem[]; itemType?: ResourceType } = $props();
    const navigate = getContext<(target: NavigationTarget) => void>('navigate');

    let expanded = $state(false);
    const LIMIT = 3;
</script>

{#if items?.length}
    <div class="flex flex-col gap-1 px-3 py-3 border-b">
        <span class="text-xs font-medium text-muted-foreground mb-1">{label}</span>

        {#each expanded ? items : items.slice(0, LIMIT) as item (item.id)}
            {@const Icon = getFileIcon(item.fileType ?? item.authorType ?? itemType ?? '')}
            <button onclick={() => navigate({ id: item.id, name: item.name, type: (itemType ?? item.authorType as ResourceType) })} title={item.name} class="flex w-full text-left items-center gap-2 text-sm py-0.5 cursor-pointer hover:text-foreground text-muted-foreground">
                <Icon size={13} class="shrink-0" />
                <span class="flex-1 truncate">{item.name}</span>
                {#if item.role}
                    <span class="text-xs text-muted-foreground/60 shrink-0">{item.role}</span>
                {/if}
            </button>
        {/each}

        {#if items.length > LIMIT}
            <button onclick={() => expanded = !expanded} class="cursor-pointer mt-1 text-xs text-muted-foreground/60 hover:text-muted-foreground transition-colors text-left">
                {expanded ? 'Show less' : `Show ${items.length - LIMIT} more`}
            </button>
        {/if}
    </div>
{/if}
