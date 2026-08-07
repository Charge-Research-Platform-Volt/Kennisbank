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
			const uuidMatch = normalizedHref.match(/[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/i);
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

		// Replace [SRC:uuid] / [ATTACH:uuid] markers (and comma-grouped variants)
		let processed = content.replace(/\[(SRC|ATTACH):[^\]]+\]/gi, (match, kind: string) => {
			const uuids = [...match.matchAll(uuidRe)].map((m) => m[0]);
			const isAttachment = kind.toUpperCase() === 'ATTACH';

			return uuids
				.map((uuid) => {
					if (!uuidOrder.has(uuid)) uuidOrder.set(uuid, counter++);
					const n = uuidOrder.get(uuid);
					const source = resolvedSources?.get(uuid);
					if (source) {
						const url = isAttachment ? `/api/files/${uuid}` : `/library?inspectorId=${uuid}&inspectorType=${source.type}`;
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
	import { api } from '$lib/api';
	import { toast } from 'svelte-sonner';
	import type { HubConnection, ISubscription } from '@microsoft/signalr';
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
	};

	const ctx = getContext<{ connection: HubConnection | null }>('chatConnection');

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

	function extractMarkerUuids(content: string, prefix: 'SRC' | 'ATTACH'): string[] {
		const uuidRe = /[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}/gi;
		const markerRe = new RegExp(`\\[${prefix}:[^\\]]+\\]`, 'gi');
		return [
			...new Set(
				[...content.matchAll(markerRe)].flatMap((m) => [...m[0].matchAll(uuidRe)].map((u) => u[0]))
			)
		];
	}

	function loadAttachments(id: string) {
		attachmentsLoaded = api
			.get<{ id: string; fileName: string; detached: boolean }[]>(`/api/chats/${id}/attachments`)
			.then((result) => {
				attachmentsMap.clear();
				(result ?? []).forEach((a) => attachmentsMap.set(a.id, { id: a.id, name: a.fileName, type: 'attachment' }));
				activeAttachments = (result ?? []).filter((a) => !a.detached).map((a) => ({ id: a.id, name: a.fileName }));
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

	function fetchLibraryItems(ids: string[]): Promise<ResolvedSource[]> {
		if (ids.length === 0) return Promise.resolve([]);
		return api.post<ResolvedSource[]>(`/api/library/items?includeAuthors=true`, ids);
	}

	async function fetchSources(content: string, msgId: string) {
		await attachmentsLoaded;
		const srcUuids = extractMarkerUuids(content, 'SRC');
		const attachUuids = extractMarkerUuids(content, 'ATTACH');
		if (srcUuids.length === 0 && attachUuids.length === 0) return;

		const libraryItems = await fetchLibraryItems(srcUuids);

		const map = new SvelteMap<string, ResolvedSource>();
		attachUuids.forEach((uuid) => {
			const a = attachmentsMap.get(uuid);
			if (a) map.set(uuid, a);
		});
		libraryItems.forEach((s) => map.set(s.id, s));

		messages = messages.map((m) => (m.id === msgId ? { ...m, resolvedSources: map } : m));
	}

	async function fetchSourcesForMessages(msgs: Message[]) {
		await attachmentsLoaded;
		const assistantMsgs = msgs.filter((m) => m.messageRole === 'Assistant');
		const allSrcUuids = [...new Set(assistantMsgs.flatMap((m) => extractMarkerUuids(m.content, 'SRC')))];
		const hasAnyAttach = assistantMsgs.some((m) => extractMarkerUuids(m.content, 'ATTACH').length > 0);

		if (allSrcUuids.length === 0 && !hasAnyAttach) return;

		const libraryItems = await fetchLibraryItems(allSrcUuids);
		// eslint-disable-next-line svelte/prefer-svelte-reactivity
		const lookup = new Map<string, ResolvedSource>();
		libraryItems.forEach((s: ResolvedSource) => lookup.set(s.id, s));

		messages = msgs.map((m) => {
			if (m.messageRole !== 'Assistant') return m;

			const map = new SvelteMap<string, ResolvedSource>();
			extractMarkerUuids(m.content, 'ATTACH').forEach((uuid) => {
				const a = attachmentsMap.get(uuid);
				if (a) map.set(uuid, a);
			});
			extractMarkerUuids(m.content, 'SRC').forEach((uuid) => {
				if (lookup.has(uuid)) map.set(uuid, lookup.get(uuid)!);
			});

			return { ...m, resolvedSources: map };
		});
	}

	function fetchMessages(id: string) {
		api
			.get<Message[]>(`/api/chats/${id}/messages`)
			.then((result) => {
				messages = result ?? [];
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

	function stream(message: string, attachmentIds: string[] = []) {
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

		subscription = ctx.connection.stream('StreamAiResponse', message, chatId, projectId ?? null, attachmentIds).subscribe({
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
		const attachmentIds = attachments.map((a) => a.attachedId).filter((id): id is string => id !== null);
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
				.get<Message[]>(`/api/chats/${chatId}/messages`)
				.then((result) => {
					const existing = result ?? [];
					if (existing.length === 0) {
						messages = [];
						stream(initialMessage!, initialAttachmentIds);
					} else {
						loading = false;
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

	$effect(() => {
		const id = chatId;
		if (id === mountedChatId) return; // skip initial run, onMount handled it
		mountedChatId = id;

		messages = [];
		subscription?.dispose();
		subscription = null;
		loading = false;
		loadAttachments(id);
		fetchMessages(id);
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
</style>