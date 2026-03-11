<script lang="ts">
    let { label, value }: { label: string; value: string } = $props();

    let expanded = $state(false);
    let element: HTMLParagraphElement | null = $state(null);
    let isTruncated = $derived(((element as unknown as HTMLParagraphElement)?.scrollHeight ?? 0) > ((element as unknown as HTMLParagraphElement)?.clientHeight ?? 1));
</script>

{#if value}
    <div class="flex flex-col gap-1 px-3 py-3 border-b">
        <span class="text-xs font-medium text-muted-foreground">{label}</span>
        <p bind:this={element} class="text-sm text-muted-foreground {expanded ? '' : 'line-clamp-4'}">{value}</p>
        {#if isTruncated || expanded}
            <button onclick={() => expanded = !expanded} class="mt-1 text-xs text-muted-foreground/60 hover:text-muted-foreground transition-colors text-left">
                {expanded ? 'Show less' : 'Show more'}
            </button>
        {/if}
    </div>
{/if}
