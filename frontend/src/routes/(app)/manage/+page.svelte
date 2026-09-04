<script lang="ts">
	import ManageList from './ManageList.svelte';
	import ManageUsers from './ManageUsers.svelte';

	const tabs = [
		{ id: 'tag', label: 'Tags' },
		{ id: 'region', label: 'Regions' },
		{ id: 'resourcetype', label: 'Resource Types' },
		{ id: 'journal', label: 'Journals' },
		{ id: 'person', label: 'Persons' },
		{ id: 'organisation', label: 'Organisations' },
		{ id: 'user', label: 'Users' }
	] as const;

	type TabId = (typeof tabs)[number]['id'];
	let activeTab = $state<TabId>('tag');
</script>

<div class="flex h-full flex-1 flex-col overflow-hidden">
	<div class="flex shrink-0 border-b px-4">
		{#each tabs as tab (tab.id)}
			<button
				onclick={() => (activeTab = tab.id)}
				class="transistion-colors -mb-px cursor-pointer border-b-2 px-4 py-3 text-sm
                    {activeTab === tab.id
					? 'border-primary font-medium text-foreground'
					: 'border-transparent text-muted-foreground hover:text-foreground'}"
			>
				{tab.label}
			</button>
		{/each}
	</div>

	<div class="flex flex-1 flex-col overflow-hidden">
		{#if activeTab === 'user'}
			<ManageUsers />
		{:else}
			<ManageList type={activeTab} />
		{/if}
	</div>
</div>
