import { HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr';
import type { HubConnection } from '@microsoft/signalr';

export function createChatConnection(onTitleUpdated?: () => void) {
    let connection = $state<HubConnection | null>(null);

    function start(): () => void {
        const conn = new HubConnectionBuilder()
            .withUrl('/chat', { skipNegotiation: true, transport: HttpTransportType.WebSockets })
            .withAutomaticReconnect()
            .configureLogging(LogLevel.None)
            .build();

        if (onTitleUpdated) conn.on('ChatTitleUpdated', onTitleUpdated);

        conn.start().then(() => { connection = conn; }).catch((e) => console.error('SignalR connection error: ', e));

        return () => conn.stop();
    }

    return {
        get connection() { return connection; },
        start
    };
}