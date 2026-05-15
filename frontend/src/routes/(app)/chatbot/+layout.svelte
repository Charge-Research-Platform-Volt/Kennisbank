<script lang="ts">
	import { HubConnectionBuilder, LogLevel, HttpTransportType } from '@microsoft/signalr';
	import type { HubConnection } from '@microsoft/signalr';
	import { setContext, onMount } from 'svelte';
	import ChatHistory from '$lib/components/chatbot/chat-history.svelte';
	import { chatRefresh } from '$lib/state/chat-refresh.svelte';

	let { children } = $props();
	let connection = $state<HubConnection | null>(null);

	onMount(() => {
		const conn = new HubConnectionBuilder()
			.withUrl('/chat', { skipNegotiation: true, transport: HttpTransportType.WebSockets })
			.withAutomaticReconnect()
			.configureLogging(LogLevel.None)
			.build();

		conn.on('ChatTitleUpdated', () => chatRefresh.trigger());

		conn
			.start()
			.then(() => {
				connection = conn;
			})
			.catch((e) => console.error('SignalR connection error: ', e));

		return () => {
			conn.stop();
		};
	});

	setContext('chatConnection', {
		get connection() {
			return connection;
		}
	});
</script>

<div class="flex h-full flex-1 overflow-hidden">
	<div class="flex flex-1 flex-col overflow-hidden">
		{@render children()}
	</div>

	<div class="flex w-64 shrink-0 flex-col overflow-hidden border-l border-border">
		<ChatHistory />
	</div>
</div>
