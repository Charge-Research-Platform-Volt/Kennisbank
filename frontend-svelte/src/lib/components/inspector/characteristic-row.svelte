<script lang="ts">
    import * as Tooltip from "$lib/components/ui/tooltip";
    import type { Icon } from "lucide-svelte";

    let { icon: LucideIcon, label, value, href }: {
        icon: typeof Icon;
        label: string;
        value?: string;
        href?: string;
    } = $props();
</script>

{#if value}
<div class="flex items-center gap-2 overflow-hidden">
    <Tooltip.Root>
        <Tooltip.Trigger>
            <LucideIcon size={14} class="text-muted-foreground shrink-0" />
        </Tooltip.Trigger>
        <Tooltip.Content>{label}</Tooltip.Content>
    </Tooltip.Root>
    {#if href}
        <a {href} title={value} target="_blank" class="text-sm truncate hover:underline min-w-0">{value}</a>
    {:else}
        <span title={value} class="text-sm truncate min-w-0">{value}</span>
    {/if}
</div>
{/if}
