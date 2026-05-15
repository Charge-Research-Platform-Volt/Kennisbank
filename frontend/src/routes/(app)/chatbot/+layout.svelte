<script lang="ts">
    import { HubConnectionBuilder, LogLevel, HttpTransportType } from "@microsoft/signalr";
    import type { HubConnection } from "@microsoft/signalr";
    import { setContext, onMount } from 'svelte';
    import ChatHistory from "$lib/components/chatbot/chat-history.svelte";
    import { chatRefresh } from '$lib/state/chat-refresh.svelte';

    let { children } = $props();
    let connection = $state<HubConnection | null>(null);

    onMount(() => {
        const conn = new HubConnectionBuilder()
            .withUrl('/chat', { skipNegotiation: true, transport: HttpTransportType.WebSockets })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.None).build();

        conn.on('ChatTitleUpdated', () => chatRefresh.trigger());

        conn.start()
            .then(() => { connection = conn; })
            .catch(e => console.error('SignalR connection error: ', e));

        return () => { conn.stop(); };
    });

    setContext('chatConnection', { get connection() { return connection; }});
</script>

<div class="flex flex-1 h-full overflow-hidden">
    <div class="flex flex-col flex-1 overflow-hidden">
        {@render children()}
    </div>

    <div class="w-64 shrink-0 border-l border-border flex flex-col overflow-hidden">
        <ChatHistory />
    </div>
</div>