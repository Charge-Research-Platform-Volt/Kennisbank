<script lang="ts">
    import { HubConnectionBuilder, HttpTransportType, LogLevel } from "@microsoft/signalr";
    import type { HubConnection } from "@microsoft/signalr";
    import { setContext, onMount } from 'svelte';
    import ChatView from "./chat-view.svelte";
    import ChatInput from "./chat-input.svelte";
    import { api } from '$lib/api';
    import * as Popover from '$lib/components/ui/popover';
    import * as Tooltip from '$lib/components/ui/tooltip';
    import { SquarePen, History, X, Info } from '@lucide/svelte';

    type ChatSummary = { id: string; title: string; creationDate: string };

    let { projectId, onClose }: { projectId: string; onClose?: () => void } = $props();

    let connection = $state<HubConnection | null>(null);
    let chatId = $state<string | null>(null);
    let pendingMessage = $state<string | null>(null);
    let creating = $state(false);
    let historyVersion = $state(0);

    let history = $state<ChatSummary[]>([]);
    let historyOpen = $state(false);

    $effect(() => {
        void historyVersion;
        
        if (!historyOpen) return;

        api.get<{ chats: Record<string, ChatSummary[]> }>(`/api/ai/all-chats?projectId=${projectId}`)
            .then(r => { history = Object.values(r.body?.chats ?? {}).flat();});
    })

    setContext('chatConnection', {
        get connection() { return connection; }
    });

    onMount(() => {
        const conn = new HubConnectionBuilder()
            .withUrl('/chat', { skipNegotiation: true, transport: HttpTransportType.WebSockets })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.None)
            .build();

        conn.on('ChatTitleUpdated', () => historyVersion++);

        conn.start().then(() => { connection = conn; }).catch((e) => console.error('SignalR connection error: ', e));

        return () => { conn.stop(); };
    });

    async function startChat(message: string) {
        if (!connection || creating) return;
        creating = true;

        try {
            const id: string = await connection.invoke('CreateChat', message, projectId);
            pendingMessage = message;
            chatId = id;
        } catch (e) {
            console.error('Failed to create chat', e);
        } finally {
            creating = false;
        }
    }

    function newChat() {
        chatId = null;
        pendingMessage = null;
    }
</script>

<div class="flex h-full flex-col">
    <!-- Header -->
    <div class="flex shrink-0 items-center justify-between border-b px-3 py-2">
        <span class="text-sm font-medium">Project Chat</span>
        <div class="flex items-center gap-1">
            <Tooltip.Provider>
                <Popover.Root bind:open={historyOpen}>
                    <Tooltip.Root>
                        <Tooltip.Trigger>
                            <Popover.Trigger class="cursor-pointer rounded p-1 text-muted-foreground hover:text-foreground">
                                <History size={15} />
                            </Popover.Trigger>
                        </Tooltip.Trigger>
                        <Tooltip.Content>Chat history</Tooltip.Content>
                    </Tooltip.Root>

                    <Popover.Content class="w-64 p-1" align="end">
                        {#if history.length === 0}
                            <p class="px-2 py-4 text-center text-xs text-muted-foreground">No past chats.</p>
                        {:else}
                            {#each history as chat (chat.id)}
                                <button
                                    onclick={() => { chatId = chat.id; pendingMessage = null; historyOpen = false; }}
                                    class="w-full cursor-pointer truncate rounded px-2 py-1.5 text-left text-sm hover:bg-accent"
                                >
                                    {chat.title}
                                </button>
                            {/each}
                        {/if}
                    </Popover.Content>
                </Popover.Root>

                <Tooltip.Root>
                    <Tooltip.Trigger>
                        <button onclick={newChat} class="cursor-pointer rounded p-1 text-muted-foreground hover:text-foreground">
                            <SquarePen size={15} />
                        </button>
                    </Tooltip.Trigger>
                    <Tooltip.Content>New chat</Tooltip.Content>
                </Tooltip.Root>

                <Tooltip.Root>
                    <Tooltip.Trigger>
                        <button onclick={onClose} class="cursor-pointer rounded p-1 text-muted-foreground hover:text-foreground">
                            <X size={15} />
                        </button>
                    </Tooltip.Trigger>
                    <Tooltip.Content>Close</Tooltip.Content>
                </Tooltip.Root>
            </Tooltip.Provider>
        </div>
    </div>

    <!-- Content -->
    {#if chatId}
        <div class="min-h-0 flex-1 overflow-hidden">
            <ChatView {chatId} {projectId} initialMessage={pendingMessage} />
        </div>
    {:else}
        <div class="flex flex-1 flex-col items-center justify-center gap-3 px-4">
            <div class="text-center">
                <div class="flex items-center justify-center gap-2">
                    <img src="/img/charge-icon.webp" alt="Charge Icon" class="h-8 w-8" />
                    <span class="text-3xl font-medium">GridAI</span>
                </div>
                <p class="mt-1 text-xs text-muted-foreground">Ask about this project</p>
                <div class="mt-3 flex items-center gap-2 rounded-md border border-yellow-500/50 px-3 py-2 text-xs text-muted-foreground">
                    <Info size={13} class="shrink-0" />
                    <span>
                        Only searches this project's items.
                        For library-wide search, use the <a href="/chatbot" class="text-foreground underline underline-offset-2 hover:opacity-70">ChatBot</a>.
                    </span>
                </div>
            </div>
        </div>
        <div class="shrink-0 px-4 pb-4">
            <ChatInput loading={creating} onSend={startChat} onStop={() => {}} />
        </div>
    {/if}
</div>