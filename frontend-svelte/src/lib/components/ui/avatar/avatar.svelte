<script lang="ts">
    let { userId = '', name = '', customAvatarVersion = null, src = null, size = 32, class: className = '' }: {
        userId?: string;
        name?: string;
        customAvatarVersion?: number | null;
        src?: string | null;
        size?: number;
        class?: string;
    } = $props();

    let resolvedSrc = $derived(
        src ?? (customAvatarVersion != null ? `/api/user/current/avatar/${userId}?v=${customAvatarVersion}` : null)
    );

    let initials = $derived(
        name.split(' ').filter(Boolean).map(n => n[0]).slice(0, 2).join('').toUpperCase()
    );
</script>

{#if resolvedSrc}
    <img
        src={resolvedSrc}
        alt={name}
        title={name}
        style="width: {size}px; height: {size}px;"
        class="rounded-full object-cover ring-2 ring-background shrink-0 {className}"
    />
{:else}
    <div
        title={name}
        style="width: {size}px; height: {size}px; font-size: {size * 0.35}px;"
        class="rounded-full bg-primary/15 text-primary flex items-center justify-center font-medium ring-2 ring-background shrink-0 select-none {className}"
    >
        {initials || '?'}
    </div>
{/if}
