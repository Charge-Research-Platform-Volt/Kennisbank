<script lang="ts">
	import Inspector from '$lib/components/inspector/inspector.svelte';
	import ProjectChatSidebar from '$lib/components/chatbot/project-chat-sidebar.svelte';
	import { createInspectorState } from '$lib/state/inspector.svelte';
	import { setContext } from 'svelte';
	import { page } from '$app/state';
	import { PaneGroup, Pane, Handle } from '$lib/components/ui/resizable';
	import { sidebar } from '$lib/state/sidebar.svelte';

	let { children } = $props();

	const insp = createInspectorState();
	let chatOpen = $state(false);

	function toggleChat() {
		chatOpen = !chatOpen;
	}

	setContext('toggleChat', toggleChat);

	let prevBothOpen = false;
	$effect(() => {
		const bothOpen = !!insp.selectedItem && chatOpen;
		if (bothOpen && !prevBothOpen) sidebar.open = false;
		prevBothOpen = bothOpen;
	});
</script>

<PaneGroup direction="horizontal" autoSaveId="project-layout">
	<Pane order={0} minSize={20}>
		{@render children()}
	</Pane>

	{#if insp.selectedItem}
		<Handle withHandle />
		<Pane id="inspector-pane" order={1} defaultSize={25} minSize={20} maxSize={45}>
			<Inspector
				bind:this={insp.inspectorRef}
				bind:item={insp.selectedItem}
				onaftersave={insp.onAfterSave}
			/>
		</Pane>
	{/if}

	{#if chatOpen}
		<Handle withHandle />
		<Pane id="chat-pane" order={2} defaultSize={25} minSize={20} maxSize={45}>
			<ProjectChatSidebar projectId={page.params.id!} onClose={() => (chatOpen = false)} />
		</Pane>
	{/if}
</PaneGroup>
