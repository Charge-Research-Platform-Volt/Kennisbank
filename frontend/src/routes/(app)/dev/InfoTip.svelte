<script lang="ts">
	import * as Tooltip from '#lib/components/ui/tooltip/index.js';
	import { Info } from '@lucide/svelte';

	let { text }: { text: string | null | undefined } = $props();
</script>

{#if text}
	<Tooltip.Root>
		<Tooltip.Trigger
			class="inline-flex shrink-0 cursor-help align-middle text-muted-foreground/70 hover:text-foreground"
			aria-label={text}
		>
			<Info size={13} />
		</Tooltip.Trigger>
		<Tooltip.Content class="max-w-xs">
			<p>{text}</p>
		</Tooltip.Content>
	</Tooltip.Root>
{/if}
