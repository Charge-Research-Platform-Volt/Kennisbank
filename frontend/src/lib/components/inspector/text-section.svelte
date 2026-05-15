<script lang="ts">
	import { getContext, untrack } from 'svelte';

	let {
		label,
		value,
		onsave
	}: {
		label: string;
		value?: string;
		onsave?: (value: string) => Promise<void>;
	} = $props();

	const getEditMode = getContext<() => boolean>('getEditMode');
	let editMode = $derived(getEditMode());

	const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

	let inputValue = $state(untrack(() => value ?? ''));

	let expanded = $state(false);
	let element: HTMLParagraphElement | null = $state(null);
	let isTruncated = $derived(
		((element as unknown as HTMLParagraphElement)?.scrollHeight ?? 0) >
			((element as unknown as HTMLParagraphElement)?.clientHeight ?? 1)
	);

	let textarea: HTMLTextAreaElement | null = $state(null);
	$effect(() => {
		if (!textarea) return;
		void inputValue;
		const scrollEl = textarea.closest('.overflow-y-auto') as HTMLElement | null;
		const scrollTop = scrollEl?.scrollTop ?? 0;
		textarea.style.height = 'auto';
		textarea.style.height = textarea.scrollHeight + 'px';
		if (scrollEl) scrollEl.scrollTop = scrollTop;
	});
</script>

{#if value || editMode}
	<div class="flex flex-col gap-1 border-b px-3 py-3">
		<span class="text-xs font-medium text-muted-foreground">{label}</span>
		{#if editMode}
			<textarea
				bind:this={textarea}
				bind:value={inputValue}
				onblur={() => {
					if (onsave) registerSave?.(onsave(inputValue));
				}}
				placeholder={label}
				rows={1}
				class="w-full resize-none overflow-hidden border-b border-transparent bg-transparent py-0.5 text-sm transition-colors focus:border-border focus:outline-none"
			></textarea>
		{:else}
			<p bind:this={element} class="text-sm text-muted-foreground {expanded ? '' : 'line-clamp-4'}">
				{value}
			</p>
			{#if isTruncated || expanded}
				<button
					onclick={() => (expanded = !expanded)}
					class="mt-1 cursor-pointer text-left text-xs text-muted-foreground/60 transition-colors hover:text-muted-foreground"
				>
					{expanded ? 'Show less' : 'Show more'}
				</button>
			{/if}
		{/if}
	</div>
{/if}
