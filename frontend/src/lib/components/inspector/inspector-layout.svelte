<script lang="ts">
	import Inspector from './inspector.svelte';
	import { createInspectorState } from '$lib/state/inspector.svelte';
	import { PaneGroup, Pane, Handle } from '$lib/components/ui/resizable';

	let { children } = $props();

	const insp = createInspectorState();
	let inspectorPane: ReturnType<typeof Pane>;

	$effect(() => {
		if (insp.selectedItem) inspectorPane?.expand();
		else inspectorPane?.collapse();
	});
</script>

<PaneGroup direction="horizontal" autoSaveId="inspector-layout">
	<Pane minSize={30}>
		{@render children()}
	</Pane>

	<Handle withHandle class={insp.selectedItem ? '' : 'hidden'} />

	<Pane
		bind:this={inspectorPane}
		defaultSize={0}
		minSize={20}
		maxSize={45}
		collapsible
		collapsedSize={0}
		onCollapse={() => { insp.selectedItem = null }}
	>
		<Inspector bind:this={insp.inspectorRef} bind:item={insp.selectedItem} onaftersave={insp.onAfterSave} />
	</Pane>
</PaneGroup>