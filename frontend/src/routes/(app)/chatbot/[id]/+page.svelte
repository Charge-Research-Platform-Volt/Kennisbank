<script module lang="ts">
	import { marked, Renderer } from 'marked';
	import markedKatex from 'marked-katex-extension';
	import 'katex/dist/katex.min.css';

	marked.use(markedKatex({ throwOnError: false }));

	type ResolvedSource = { id: string; name: string; type: string; fileType: string; sourceUrl?: string };

	const renderer = new Renderer();
	renderer.link = ({ href, text }) => {
		// Normalize library links — strip any origin the LLM may have prepended
		let normalizedHref = href ?? '';
		try {
			const url = new URL(normalizedHref);
			if (url.pathname.startsWith('/library')) {
				normalizedHref = url.pathname + url.search;
			}
		} catch {
			/* already a relative URL */
		}

		if (/^\d+$/.test(text)) {
			const uuidMatch = normalizedHref.match(/inspectorId=([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/i);
			const uuid = uuidMatch?.[1] ?? '';
			return `<a href="${normalizedHref}" class="chat-cite-num" data-uuid="${uuid}" target="_blank" rel="noopener noreferrer">${text}</a>`;
		}
		if (normalizedHref.startsWith('/library')) {
			return `<a href="${normalizedHref}" class="chat-cite-source" title="${text}" target="_blank" rel="noopener noreferrer">${text}</a>`;
		}
		return `<a href="${normalizedHref}" class="chat-link" target="_blank" rel="noopener noreferrer">${text}</a>`;
	};
	marked.use({ renderer });

	function render(content: string, resolvedSources?: Map<string, ResolvedSource>): string {
		// eslint-disable-next-line svelte/prefer-svelte-reactivity
		const uuidOrder = new Map<string, number>();
		let counter = 1;

		const uuidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;

		// Replace [SRC:uuid] and [SRC:uuid,SRC:uuid,...] markers
		let processed = content.replace(/\[SRC:[^\]]+\]/gi, (match) => {
			const uuids = [...match.matchAll(uuidRe)].map((m) => m[0]);
			return uuids
				.map((uuid) => {
					if (!uuidOrder.has(uuid)) uuidOrder.set(uuid, counter++);
					const n = uuidOrder.get(uuid);
					if (resolvedSources?.has(uuid)) {
						const { type } = resolvedSources.get(uuid)!;
						const url = `/library?inspectorId=${uuid}&inspectorType=${type}`;
						return `[${n}](${url})`;
					}
					return `[[BADGE:${n}]]`;
				})
				.join('');
		});

		let html = marked(processed) as string;

		// Replace [[BADGE:N]]
		html = html.replace(
			/\[\[BADGE:(\d+)\]\]/g,
			(_, n) => `<span class="chat-cite-num">${n}</span>`
		);

		return html;
	}
</script>

<script lang="ts">
	import { getContext, onMount, tick } from 'svelte';
	import { fade } from 'svelte/transition';
	import { page } from '$app/state';
	import { api } from '$lib/api';
	import type { HubConnection, ISubscription } from '@microsoft/signalr';
	import ChatInput from '$lib/components/chatbot/chat-input.svelte';
	import { SvelteMap } from 'svelte/reactivity';
	import ChatMessage from '$lib/components/chatbot/ChatMessage.svelte';
	import { ArrowDown } from '@lucide/svelte';

	type Message = {
		id: string;
		messageRole: 'User' | 'Assistant';
		content: string;
		resolvedSources?: Map<string, ResolvedSource>;
	};

	const ctx = getContext<{ connection: HubConnection | null }>('chatConnection');
	const chatId = $derived(page.params.id);

	let messages = $state<Message[]>([]);
	let loading = $state(false);
	let thinking = $state(false);
	let showScrollButton = $state(false);
	let bottomRef = $state<HTMLDivElement | null>(null);
	let userMessageRef = $state<HTMLDivElement | null>(null);
	let assistantMessageRef = $state<HTMLDivElement | null>(null);
	let scrollContainerRef = $state<HTMLDivElement | null>(null);
	let spacerHeight = $state(0);
	let chatInput = $state<{ focus: () => void } | null>(null);
	let subscription: ISubscription<string> | null = null;
	let mountedChatId: string | undefined;
	let searchingQuery = $state<string | null>(null);

	const itemGap = 16; // gap-4
	const topPadding = 24;
	const bottomPadding = 0;

	function updateSpacer() {
		if (!scrollContainerRef || !userMessageRef) {
			spacerHeight = 0;
			return;
		}
		const containerH = scrollContainerRef.clientHeight;
		const userH = userMessageRef.offsetHeight;
		const assistantH = assistantMessageRef?.offsetHeight ?? 0;
		spacerHeight = Math.max(
			0,
			containerH - topPadding - bottomPadding - userH - itemGap * 2 - assistantH
		);
	}

	function scrollToLastUserMessage() {
		if (!userMessageRef || !scrollContainerRef) return;
		const rect = userMessageRef.getBoundingClientRect();
		const containerRect = scrollContainerRef.getBoundingClientRect();
		scrollContainerRef.scrollTo({
			top: scrollContainerRef.scrollTop + rect.top - containerRect.top - topPadding + itemGap,
			behavior: 'smooth'
		});
	}

	$effect(() => {
		void messages;
		tick().then(() => setTimeout(updateSpacer, 50));
	});

	$effect(() => {
		if (!scrollContainerRef) return;
		const onScroll = () => {
			const { scrollTop, scrollHeight, clientHeight } = scrollContainerRef!;
			showScrollButton = scrollHeight - scrollTop - clientHeight > 100;
		};
		scrollContainerRef.addEventListener('scroll', onScroll);
		return () => scrollContainerRef!.removeEventListener('scroll', onScroll);
	});

	function extractUuids(content: string): string[] {
		const uuidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
		return [
			...new Set(
				[...content.matchAll(/\[SRC:[^\]]+\]/gi)].flatMap((m) =>
					[...m[0].matchAll(uuidRe)].map((u) => u[0])
				)
			)
		];
	}

	function fetchSources(content: string, msgId: string) {
		const uuids = extractUuids(content);
		if (uuids.length === 0) return;
		api.get<ResolvedSource[]>(`/api/ai/items?ids=${uuids.join(',')}`).then((r) => {
			const map = new SvelteMap<string, ResolvedSource>();
			r.body?.forEach((s: ResolvedSource) => map.set(s.id, s));
			messages = messages.map((m) => (m.id === msgId ? { ...m, resolvedSources: map } : m));
		});
	}

	function fetchSourcesForMessages(msgs: Message[]) {
		const assistantMsgs = msgs.filter((m) => m.messageRole === 'Assistant');
		const allUuids = [...new Set(assistantMsgs.flatMap((m) => extractUuids(m.content)))];

		if (allUuids.length === 0) return;

		api.get<ResolvedSource[]>(`/api/ai/items?ids=${allUuids.join(',')}`).then((r) => {
			// eslint-disable-next-line svelte/prefer-svelte-reactivity
			const lookup = new Map<string, ResolvedSource>();
			r.body?.forEach((s: ResolvedSource) => lookup.set(s.id, s));

			messages = msgs.map((m) => {
				if (m.messageRole !== 'Assistant') return m;

				const uuids = extractUuids(m.content);
				const map = new SvelteMap<string, ResolvedSource>();

				uuids.forEach((uuid) => {
					if (lookup.has(uuid)) map.set(uuid, lookup.get(uuid)!);
				});

				return { ...m, resolvedSources: map };
			});
		});
	}

	function fetchMessages(id: string) {
		api
			.get<{ messages: Message[] }>(`/api/ai/messages/${id}`)
			.then((result) => {
				messages = result.body?.messages ?? [];
				fetchSourcesForMessages(messages);
				tick().then(() =>
					setTimeout(
						() => (userMessageRef ? scrollToLastUserMessage() : bottomRef?.scrollIntoView()),
						100
					)
				);
			})
			.catch(() => {
				messages = [{ id: 'error', messageRole: 'Assistant', content: 'Failed to load messages.' }];
			});
	}

	function stream(message: string) {
		if (!ctx.connection) return;
		loading = true;

		messages = messages.filter((m) => !(m.messageRole === 'Assistant' && m.content === ''));

		const userId = `${Date.now()}-user`;
		const assistantId = `${Date.now()}-assistant`;
		messages = [
			...messages,
			{ id: userId, messageRole: 'User', content: message },
			{ id: assistantId, messageRole: 'Assistant', content: '' }
		];
		tick().then(() => setTimeout(scrollToLastUserMessage, 100));

		subscription = ctx.connection.stream('StreamAiResponse', message, chatId).subscribe({
			next: (chunk) => {
				messages = messages.map((m) =>
					m.id === assistantId ? { ...m, content: m.content + chunk } : m
				);
			},
			error: () => {
				loading = false;
				thinking = false;
				messages = messages.map((m) =>
					m.id === assistantId ? { ...m, content: 'Something went wrong, please try again.' } : m
				);
				searchingQuery = null;
			},
			complete: () => {
				loading = false;
				thinking = false;
				searchingQuery = null;

				const content = messages.find((m) => m.id === assistantId)?.content ?? '';
				fetchSources(content, assistantId);

				tick().then(() => chatInput?.focus());
			}
		});
	}

	function stop() {
		subscription?.dispose();
		subscription = null;
		loading = false;
	}

	onMount(() => {
		const id = chatId;
		mountedChatId = id;
		const { initialMessage } = page.state;

		if (initialMessage) {
			api
				.get<{ messages: Message[] }>(`/api/ai/messages/${id}`)
				.then((result) => {
					const existing = result.body?.messages ?? [];
					if (existing.length === 0) {
						stream(initialMessage);
					} else {
						messages = existing;
						fetchSourcesForMessages(messages);
						tick().then(() =>
							setTimeout(
								() => (userMessageRef ? scrollToLastUserMessage() : bottomRef?.scrollIntoView()),
								100
							)
						);
					}
				})
				.catch(() => stream(initialMessage));
		} else {
			fetchMessages(id!);
		}

		return () => {
			subscription?.dispose();
		};
	});

	$effect(() => {
		const id = chatId;
		if (id === mountedChatId) return; // skip initial run, onMount handled it
		mountedChatId = id;

		messages = [];
		subscription?.dispose();
		subscription = null;
		loading = false;
		fetchMessages(id!);
	});

	$effect(() => {
		if (!ctx.connection) return;

		ctx.connection.on('ToolStatus', (_tool: string, label: string) => {
			searchingQuery = label;
			thinking = false;
		});
		ctx.connection.on('Thinking', () => {
			searchingQuery = null;
			thinking = true;
		});
	});
