<script lang="ts">
	import InspectorLayout from '$lib/components/inspector/inspector-layout.svelte';
	let { children } = $props();
</script>

<InspectorLayout>
	{@render children()}
</InspectorLayout>
