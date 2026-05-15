<script lang="ts">
    import { ArrowUp, CircleX } from '@lucide/svelte';
    // import Switch from '$lib/components/ui/switch/switch.svelte';
    // import Label from '../ui/label/label.svelte';

    let { onSend, onStop, loading = false }: {
        onSend: (message: string) => void;
        onStop: () => void;
        loading?: boolean;
    } = $props();

    let message = $state('');
    let textarea = $state<HTMLTextAreaElement | null>(null);

    export function focus() {
        textarea?.focus();
    }

    function handleSend() {
        if (!message.trim() || loading) return;
        onSend(message);
        message = '';
    }
</script>

<div class="border border-input rounded-lg bg-background shadow-sm">
    <textarea
        bind:this={textarea}
        class="w-full px-4 pt-3 pb-2 text-sm bg-transparent outline-none resize-none placeholder:text-muted-foreground"
        placeholder={loading ? "Thinking..." : "I would like to know..."}
        rows={3}
        bind:value={message}
        onkeydown={(e) => { if (e.key === 'Enter' && !e.shiftKey && !loading) { e.preventDefault(); handleSend(); }}}
        disabled={loading}
    ></textarea>

    <div class="flex items-center justify-between px-3 pb-2">
        <!-- Toolbar -->
        <div class="flex items-center gap-2 px-3 pb-3">
            <!-- <Switch bind:checked={contentBased} id="kb-toggle" />
            <Label for="kb-toggle" class="text-xs text-muted-foreground cursor-pointer">Library Only</Label> -->
        </div>

        {#if loading}
            <button class="cursor-pointer bg-foreground text-background rounded-md p-1.5 disabled:opacity-40 hover:bg-foreground/90" onclick={onStop}>
                <CircleX size={18} />
            </button>
        {:else}
            <button class="cursor-pointer bg-primary text-primary-foreground rounded-md p-1.5 hover:bg-primary/90 disabled:opacity-40 disabled:cursor-not-allowed" onclick={handleSend} disabled={!message.trim()}>
                <ArrowUp size={18} />
            </button>
        {/if}
    </div>
</div>