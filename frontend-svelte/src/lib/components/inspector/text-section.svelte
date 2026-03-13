<script lang="ts">
    import { getContext, untrack } from "svelte";

    let { label, value, onsave }: {
        label: string;
        value?: string;
        onsave?: (value: string) => Promise<void>;
    } = $props();

    const getEditMode = getContext<() => boolean>('getEditMode');
    let editMode = $derived(getEditMode());

    const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

    let inputValue = $state(untrack(() => value ?? ''));

    let expanded = $state(false);
    let element: HTMLParagraphElement | null = $state(null);
    let isTruncated = $derived(((element as unknown as HTMLParagraphElement)?.scrollHeight ?? 0) > ((element as unknown as HTMLParagraphElement)?.clientHeight ?? 1));

    let textarea: HTMLTextAreaElement | null = $state(null);
    $effect(() => {
        if (!textarea) return;
        void inputValue;
        const scrollEl = textarea.closest('.overflow-y-auto') as HTMLElement | null;
        const scrollTop = scrollEl?.scrollTop ?? 0;
        textarea.style.height = 'auto';
        textarea.style.height = textarea.scrollHeight + 'px';
        if (scrollEl) scrollEl.scrollTop = scrollTop;
    });
</script>

{#if value || editMode}
    <div class="flex flex-col gap-1 px-3 py-3 border-b">
        <span class="text-xs font-medium text-muted-foreground">{label}</span>
        {#if editMode}
            <textarea
                bind:this={textarea}
                bind:value={inputValue}
                onblur={() => { if (onsave) registerSave?.(onsave(inputValue)); }}
                placeholder={label}
                class="text-sm bg-transparent border border-input rounded-sm px-2 py-1.5 focus:outline-none focus:border-ring resize-none overflow-hidden"
            ></textarea>
        {:else}
            <p bind:this={element} class="text-sm text-muted-foreground {expanded ? '' : 'line-clamp-4'}">{value}</p>
            {#if isTruncated || expanded}
                <button onclick={() => expanded = !expanded} class="cursor-pointer mt-1 text-xs text-muted-foreground/60 hover:text-muted-foreground transition-colors text-left">
                    {expanded ? 'Show less' : 'Show more'}
                </button>
            {/if}
        {/if}
    </div>
{/if}
