<script lang="ts">
    import { untrack } from 'svelte';
    import BadgeSection from './badge-section.svelte';

    let { label, value, onsave }: {
        label: string;
        value?: string[];
        onsave?: (value: string[]) => Promise<void>;
    } = $props();

    let aliases = $state(untrack(() => value ?? []));
</script>

<BadgeSection 
    {label} 
    items={aliases.map((a) => ({ id: a, name: a }))}
    oncreate={async (name) => ({ id: name, name })}
    onadd={async (_, name) => { aliases = [...aliases, name]; await onsave?.(aliases); }}
    onremove={async (rel) => { aliases = aliases.filter((a) => a !== rel.id); await onsave?.(aliases); }}
/>