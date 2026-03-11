<script lang="ts">
    import type { RelationItem } from "$lib/types/resource";

    let { label, items }: { label: string; items: RelationItem[] } = $props();
</script>

{#if items?.length}
    <div class="flex flex-col gap-2 px-3 py-3 border-b">
        <span class="text-xs font-medium text-muted-foreground">{label}</span>
        <div class="flex flex-wrap gap-1">
            {#each items as item (item.id)}
                <span class="text-xs px-2 py-0.5 rounded-full bg-muted border border-border text-muted-foreground">{item.name}</span>
            {/each}
        </div>
    </div>
{/if}
