<script module lang="ts">
	import { marked, Renderer } from 'marked';
	import markedKatex from 'marked-katex-extension';
	import 'katex/dist/katex.min.css';

	marked.use(markedKatex({ throwOnError: false }));

	type AuthorRef = { id: string; name: string; fileType?: string };

	type ResolvedSource = {
		id: string;
		name: string;
		type: 'resource' | 'person' | 'organisation' | 'attachment';
		fileType?: string;
		sourceUrl?: string;
		authors?: AuthorRef[] | null;
	};

	const libraryItemCache = new SvelteMap<string, ResolvedSource>();

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
			const uuidMatch = normalizedHref.match(
				/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i
			);
			const uuid = uuidMatch?.[0] ?? '';
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

		// Replace [SRC:uuid] / [ATTACH:uuid] markers (and comma-grouped variants), and bare UUIDs
		// the model occasionally emits without the required brackets — the resolved source's own
		// type decides the link target, not the marker kind, so this is robust either way. Also
		// swallow a stray colon glued directly in front of the marker with no space (another
		// formatting tic the model sometimes produces) rather than leaving it dangling in the text.
		let processed = content.replace(
			/:?(\[(?:SRC|ATTACH):[^\]]+\]|[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/gi,
			(_fullMatch, marker: string) => {
				const uuids = [...marker.matchAll(uuidRe)].map((m) => m[0]);

				return uuids
					.map((uuid) => {
						if (!uuidOrder.has(uuid)) uuidOrder.set(uuid, counter++);
						const n = uuidOrder.get(uuid);
						const source = resolvedSources?.get(uuid);
						if (source) {
							const url =
								source.type === 'attachment'
									? `/api/files/${uuid}`
									: `/library?inspectorId=${uuid}&inspectorType=${source.type}`;
							return `[${n}](${url})`;
						}
						return `[[BADGE:${n}]]`;
					})
					.join('');
			}
		);

		// Citations often land immediately before the sentence's terminal period, wherever the
		// model happened to place the marker in its own text — move a run of one or more
		// consecutive citations to after the period instead, matching normal citation style.
		processed = processed.replace(/((?:\[\d+\]\([^)]+\)|\[\[BADGE:\d+\]\])+)\./g, '.$1');

		// Collapse a run of consecutive horizontal rules (blank lines in between) into a single one.
		processed = processed.replace(/(?:^---$\n*){2,}/gm, '---\n\n');

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
	import { api } from '$lib/api';
	import { toast } from 'svelte-sonner';
	import type { ISubscription } from '@microsoft/signalr';
	import { onHubEvent, type HubConnectionContext } from '$lib/state/hub-connection.svelte';
	import ChatInput from '$lib/components/chatbot/chat-input.svelte';
	import { SvelteMap } from 'svelte/reactivity';
	import ChatMessage from '$lib/components/chatbot/ChatMessage.svelte';
	import { ArrowDown } from '@lucide/svelte';
	import type { PendingAttachment } from '$lib/components/chatbot/chat-input.svelte';

	let {
		chatId,
		projectId = null,
		initialMessage = null,
		initialAttachmentIds = []
	}: {
		chatId: string;
		projectId?: string | null;
		initialMessage?: string | null;
		initialAttachmentIds?: string[];
	} = $props();

	type Message = {
		id: string;
		messageRole: 'User' | 'Assistant';
		content: string;
		resolvedSources?: Map<string, ResolvedSource>;
		reasoning?: string;
		createdOn?: string;
	};

	const hub = getContext<HubConnectionContext>('hubConnection');

	let messages = $state<Message[]>([]);
	let hasMoreMessages = $state(true);
	let loadingOlder = $state(false);
	let topSentinel: HTMLDivElement | null = $state(null);
	let streamingAssistantId: string | null = null;
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
	const attachmentsMap = new SvelteMap<string, ResolvedSource>();
	let activeAttachments = $state<{ id: string; name: string }[]>([]);
	let attachmentsLoaded: Promise<void> = Promise.resolve();

	const itemGap = 16; // gap-4
	const topPadding = 24;
	const bottomPadding = 0;

	function updateSpacer() {
		if (!scrollContainerRef || !userMessageRef) {
			spacerHeight = 0;
			return;
		}

		const userEls = scrollContainerRef.querySelectorAll<HTMLElement>('[data-role="user"]');
		const lastUser = userEls[userEls.length - 1];

		if (!lastUser) {
			spacerHeight = 0;
			return;
		}

		const assistantEls =
			scrollContainerRef.querySelectorAll<HTMLElement>('[data-role="assistant"]');
		const lastAssistant = assistantEls[assistantEls.length - 1];

		const containerH = scrollContainerRef.clientHeight;
		const userH = lastUser.offsetHeight;
		const assistantH = lastAssistant?.offsetHeight ?? 0;
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

	let spacerUpdateScheduled = false;

	function scheduleSpacerUpdate() {
		if (spacerUpdateScheduled) return;

		spacerUpdateScheduled = true;
		tick().then(() => {
			requestAnimationFrame(() => {
				spacerUpdateScheduled = false;
				updateSpacer();
			});
		});
	}

	$effect(() => {
		void messages;
		scheduleSpacerUpdate();
	});

	// Show scroll button trigger
	$effect(() => {
		if (!scrollContainerRef) return;
		const onScroll = () => {
			const { scrollTop, scrollHeight, clientHeight } = scrollContainerRef!;
			showScrollButton = scrollHeight - scrollTop - clientHeight > 100;
		};
		scrollContainerRef.addEventListener('scroll', onScroll);
		return () => scrollContainerRef!.removeEventListener('scroll', onScroll);
	});

	// Load older messages sentinel trigger
	$effect(() => {
		if (!topSentinel || !scrollContainerRef) return;

		const observer = new IntersectionObserver(
			(entries) => {
				if (entries[0].isIntersecting) loadOlderMessages();
			},
			{ root: scrollContainerRef, rootMargin: '400px 0px 0px 0px' }
		);

		observer.observe(topSentinel);
		return () => observer.disconnect();
	});

	function extractAllUuids(content: string): string[] {
		const uuidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
		return [...new Set([...content.matchAll(uuidRe)].map((m) => m[0]))];
	}

	function loadAttachments(id: string) {
		attachmentsLoaded = api
			.get<{ id: string; fileName: string; detached: boolean }[]>(`/api/chats/${id}/attachments`)
			.then((result) => {
				attachmentsMap.clear();
				(result ?? []).forEach((a) =>
					attachmentsMap.set(a.id, { id: a.id, name: a.fileName, type: 'attachment' })
				);
				activeAttachments = (result ?? [])
					.filter((a) => !a.detached)
					.map((a) => ({ id: a.id, name: a.fileName }));
			})
			.catch(() => {
				attachmentsMap.clear();
				activeAttachments = [];
			});
	}

	async function removeAttachment(id: string) {
		try {
			await api.delete(`/api/chats/${chatId}/attachments/${id}`);
			activeAttachments = activeAttachments.filter((a) => a.id !== id);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to remove attachment.');
		}
	}

	async function resolveLibraryItems(ids: string[]): Promise<SvelteMap<string, ResolvedSource>> {
		const uncached = ids.filter((id) => !libraryItemCache.has(id));
		if (uncached.length > 0) {
			const items = await api.post<ResolvedSource[]>(
				`/api/library/items?includeAuthors=true`,
				uncached
			);
			(items ?? []).forEach((item) => libraryItemCache.set(item.id, item));
		}

		const map = new SvelteMap<string, ResolvedSource>();
		ids.forEach((id) => {
			const item = libraryItemCache.get(id);
			if (item) map.set(id, item);
		});

		return map;
	}

	async function fetchSources(content: string, msgId: string) {
		await attachmentsLoaded;
		const allUuids = extractAllUuids(content);
		if (allUuids.length === 0) return;

		const srcUuids = allUuids.filter((u) => !attachmentsMap.has(u));
		const libraryLookup = await resolveLibraryItems(srcUuids);

		const map = new SvelteMap<string, ResolvedSource>();
		allUuids.forEach((uuid) => {
			const a = attachmentsMap.get(uuid);
			if (a) {
				map.set(uuid, a);
				return;
			}
			const item = libraryLookup.get(uuid);
			if (item) map.set(uuid, item);
		});

		messages = messages.map((m) => (m.id === msgId ? { ...m, resolvedSources: map } : m));
	}

	async function fetchSourcesForMessages(msgs: Message[]) {
		await attachmentsLoaded;
		const assistantMsgs = msgs.filter((m) => m.messageRole === 'Assistant');
		const allUuids = [...new Set(assistantMsgs.flatMap((m) => extractAllUuids(m.content)))];
		if (allUuids.length === 0) return;

		const srcUuids = allUuids.filter((u) => !attachmentsMap.has(u));
		const libraryLookup = await resolveLibraryItems(srcUuids);

		messages = msgs.map((m) => {
			if (m.messageRole !== 'Assistant') return m;

			const map = new SvelteMap<string, ResolvedSource>();
			extractAllUuids(m.content).forEach((uuid) => {
				const a = attachmentsMap.get(uuid);
				if (a) {
					map.set(uuid, a);
					return;
				}
				const item = libraryLookup.get(uuid);
				if (item) map.set(uuid, item);
			});

			return { ...m, resolvedSources: map };
		});
	}

	function fetchMessages(id: string) {
		api
			.get<{ items: Message[]; hasMore: boolean }>(`/api/chats/${id}/messages`)
			.then((result) => {
				messages = result?.items ?? [];
				hasMoreMessages = result?.hasMore ?? false;
				fetchSourcesForMessages(messages);
				tick().then(() => {
					setTimeout(
						() => (userMessageRef ? scrollToLastUserMessage() : bottomRef?.scrollIntoView()),
						100
					);
				});
			})
			.catch(() => {
				messages = [{ id: 'error', messageRole: 'Assistant', content: 'Failed to load messages.' }];
			});
	}

	async function loadOlderMessages() {
		if (loadingOlder || !hasMoreMessages || messages.length === 0) return;
		loadingOlder = true;

		const oldestCreatedOn = messages[0]?.createdOn;
		const container = scrollContainerRef;
		const prevScrollHeight = container?.scrollHeight ?? 0;

		try {
			const result = await api.get<{ items: Message[]; hasMore: boolean }>(
				`/api/chats/${chatId}/messages?before=${encodeURIComponent(oldestCreatedOn ?? '')}&limit=50`
			);
			messages = [...(result?.items ?? []), ...messages];
			hasMoreMessages = result?.hasMore ?? false;
			loadingOlder = false;

			await tick();
			await new Promise(requestAnimationFrame);
			if (container) {
				const newScrollHeight = container.scrollHeight;
				const delta = newScrollHeight - prevScrollHeight;
				container.scrollTop += delta;
			}

			fetchSourcesForMessages(messages);
		} catch {
			loadingOlder = false;
		}
	}

	function stream(message: string, attachmentIds: string[] = []) {
		if (!hub.connection) return;
		loading = true;

		messages = messages.filter((m) => !(m.messageRole === 'Assistant' && m.content === ''));

		const userId = `${Date.now()}-user`;
		const assistantId = `${Date.now()}-assistant`;
		streamingAssistantId = assistantId;
		messages = [
			...messages,
			{ id: userId, messageRole: 'User', content: message },
			{ id: assistantId, messageRole: 'Assistant', content: '', reasoning: '' }
		];
		tick().then(() => setTimeout(scrollToLastUserMessage, 100));

		subscription = hub.connection
			.stream('StreamChatResponse', message, chatId, projectId ?? null, attachmentIds)
			.subscribe({
				next: (chunk) => {
					messages = messages.map((m) =>
						m.id === assistantId ? { ...m, content: m.content + chunk } : m
					);
				},
				error: () => {
					streamingAssistantId = null;
					loading = false;
					thinking = false;
					messages = messages.map((m) =>
						m.id === assistantId ? { ...m, content: 'Something went wrong, please try again.' } : m
					);
					searchingQuery = null;
				},
				complete: () => {
					streamingAssistantId = null;
					loading = false;
					thinking = false;
					searchingQuery = null;

					if (attachmentIds.length > 0) loadAttachments(chatId);

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

	function handleSend(message: string, attachments: PendingAttachment[]) {
		const attachmentIds = attachments
			.map((a) => a.attachedId)
			.filter((id): id is string => id !== null);
		stream(message, attachmentIds);
	}

	onMount(() => {
		mountedChatId = chatId;
		loadAttachments(chatId);

		if (initialMessage) {
			// Show the message and a loading indicator immediately so the page doesn't
			// render blank (looking like a fresh, empty chat) while we check whether this
			// conversation already has messages.
			loading = true;
			messages = [
				{ id: 'optimistic-user', messageRole: 'User', content: initialMessage },
				{ id: 'optimistic-assistant', messageRole: 'Assistant', content: '' }
			];
			tick().then(() => setTimeout(scrollToLastUserMessage, 100));

			api
				.get<{ items: Message[]; hasMore: boolean }>(`/api/chats/${chatId}/messages`)
				.then((result) => {
					const existing = result?.items ?? [];
					if (existing.length === 0) {
						messages = [];
						stream(initialMessage!, initialAttachmentIds);
					} else {
						loading = false;
						messages = existing;
						hasMoreMessages = result?.hasMore ?? false;
						fetchSourcesForMessages(messages);
						tick().then(() =>
							setTimeout(
								() => (userMessageRef ? scrollToLastUserMessage() : bottomRef?.scrollIntoView()),
								100
							)
						);
					}
				})
				.catch(() => {
					messages = [];
					stream(initialMessage!, initialAttachmentIds);
				});
		} else {
			fetchMessages(chatId);
		}

		return () => {
			subscription?.dispose();
		};
	});

	// Reset variables on chat switch
	$effect(() => {
		const id = chatId;
		if (id === mountedChatId) return; // skip initial run, onMount handled it
		mountedChatId = id;

		messages = [];
		hasMoreMessages = true;
		loadingOlder = false;
		subscription?.dispose();
		subscription = null;
		loading = false;
		loadAttachments(id);
		fetchMessages(id);
	});

	onHubEvent(hub, 'ChatToolStatus', (_tool, label) => {
		searchingQuery = label;
		thinking = false;
	});

	onHubEvent(hub, 'ChatThinking', () => {
		searchingQuery = null;
		thinking = true;
		if (streamingAssistantId) {
			const id = streamingAssistantId;
			messages = messages.map((m) =>
				m.id === id && m.reasoning ? { ...m, reasoning: m.reasoning + '\n\n' } : m
			);
		}
	});

	onHubEvent(hub, 'ChatReasoningChunk', (text) => {
		if (!streamingAssistantId) return;
		const id = streamingAssistantId;
		messages = messages.map((m) =>
			m.id === id ? { ...m, reasoning: (m.reasoning ?? '') + text } : m
		);
	});
</script>

<div class="flex h-full flex-col">
	<div bind:this={scrollContainerRef} class="chat-scroll-container flex-1 overflow-y-auto pt-6">
		<div class="mx-auto flex w-full max-w-2xl flex-col gap-4 px-4">
			<div bind:this={topSentinel}></div>

			<div
				class="flex justify-center py-2 text-xs text-muted-foreground"
				class:invisible={!loadingOlder}
			>
				<span class="status-spinner"></span>
			</div>

			{#each messages as msg (msg.id)}
				{#if msg.messageRole === 'User'}
					<div
						bind:this={userMessageRef}
						data-role="user"
						class="max-w-[80%] self-end rounded-2xl bg-accent px-4 py-2 text-sm"
					>
						{msg.content}
					</div>
				{:else}
					<div class="flex flex-col gap-1">
						{#if msg.content === '' && loading}
							<div
								bind:this={assistantMessageRef}
								data-role="assistant"
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
						{/if}

						{#if msg.reasoning}
							<details class="chat-thinking-block" open={msg.content === ''}>
								<summary class="chat-thinking-summary"
									>{msg.content === '' ? 'Thinking...' : 'View reasoning'}</summary
								>
								<div class="chat-thinking-content">{msg.reasoning}</div>
							</details>
						{/if}

						{#if msg.content !== ''}
							<div
								bind:this={assistantMessageRef}
								data-role="assistant"
								class="prose prose-sm max-w-none text-sm"
							>
								<ChatMessage content={msg.content} resolvedSources={msg.resolvedSources} {render} />
							</div>
						{/if}
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
		<div class="mx-auto max-w-2xl px-4">
			<ChatInput
				bind:this={chatInput}
				{chatId}
				{loading}
				{activeAttachments}
				onRemoveAttachment={removeAttachment}
				onSend={handleSend}
				onStop={stop}
			/>
			<p class="mt-2 text-center text-xs text-muted-foreground">
				AI can make mistakes. Verify important information.
			</p>
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
	:global(.chat-cite-source-tag) {
		display: inline-block;
		margin-left: 0.4rem;
		padding: 0.05rem 0.35rem;
		border-radius: 9999px;
		background-color: color-mix(in oklch, var(--muted-foreground) 15%, transparent);
		font-size: 0.62rem;
		font-weight: 600;
		text-transform: uppercase;
		letter-spacing: 0.03em;
		text-decoration: none;
		vertical-align: middle;
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

	.chat-thinking-block {
		opacity: 0.75;
		font-size: 0.8rem;
	}

	.chat-thinking-summary {
		cursor: pointer;
		color: var(--muted-foreground);
		font-weight: 500;
		user-select: none;
	}

	.chat-thinking-content {
		white-space: pre-wrap;
		color: var(--muted-foreground);
		margin-top: 0.35rem;
		border-left: 2px solid var(--muted-foreground);
		padding-left: 0.75rem;
	}

	.chat-scroll-container {
		overflow-anchor: none;
	}
</style>
