<script lang="ts">
	import Inspector from './inspector.svelte';
	import { createInspectorState } from '$lib/state/inspector.svelte';
	import { PaneGroup, Pane, Handle } from '$lib/components/ui/resizable';

	let { children } = $props();

	const insp = createInspectorState();
</script>

<PaneGroup direction="horizontal" autoSaveId="inspector-layout">
	<Pane minSize={30}>
		{@render children()}
	</Pane>

	{#if insp.selectedItem}
		<Handle withHandle />

		<Pane defaultSize={25} minSize={20} maxSize={45}>
			<Inspector bind:this={insp.inspectorRef} bind:item={insp.selectedItem} onaftersave={insp.onAfterSave} />
		</Pane>
	{/if}
</PaneGroup>