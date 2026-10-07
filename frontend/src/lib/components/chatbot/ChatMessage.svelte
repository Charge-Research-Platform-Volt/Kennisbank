<script lang="ts">
	import * as Tooltip from '$lib/components/ui/tooltip';
	import { buildSourceOrder, parseSegments, type ResolvedSource } from '$lib/chat/render';

	const maxAuthorsShown = 3;

	let {
		content,
		resolvedSources,
		render
	}: {
		content: string;
		resolvedSources?: Map<string, ResolvedSource>;
		render: (content: string, resolvedSources?: Map<string, ResolvedSource>) => string;
	} = $props();

	const segments = $derived(parseSegments(content));

	const sourceOrder = $derived(buildSourceOrder(content));

	// Popover
	type PopoverState = { source: ResolvedSource; x: number; y: number };
	let popover = $state<PopoverState | null>(null);
	let dismissTimer: ReturnType<typeof setTimeout> | null = null;

	function cancelDismiss() {
		if (dismissTimer) {
			clearTimeout(dismissTimer);
			dismissTimer = null;
		}
	}

	function scheduleDismiss() {
		dismissTimer = setTimeout(() => {
			popover = null;
			dismissTimer = null;
		}, 150);
	}

	function showPopoverForElement(el: HTMLElement, source: ResolvedSource) {
		cancelDismiss();
		const rect = el.getBoundingClientRect();
		popover = { source, x: rect.left + rect.width / 2, y: rect.top };
	}

	function handlePointerOver(e: PointerEvent) {
		if (!resolvedSources) return;
		const badge = (e.target as HTMLElement).closest(
			'.chat-cite-num[data-uuid]'
		) as HTMLAnchorElement | null;
		if (!badge) return;
		const uuid = badge.dataset.uuid;
		if (!uuid) return;
		const source = resolvedSources.get(uuid);
		if (!source) return;
		showPopoverForElement(badge, source);
	}

	function handlePointerOut(e: PointerEvent) {
		const badge = (e.target as HTMLElement).closest('.chat-cite-num[data-uuid]');
		if (!badge) return;
		const related = (e.relatedTarget as HTMLElement | null)?.closest('.chat-cite-num[data-uuid]');
		if (related === badge) return;
		scheduleDismiss();
	}
</script>

<!-- svelte-ignore a11y_no_static_element_interactions -->
<div onpointerover={handlePointerOver} onpointerout={handlePointerOut}>
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
					{@const href =
						src.type === 'attachment'
							? `/api/files/${uuid}`
							: `/library?inspectorId=${uuid}&inspectorType=${src.type}`}
					<li>
						<a
							{href}
							class="chat-cite-source"
							title={src.name}
							target="_blank"
							rel="noopener noreferrer"
							onpointerenter={(e) => showPopoverForElement(e.currentTarget, src)}
							onpointerleave={scheduleDismiss}
						>
							[{n}] {src.name}
							{#if src.type === 'attachment'}
								<span class="chat-cite-source-tag">Attachment</span>
							{/if}
						</a>
					</li>
				{/if}
			{/each}
		</ol>
	{/if}
</div>

{#if popover}
	<div
		class="cite-popover"
		style="left: {popover.x}px; top: {popover.y}px;"
		onpointerenter={cancelDismiss}
		onpointerleave={scheduleDismiss}
		role="tooltip"
	>
		<p class="cite-popover-kind">
			{popover.source.type === 'attachment' ? 'Attachment' : 'Library Source'}
		</p>
		<p class="cite-popover-name" title={popover.source.name}>{popover.source.name}</p>
		{#if popover.source.authors && popover.source.authors.length > 0}
			{@const authors = popover.source.authors}
			{@const shown = authors.slice(0, maxAuthorsShown)}
			{@const hiddenCount = authors.length - shown.length}
			<p class="cite-popover-authors" title={authors.map((a) => a.name).join(', ')}>
				{#each shown as author, i (author.id)}
					{#if author.fileType === 'person' || author.fileType === 'organisation'}
						<a
							href="/library?inspectorId={author.id}&inspectorType={author.fileType}"
							target="_blank"
							rel="noopener noreferrer"
						>
							{author.name}
						</a>
					{:else}
						<span>{author.name}</span>
					{/if}
					{#if i < shown.length - 1}<span>, </span>{/if}
				{/each}
				{#if hiddenCount > 0}
					<span> +{hiddenCount} more</span>
				{/if}
			</p>
		{/if}
		<div class="cite-popover-links">
			{#if popover.source.type !== 'attachment'}
				<a
					href="/library?inspectorId={popover.source.id}&inspectorType={popover.source.type}"
					target="_blank"
					rel="noopener noreferrer"
				>
					Open in library
				</a>
			{/if}
			{#if popover.source.fileType === 'website' && popover.source.sourceUrl}
				<a href={popover.source.sourceUrl} target="_blank" rel="noopener noreferrer">
					Open website
				</a>
			{:else if popover.source.type === 'attachment' || (popover.source.type === 'resource' && popover.source.fileType !== 'website')}
				<a href="/api/files/{popover.source.id}" target="_blank" rel="noopener noreferrer">
					Open file
				</a>
			{/if}
		</div>
	</div>
{/if}

<style>
	.cite-popover {
		position: fixed;
		transform: translate(-50%, calc(-100% - 8px));
		z-index: 9999;
		background: var(--background);
		border: 1px solid var(--border);
		border-radius: 0.5rem;
		padding: 0.5rem 0.75rem;
		box-shadow: 0 4px 16px color-mix(in oklch, currentColor 10%, transparent);
		min-width: 160px;
		max-width: 280px;
		pointer-events: auto;
	}

	.cite-popover-kind {
		font-size: 0.62rem;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.04em;
		color: var(--muted-foreground);
		margin: 0 0 0.15rem 0;
	}

	.cite-popover-name {
		font-size: 0.75rem;
		font-weight: 500;
		color: var(--foreground);
		margin: 0 0 0.35rem 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.cite-popover-authors {
		font-size: 0.7rem;
		color: var(--muted-foreground);
		margin: -0.2rem 0 0.4rem 0;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.cite-popover-authors a {
		color: inherit;
		text-decoration: underline;
		text-decoration-color: color-mix(in oklch, var(--muted-foreground) 40%, transparent);
		text-underline-offset: 2px;
	}

	.cite-popover-authors a:hover {
		color: var(--foreground);
	}

	.cite-popover-links {
		display: flex;
		flex-direction: column;
		gap: 0.2rem;
	}

	.cite-popover-links a {
		font-size: 0.7rem;
		color: var(--muted-foreground);
		text-decoration: none;
		transition: color 0.15s;
	}

	.cite-popover-links a:hover {
		color: var(--foreground);
		text-decoration: underline;
		text-underline-offset: 2px;
	}
</style>
