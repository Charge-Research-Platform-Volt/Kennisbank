<script lang="ts">
    import { userState } from '$lib/state/user.svelte';

    let { children } = $props();
</script>

{#if userState.isOwner}
    {@render children()}
{:else}
    <div class="flex h-full items-center justify-center text-sm text-muted-foreground">
        Page not found
    </div>
{/if}