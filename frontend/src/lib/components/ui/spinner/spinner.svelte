<script lang="ts">
	import { cn } from '#lib/utils.js';
	import LoaderCircleIcon from '@lucide/svelte/icons/loader-circle';
	import type { ComponentProps } from 'svelte';

	let { class: className, ...restProps }: ComponentProps<typeof LoaderCircleIcon> = $props();
</script>

<LoaderCircleIcon
	role="status"
	aria-label="Loading"
	class={cn('size-4 animate-spin', className)}
	{...restProps}
/>
