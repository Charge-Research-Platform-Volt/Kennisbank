<script lang="ts">
	import { setContext, onMount } from 'svelte';
	import ChatHistory from '$lib/components/chatbot/chat-history.svelte';
	import { chatRefresh } from '$lib/state/chat-refresh.svelte';
	import { createChatConnection } from '$lib/state/chat-connection.svelte';

	let { children } = $props();
	const chat = createChatConnection(() => chatRefresh.trigger());
	onMount(() => chat.start());
	setContext('chatConnection', chat);
</script>

<div class="flex h-full flex-1 overflow-hidden">
	<div class="flex flex-1 flex-col overflow-hidden">
		{@render children()}
	</div>

	<div class="flex w-64 shrink-0 flex-col overflow-hidden border-l border-border">
		<ChatHistory />
	</div>
</div>
