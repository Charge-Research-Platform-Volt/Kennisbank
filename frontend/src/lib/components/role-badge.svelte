<script lang="ts">
    let {
        role = $bindable<string>('subject'),
        editable = false,
        onchange
    }: {
        role?: string;
        editable?: boolean;
        onchange?: (newRole: string) => void;
    } = $props();

    function toggle() {
        const newRole = role === 'production' ? 'subject' : 'production';
        role = newRole;
        onchange?.(newRole);
    }

    const label = (r: string) => r === 'production' ? 'Production' : 'Subject';
    const colorClass = (r: string) => r === 'production'
        ? 'bg-sky-500/15 text-sky-400'
        : 'bg-violet-500/15 text-violet-400';
</script>

{#if editable}
    <button onclick={toggle} title="Click to toggle role" class="cursor-pointer rounded px-1.5 py-0.5 text-xs font-medium transition-opacity hover:opacity-70 {colorClass(role ?? 'subject')}">
        {label(role ?? 'subject')}
    </button>
{:else if role}
    <span class="rounded px-1.5 py-0.5 text-xs font-medium {colorClass(role)}">
        {label(role)}
    </span>
{/if}