<script module>
    import { marked, Renderer } from 'marked';

    const renderer = new Renderer();
    renderer.link = ({ href, text }) => {
        // Normalize library links — strip any origin the LLM may have prepended
        let normalizedHref = href ?? '';
        try {
            const url = new URL(normalizedHref);
            if (url.pathname.startsWith('/library')) {
                normalizedHref = url.pathname + url.search;
            }
        } catch { /* already a relative URL */ }

        if (/^\d+$/.test(text)) {
            return `<a href="${normalizedHref}" class="chat-cite-num" target="_blank" rel="noopener noreferrer">${text}</a>`;
        }
        if (normalizedHref.startsWith('/library')) {
            return `<a href="${normalizedHref}" class="chat-cite-source" title="${text}" target="_blank" rel="noopener noreferrer">${text}</a>`;
        }
        return `<a href="${normalizedHref}" class="chat-link" target="_blank" rel="noopener noreferrer">${text}</a>`;
    };
    marked.use({ renderer });

    function render(content: string): string {
        // Build number → title map from "N. [Title](url)" entries (sources list)
        const sourceMap: Record<number, string> = {};
        for (const m of content.matchAll(/^(\d+)\.\s+\[([^\]]+)\]/gm)) {
            sourceMap[parseInt(m[1])] = m[2];
        }

        const titleAttr = (n: string) => {
            const t = sourceMap[parseInt(n)];
            return t ? ` title="${t.replace(/"/g, '&quot;')}"` : '';
        };

        let html = marked(content) as string;
        // Add title to already-linked badges from renderer.link
        html = html.replace(/<a([^>]*class="chat-cite-num"[^>]*)>(\d+)<\/a>/g,
            (_, attrs, n) => `<a${attrs}${titleAttr(n)}>${n}</a>`);
        // Fallback: bare [N] the LLM forgot to linkify → non-clickable badge
        html = html.replace(/\[(\d+)\]/g,
            (_, n) => `<span class="chat-cite-num"${titleAttr(n)}>${n}</span>`);
        return html;
    }
</script>

<script lang="ts">
    import { getContext, onMount, tick } from 'svelte';
    import { page } from '$app/state';
    import { api } from '$lib/api';
    import { HubConnection, type ISubscription } from '@microsoft/signalr';
    import ChatInput from '$lib/components/chatbot/chat-input.svelte';

    type Message = { id: string; messageRole: 'User' | 'Assistant'; content: string };

    const ctx = getContext<{ connection: HubConnection | null}>('chatConnection');
    const chatId = $derived(page.params.id);

    let messages = $state<Message[]>([]);
    let loading = $state(false);
    let bottomRef = $state<HTMLDivElement | null>(null);
    let subscription: ISubscription<string> | null = null;
    let mountedChatId: string | undefined;
    let searchingQuery = $state<string | null>(null);

    function fetchMessages(id: string) {
        api.get<{ messages: Message[] }>(`/api/ai/messages/${id}`)
            .then(result => {
                messages = result.body?.messages ?? [];
                tick().then(() => bottomRef?.scrollIntoView());
            })
            .catch(() => {
                messages = [{ id: 'error', messageRole: 'Assistant', content: 'Failed to load messages.' }];
            });
    }

    function stream(message: string) {
        if (!ctx.connection) return;
        loading = true;

        const userId = `${Date.now()}-user`;
        const assistantId = `${Date.now()}-assistant`;
        messages = [...messages,
            { id: userId, messageRole: 'User', content: message },
            { id: assistantId, messageRole: 'Assistant', content: '' }
        ];
        tick().then(() => bottomRef?.scrollIntoView({ behavior: 'smooth' }));

        subscription = ctx.connection.stream('StreamAiResponse', message, chatId).subscribe({
            next: (chunk) => {
                messages = messages.map(m => m.id === assistantId ? { ...m, content: m.content + chunk } : m);
                bottomRef?.scrollIntoView({ behavior: 'smooth' });
            },
            error: () => {
                loading = false;
                messages = messages.map(m => m.id === assistantId
                    ? { ...m, content: 'Something went wrong, please try again.' }
                    : m);
                searchingQuery = null;
            },
            complete: () => { loading = false; searchingQuery = null; }
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
            const key = `chat-streamed-${id}`;
            if (!sessionStorage.getItem(key)) {
                sessionStorage.setItem(key, '1');
                stream(initialMessage);
            } else {
                fetchMessages(id!);
            }
        } else {
            fetchMessages(id!);
        }

        return () => { subscription?.dispose(); };
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

        ctx.connection.on('SearchStatus', (query: string) => {
            searchingQuery = query;
        });
    });
</script>

<div class="flex flex-col h-full">
    <div class="flex-1 overflow-y-auto py-6">
        <div class="mx-auto w-full max-w-2xl px-4 flex flex-col gap-4">
            {#each messages as msg (msg.id)}
                {#if msg.messageRole === 'User'}
                    <div class="self-end bg-accent rounded-2xl px-4 py-2 text-sm max-w-[80%]">
                        {msg.content}
                    </div>
                {:else if msg.content === '' && loading}
                    <div class="flex gap-1 items-center h-6 text-xs text-muted-foreground">
                        {#if searchingQuery}
                            Searching: {searchingQuery}
                        {:else}
                            <span class="thinking-dot"></span>
                            <span class="thinking-dot" style="animation-delay: 0.15s"></span>
                            <span class="thinking-dot" style="animation-delay: 0.3s"></span>
                        {/if}
                    </div>
                {:else}
                    <div class="text-sm prose prose-sm max-w-none">
                        <!-- eslint-disable-next-line svelte/no-at-html-tags -->
                        {@html render(msg.content)}
                    </div>
                {/if}
            {/each}

            <div bind:this={bottomRef}></div>
        </div>
    </div>

    <div class="shrink-0 pb-4">
        <div class="mx-auto max-w-2xl">
            <ChatInput {loading} onSend={stream} onStop={stop} />
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
    :global(.thinking-dot) {
        display: inline-block;
        width: 0.4rem;
        height: 0.4rem;
        border-radius: 9999px;
        background-color: var(--muted-foreground);
        animation: thinking-bounce 0.8s ease-in-out infinite;
    }
    @keyframes thinking-bounce {
        0%, 80%, 100% { transform: translateY(0); opacity: 0.4; }
        40% { transform: translateY(-0.3rem); opacity: 1; }
    }
    :global(.chat-link) {
        color: var(--primary);
        text-decoration: underline;
        text-underline-offset: 3px;
    }
</style>
