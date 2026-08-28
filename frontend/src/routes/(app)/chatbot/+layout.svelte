<script lang="ts">
	import { getContext } from 'svelte';
	import ChatHistory from '$lib/components/chatbot/chat-history.svelte';
	import { chatRefresh } from '$lib/state/chat-refresh.svelte';
	import type { HubConnection } from '@microsoft/signalr';

	let { children } = $props();
	const chat = getContext<{ connection: HubConnection | null }>('chatConnection');

	$effect(() => {
		const conn = chat.connection;
		if (!conn) return;
		conn.on('ChatTitleUpdated', chatRefresh.trigger);
		return () => conn.off('ChatTitleUpdated', chatRefresh.trigger);
	})
</script>

<div class="flex h-full flex-1 overflow-hidden">
	<div class="flex flex-1 flex-col overflow-hidden">
		{@render children()}
	</div>

	<div class="flex w-64 shrink-0 flex-col overflow-hidden border-l border-border">
		<ChatHistory />
	</div>
</div>
