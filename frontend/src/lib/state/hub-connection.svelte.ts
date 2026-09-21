import { api } from '$lib/api';
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import type { HubConnection } from '@microsoft/signalr';
import type { MistralStatus } from './mistral-status.svelte';

export type HubConnectionContext = { readonly connection: HubConnection | null };

export type HubClientEvents = {
    ChatTitleUpdated: (chatId: string) => void;
    ChatThinking: () => void;
    ChatReasoningChunk: (text: string) => void;
    ChatToolStatus: (tool: string, label: string) => void;
    MistralStatusChanged: (status: MistralStatus) => void;
    EmbeddingStatusChanged: (id: string, status: string) => void;
}

export function onHubEvent<K extends keyof HubClientEvents>(hub: HubConnectionContext, event: K, handler: HubClientEvents[K]) {
    $effect(() => {
        const conn = hub.connection;
        if (!conn) return;
        conn.on(event, handler as (...args: unknown[]) => void);
        return () => conn.off(event, handler as (...args: unknown[]) => void);
    });
}

export function createHubConnection() {
    let connection = $state<HubConnection | null>(null);

    function start(): () => void {
        let stopped = false;

        const conn = new HubConnectionBuilder()
            .withUrl('/hub', { skipNegotiation: true, transport: HttpTransportType.WebSockets })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.None)
            .build();

        conn.onclose(() => {
            if (!stopped) api.get('/api/users/me').catch(() => {});
        })

        conn.start().then(() => { connection = conn; }).catch((e) => {
            console.error('SignalR connection error: ', e);
            api.get('/api/users/me').catch(() => {});
        });

        return () => { stopped = true; conn.stop(); };
    }

    return {
        get connection() { return connection; },
        start
    };
}