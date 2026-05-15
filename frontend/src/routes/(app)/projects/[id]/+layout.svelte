<script lang="ts">
	import { afterNavigate } from '$app/navigation';
	import Inspector from '$lib/components/inspector/inspector.svelte';
	import ProjectChatSidebar from '$lib/components/chatbot/project-chat-sidebar.svelte';
	import type { ResourceItem, EntityType } from '$lib/types/resource';
	import { getParam, setParams } from '$lib/utils/urlState';
	import { onMount, setContext } from 'svelte';
	import { page } from '$app/state';

	let { children } = $props();

	let selectedItem = $state<ResourceItem | null>(null);
	let inspector: ReturnType<typeof Inspector>;
	let refreshFn: (() => void) | null = null;
	let chatOpen = $state(false);

	function openInspector(item: ResourceItem) {
		chatOpen = false;
		selectedItem = item;
		setParams({ inspectorId: item.id, inspectorType: item.type });
	}

	function closeInspector() {
		selectedItem = null;
		setParams({ inspectorId: null, inspectorType: null });
	}

	function toggleChat() {
		chatOpen = !chatOpen;
		if (chatOpen) closeInspector();
	}

	setContext('openInspector', openInspector);
	setContext('closeInspector', closeInspector);
	setContext('registerRefresh', (fn: () => void) => (refreshFn = fn));
	setContext('toggleChat', toggleChat);

	onMount(() => {
		const id = getParam('inspectorId');
		const rawType = getParam('inspectorType');
		const urlType = ['resource', 'person', 'organisation'].includes(rawType)
			? (rawType as EntityType)
			: null;

		if (id && urlType) inspector.navigate({ id, type: urlType, name: '' });
		else selectedItem = null;
	});

	afterNavigate(() => {
		if (!getParam('inspectorId')) closeInspector();
	});
</script>

<div class="flex h-full overflow-hidden">
	<div class="min-w-0 flex-1 overflow-hidden">
		{@render children()}
	</div>

	<Inspector bind:this={inspector} bind:item={selectedItem} onaftersave={() => refreshFn?.()} />

	<div class="h-full shrink-0 overflow-hidden transition-all duration-300 {chatOpen ? 'w-[520px]' : 'w-0'}">
		<ProjectChatSidebar projectId={page.params.id!} onClose={() => (chatOpen = false)} />
	</div>
</div>
