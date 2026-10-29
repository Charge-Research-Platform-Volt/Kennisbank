<script lang="ts">
	import './layout.css';
	import { Toaster } from '#lib/components/ui/sonner/index.js';
	import ConfirmDialog from '#lib/components/ui/confirm-dialog.svelte';

	let { children } = $props();
</script>

<Toaster position="top-center" />
<ConfirmDialog />

{@render children()}