</script>

<div class="flex h-full flex-col">
	<div bind:this={scrollContainerRef} class="flex-1 overflow-y-auto pt-6">
		<div class="mx-auto flex w-full max-w-2xl flex-col gap-4 px-4">
			{#each messages as msg (msg.id)}
				{#if msg.messageRole === 'User'}
					<div
						bind:this={userMessageRef}
						class="max-w-[80%] self-end rounded-2xl bg-accent px-4 py-2 text-sm"
					>
						{msg.content}
					</div>
				{:else if msg.content === '' && loading}
					<div
						bind:this={assistantMessageRef}
						class="flex items-center gap-1 text-xs text-muted-foreground"
					>
						{#if searchingQuery}
							<span class="status-spinner"></span>{searchingQuery}
						{:else if thinking}
							<span class="status-spinner"></span>Thinking...
						{:else}
							<span class="thinking-dot"></span>
							<span class="thinking-dot" style="animation-delay: 0.15s"></span>
							<span class="thinking-dot" style="animation-delay: 0.3s"></span>
						{/if}
					</div>
				{:else}
					<div bind:this={assistantMessageRef} class="prose prose-sm max-w-none text-sm">
						<ChatMessage content={msg.content} resolvedSources={msg.resolvedSources} {render} />
					</div>
				{/if}
			{/each}

			<div style="height: {spacerHeight}px"></div>
			<div bind:this={bottomRef}></div>
		</div>
	</div>

	<div class="relative shrink-0 pb-4">
		{#if showScrollButton}
			<div class="absolute -top-12 left-1/2 -translate-x-1/2" transition:fade={{ duration: 150 }}>
				<button
					onclick={() => bottomRef?.scrollIntoView({ behavior: 'smooth' })}
					class="flex cursor-pointer items-center gap-1.5 rounded-full border bg-background px-3 py-1.5 text-xs text-muted-foreground shadow-sm transition-colors hover:text-foreground"
				>
					<ArrowDown size={12} />
					Scroll to bottom
				</button>
			</div>
		{/if}
		<div class="mx-auto max-w-2xl">
			<ChatInput bind:this={chatInput} {loading} onSend={stream} onStop={stop} />
		</div>
	</div>
</div>

<style>
	:global(.chat-cite-num) {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-size: 0.65rem;
		font-weight: 600;
		line-height: 1;
		min-width: 1.1rem;
		height: 1.1rem;
		padding: 0 0.25rem;
		border-radius: 9999px;
		background-color: color-mix(in oklch, var(--primary) 15%, transparent);
		color: var(--primary);
		text-decoration: none;
		vertical-align: super;
		margin: 0 1px;
		cursor: pointer;
	}
	:global(.chat-cite-num:hover) {
		background-color: color-mix(in oklch, var(--primary) 25%, transparent);
	}
	:global(.chat-cite-source) {
		display: inline-block;
		max-width: 100%;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
		vertical-align: bottom;
		color: var(--muted-foreground);
		font-size: 0.8rem;
		text-decoration: underline;
		text-underline-offset: 3px;
		text-decoration-color: color-mix(in oklch, var(--muted-foreground) 40%, transparent);
		transition: color 0.15s;
	}
	:global(.chat-cite-source:hover) {
		color: var(--foreground);
	}
	:global(.status-spinner) {
		display: inline-block;
		width: 0.7rem;
		height: 0.7rem;
		border-radius: 9999px;
		border: 1.5px solid var(--muted-foreground);
		border-top-color: transparent;
		animation: spin 0.6s linear infinite;
		margin-right: 0.4rem;
		flex-shrink: 0;
	}
	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}
	:global(.thinking-dot) {
		display: inline-block;
		width: 0.4rem;
		height: 0.4rem;
		border-radius: 9999px;
		background-color: var(--muted-foreground);
		animation: thinking-bounce 0.8s ease-in-out infinite;
	}
	@keyframes thinking-bounce {
		0%,
		80%,
		100% {
			transform: translateY(0);
			opacity: 0.4;
		}
		40% {
			transform: translateY(-0.3rem);
			opacity: 1;
		}
	}
	:global(.chat-link) {
		color: var(--primary);
		text-decoration: underline;
		text-underline-offset: 3px;
	}
	:global(.ai-knowledge-block) {
		border-left: 2px solid oklch(0.75 0.12 85);
		padding-left: 0.75rem;
		margin: 0.5rem 0;
		opacity: 0.85;
	}
	:global(.ai-badge) {
		display: inline-flex;
		align-items: center;
		justify-content: center;
		font-size: 0.7rem;
		font-weight: 700;
		line-height: 1;
		padding: 0.2rem 0.5rem;
		border-radius: 9999px;
		background-color: color-mix(in oklch, oklch(0.8 0.15 85) 20%, transparent);
		color: oklch(0.55 0.15 85);
		vertical-align: middle;
		margin-right: 0.2rem;
		cursor: default;
	}
</style>
