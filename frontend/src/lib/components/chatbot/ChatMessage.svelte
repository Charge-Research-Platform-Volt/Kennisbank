<script lang="ts">
	import * as Tooltip from '$lib/components/ui/tooltip';

	type ResolvedSource = { id: string; name: string; type: string };

	let {
		content,
		resolvedSources,
		render
	}: {
		content: string;
		resolvedSources?: Map<string, ResolvedSource>;
		render: (content: string, resolvedSources?: Map<string, ResolvedSource>) => string;
	} = $props();

	type Segment = { type: 'text' | 'ai'; content: string };

	function parseSegments(raw: string): Segment[] {
		const segments: Segment[] = [];
		const re = /\[AI\](.*?)\[\/AI\]/gs;

		let last = 0;

		for (const m of raw.matchAll(re)) {
			if (m.index! > last) segments.push({ type: 'text', content: raw.slice(last, m.index) });

			segments.push({ type: 'ai', content: m[1] });
			last = m.index! + m[0].length;
		}

		if (last < raw.length) segments.push({ type: 'text', content: raw.slice(last) });

		return segments;
	}

	const segments = $derived(parseSegments(content));

	const uuidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

	function buildSourceOrder(): Map<string, number> {
		// eslint-disable-next-line svelte/prefer-svelte-reactivity
		const order = new Map<string, number>();
		let counter = 1;
		for (const m of content.matchAll(/\[SRC:[^\]]+\]/gi)) {
			for (const u of m[0].matchAll(uuidRe)) {
				if (!order.has(u[0])) order.set(u[0], counter++);
			}
		}
		return order;
	}

	const sourceOrder = $derived(buildSourceOrder());
</script>

{#each segments as seg, i (i)}
	{#if seg.type === 'text'}
		<!-- eslint-disable-next-line svelte/no-at-html-tags -->
		{@html render(seg.content, resolvedSources)}
	{:else}
		<div class="ai-knowledge-block">
			<Tooltip.Root>
				<Tooltip.Trigger>
					<span class="ai-badge">AI Knowledge</span>
				</Tooltip.Trigger>
				<Tooltip.Content>
					<p>Generated from AI training data, not from the library. Verify independently.</p>
				</Tooltip.Content>
			</Tooltip.Root>
			<!-- eslint-disable-next-line svelte/no-at-html-tags -->
			{@html render(seg.content, resolvedSources)}
		</div>
	{/if}
{/each}

{#if resolvedSources && sourceOrder.size > 0}
	<hr class="my-3 opacity-50" />
	<p class="mb-1 text-xs font-medium text-muted-foreground">Sources</p>
	<ol class="m-0 list-none p-0">
		{#each [...sourceOrder.entries()].sort((a, b) => a[1] - b[1]) as [uuid, n] (uuid)}
			{#if resolvedSources.has(uuid)}
				{@const src = resolvedSources.get(uuid)!}
				<li>
					<a
						href="/library?inspectorId={uuid}&inspectorType={src.type}"
						class="chat-cite-source"
						title={src.name}
						target="_blank"
						rel="noopener noreferrer"
					>
						[{n}] {src.name}
					</a>
				</li>
			{/if}
		{/each}
	</ol>
{/if}
