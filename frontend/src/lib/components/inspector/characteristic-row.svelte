<script lang="ts">
    import * as Tooltip from "$lib/components/ui/tooltip";
    import type { Icon } from "lucide-svelte";
	import { getContext, untrack, type Snippet } from "svelte";

    let { icon: LucideIcon, label, value, href, onsave, editContent }: {
        icon: typeof Icon;
        label: string;
        value?: string;
        href?: string;
        onsave?: (value: string) => Promise<void>;
        editContent?: Snippet;
    } = $props();
    
    const getEditMode = getContext<() => boolean>('getEditMode');
    let editMode = $derived(getEditMode());

    const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

    let inputValue = $state(untrack(() => value ?? ''));
</script>

{#if value || editMode}
<div class="flex items-center gap-2 overflow-hidden">
    <Tooltip.Root>
        <Tooltip.Trigger>
            <LucideIcon size={14} class="text-muted-foreground shrink-0" />
        </Tooltip.Trigger>
        <Tooltip.Content>{label}</Tooltip.Content>
    </Tooltip.Root>
    {#if editMode}
        {#if editContent}
            {@render editContent()}
        {:else}
            <input bind:value={inputValue} onblur={() => { if (onsave) registerSave?.(onsave(inputValue)); }} placeholder={label} class="text-sm bg-transparent border-b border-transparent focus:border-border focus:outline-none transition-colors min-w-0 flex-1 py-0.5" />
        {/if}
    {:else if href}
        <a {href} title={value} target="_blank" class="text-sm truncate hover:underline min-w-0">{value}</a>
    {:else}
        <span title={value} class="text-sm truncate min-w-0">{value}</span>
    {/if}
</div>
{/if}
