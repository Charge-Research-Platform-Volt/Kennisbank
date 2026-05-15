<script lang="ts">
	import * as Tooltip from '$lib/components/ui/tooltip';
	import type { LucideIcon } from '@lucide/svelte';
	import { getContext, untrack, type Snippet } from 'svelte';

	let {
		icon: IconComponent,
		label,
		value,
		href,
		onsave,
		editContent
	}: {
		icon: LucideIcon;
		label: string;
		value?: string;
		href?: string;
		onsave?: (value: string) => Promise<void>;
		editContent?: Snippet;
	} = $props();

	const getEditMode = getContext<() => boolean>('getEditMode');
	let editMode = $derived(getEditMode());

	const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

	let inputValue = $state(untrack(() => value ?? ''));
</script>

{#if value || editMode}
	<div class="flex items-center gap-2 overflow-hidden">
		<Tooltip.Root>
			<Tooltip.Trigger>
				<IconComponent size={14} class="shrink-0 text-muted-foreground" />
			</Tooltip.Trigger>
			<Tooltip.Content>{label}</Tooltip.Content>
		</Tooltip.Root>
		{#if editMode}
			{#if editContent}
				{@render editContent()}
			{:else}
				<input
					bind:value={inputValue}
					onblur={() => {
						if (onsave) registerSave?.(onsave(inputValue));
					}}
					placeholder={label}
					class="min-w-0 flex-1 border-b border-transparent bg-transparent py-0.5 text-sm transition-colors focus:border-border focus:outline-none"
				/>
			{/if}
		{:else if href}
			<a {href} title={value} target="_blank" class="min-w-0 truncate text-sm hover:underline"
				>{value}</a
			>
		{:else}
			<span title={value} class="min-w-0 truncate text-sm">{value}</span>
		{/if}
	</div>
{/if}
