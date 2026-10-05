<script lang="ts">
	import { api } from '$lib/api';
	import Button from '$lib/components/ui/button/button.svelte';
	import Input from '$lib/components/ui/input/input.svelte';
	import { renderChangelog } from '$lib/changelog';

	let title = $state('');
	let body = $state('');
	let status = $state('');

	const tips: { label: string; markdown: string }[] = [
		{ label: 'Bold', markdown: '**important**' },
		{ label: 'Italic', markdown: '*emphasis*' },
		{
			label: 'Link (internal, same tab)',
			markdown: '[see the full changelog](/changelog)'
		},
		{
			label: 'Link (external, new tab)',
			markdown: '[read more](https://example.com)'
		},
		{ label: 'Inline code', markdown: 'Run `dotnet build` to verify' },
		{ label: 'Bullet list', markdown: '- First point\n- Second point' },
		{ label: 'Numbered list', markdown: '1. First step\n2. Second step' },
		{ label: 'Heading', markdown: '## A heading' },
		{ label: 'Blockquote', markdown: '> A callout or quote' },
		{ label: 'Code block', markdown: '```\nconst x = 1;\n```' },
		{ label: 'Raw HTML', markdown: 'Also allowed: <strong>bold via HTML</strong>' }
	];

	async function submit() {
		try {
			await api.post('/api/changelog', { title, body });
			title = '';
			body = '';
			status = 'Entry added.';
		} catch (e) {
			status = `Error: ${e instanceof Error ? e.message : 'Unknown error'}`;
		}
	}
</script>

<div class="mx-auto grid h-full max-w-5xl grid-cols-2 gap-10 p-8">
	<!-- Form -->
	<div class="flex min-h-0 flex-col gap-4">
		<h1 class="text-lg font-semibold">Add Changelog Entry</h1>

		<div class="flex flex-col gap-1.5">
			<label class="text-sm font-medium" for="title">Title</label>
			<Input id="title" bind:value={title} placeholder="What changed?" />
		</div>

		<div class="flex flex-col gap-1.5">
			<label class="text-sm font-medium" for="body">Body</label>
			<textarea
				id="body"
				bind:value={body}
				placeholder="Describe the change..."
				rows="14"
				class="flex min-h-64 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50"
			></textarea>
		</div>

		<div class="flex flex-col items-center gap-2">
			<Button class="cursor-pointer" onclick={submit} disabled={!title || !body}>Submit</Button>
			{#if status}
				<span class="text-sm text-muted-foreground">{status}</span>
			{/if}
		</div>
	</div>

	<!-- Styling cheatsheet -->
	<div class="flex min-h-0 flex-col gap-3">
		<h2 class="text-lg font-semibold">Styling tips</h2>
		<p class="text-xs text-muted-foreground">
			The body is rendered as markdown, so any of the below works. Raw HTML tags are also passed
			through as-is.
		</p>

		<div
			class="flex min-h-0 flex-1 flex-col divide-y divide-border overflow-y-auto rounded-md border"
		>
			{#each tips as tip (tip.label)}
				<div class="flex flex-col gap-1.5 p-3">
					<span class="text-xs font-medium text-muted-foreground">{tip.label}</span>
					<pre class="overflow-x-auto rounded bg-muted p-2 text-xs">{tip.markdown}</pre>
					<div class="prose prose-sm max-w-none text-sm">
						<!-- eslint-disable-next-line svelte/no-at-html-tags -->
						{@html renderChangelog(tip.markdown)}
					</div>
				</div>
			{/each}
		</div>
	</div>
</div>
