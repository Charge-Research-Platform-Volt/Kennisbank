<script lang="ts">
    import ManageList from "./ManageList.svelte";
    import ManageUsers from "./ManageUsers.svelte";
    
    const tabs = [
        { id: 'tags', label: 'Tags' },
        { id: 'regions', label: 'Regions' },
        { id: 'resourceTypes', label: 'Resource Types' },
        { id: 'journals', label: 'Journals' },
        { id: 'persons', label: 'Persons' },
        { id: 'organisations', label: 'Organisations' },
        { id: 'users', label: 'Users' },
    ] as const;

    type TabId = typeof tabs[number]['id'];
    let activeTab = $state<TabId>('tags');
</script>

<div class="flex flex-col flex-1 h-full overflow-hidden">
    <div class="flex border-b px-4 shrink-0">
        {#each tabs as tab (tab.id)}
            <button
                onclick={() => activeTab = tab.id}
                class="px-4 py-3 text-sm -mb-px border-b-2 transistion-colors cursor-pointer
                    {activeTab === tab.id
                    ? 'border-primary font-medium text-foreground'
                    : 'border-transparent text-muted-foreground hover:text-foreground'}"
            >
                {tab.label}
            </button>
        {/each}
    </div>

    <div class="flex flex-col flex-1 overflow-hidden">
        {#if activeTab === 'users'}
            <ManageUsers />
        {:else}
            <ManageList type={activeTab} />
        {/if}
    </div>
</div>