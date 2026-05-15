<script lang="ts">
    import { Check, CircleX } from '@lucide/svelte';

    let { password }: { password: string } = $props();

    let len = $derived(password.length >= 6);
    let upper = $derived(/[A-Z]/.test(password));
    let lower = $derived(/[a-z]/.test(password))
    let digit = $derived(/[0-9]/.test(password));
    let special = $derived(/[^a-zA-Z0-9]/.test(password));
</script>

{#snippet condition(name: string, met: boolean)}
    <div class="flex items-center gap-1.5 text-xs {met ? 'text-green-500' : 'text-muted-foreground'}">
        {#if met}
            <Check size={12} />
        {:else}
            <CircleX size={12} />
        {/if}

        {name}
    </div>
{/snippet}

{#if password.length > 0}
    <div class="flex flex-col gap-1 mt-1">
        {@render condition('At least 6 characters', len)}
        {@render condition('At least one uppercase letter', upper)}
        {@render condition('At least one lowercase letter', lower)}
        {@render condition('At least one digit', digit)}
        {@render condition('At least one special character', special)}
    </div>
{/if}