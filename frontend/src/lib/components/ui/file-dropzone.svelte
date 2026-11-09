<script lang="ts">
	import { FileText, Upload } from '@lucide/svelte';

	let {
		file = $bindable<File | null>(null),
		extensions = [],
		disabled = false
	}: {
		file: File | null;
		/** Lower-case, without dots. Empty accepts anything, e.g. while the list is still loading */
		extensions?: string[];
		disabled?: boolean;
	} = $props();

	let isDragging = $state(false);
	let error = $state<string | null>(null);
	let input = $state<HTMLInputElement | null>(null);

	function select(candidate: File | undefined) {
		error = null;
		if (!candidate) return;

		const ext = candidate.name.split('.').pop()?.toLowerCase() ?? '';
		if (extensions.length > 0 && !extensions.includes(ext)) {
			error = 'This file is not supported.';
			return;
		}

		file = candidate;
	}

	function browse() {
		if (!disabled) input?.click();
	}
</script>

<div
	role="button"
	tabindex="0"
	aria-disabled={disabled}
	onclick={browse}
	onkeydown={(e) => e.key === 'Enter' && browse()}
    ondragenter={(e) => {
        // Firefox only treats an element as drop target when dragenter is cancelled too
        e.preventDefault();
        if (!disabled) isDragging = true;
    }}
	ondragover={(e) => {
		e.preventDefault();
		if (!disabled) isDragging = true;
	}}
	ondragleave={() => (isDragging = false)}
	ondrop={(e) => {
		e.preventDefault();
		isDragging = false;
		if (!disabled) select(e.dataTransfer?.files[0]);
	}}
	class="flex min-h-48 cursor-pointer flex-col items-center justify-center gap-3 rounded-lg border-2 border-dashed border-border p-8 transition-colors {isDragging
		? 'border-primary bg-accent'
		: 'hover:bg-accent/50'} {disabled ? 'pointer-events-none opacity-60' : ''}"
>
	{#if file}
		<FileText class="h-8 w-8 text-primary" />
		<p class="text-sm font-medium">{file.name}</p>
		<p class="text-xs text-muted-foreground">{(file.size / 1024 / 1024).toFixed(2)} MB</p>
		<button
			onclick={(e) => {
				e.stopPropagation();
				file = null;
			}}
			class="text-xs text-muted-foreground underline hover:text-foreground">Remove</button
		>
	{:else}
		<Upload class="h-8 w-8 text-muted-foreground" />
		<p class="text-sm font-medium">Drop a file or click to browse</p>
		<p class="text-xs text-muted-foreground">{extensions.join(', ')}</p>
	{/if}
</div>

{#if error}
	<p class="text-xs text-destructive">{error}</p>
{/if}

<input
	bind:this={input}
	type="file"
	class="hidden"
	accept={extensions.map((ext) => `.${ext}`).join(',')}
	onchange={() => {
		select(input?.files?.[0]);
		// Otherwise picking the same file again after "Remove" fires no change event
		if (input) input.value = '';
	}}
/>
