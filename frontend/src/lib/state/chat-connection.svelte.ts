import { api } from '$lib/api';
import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import type { HubConnection } from '@microsoft/signalr';

export function createChatConnection() {
    let connection = $state<HubConnection | null>(null);

    function start(): () => void {
        let stopped = false;

        const conn = new HubConnectionBuilder()
            .withUrl('/chat', { skipNegotiation: true, transport: HttpTransportType.WebSockets })
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