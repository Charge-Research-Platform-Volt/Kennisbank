<script lang="ts">
	import './layout.css';
	import favicon from '$lib/assets/favicon.svg';
	import { Toaster } from '$lib/components/ui/sonner';
	import ConfirmDialog from '$lib/components/ui/confirm-dialog.svelte';

	let { children } = $props();
</script>

<Toaster position="top-center" />
<ConfirmDialog />

<svelte:head><link rel="icon" href={favicon} /></svelte:head>
{@render children()}
