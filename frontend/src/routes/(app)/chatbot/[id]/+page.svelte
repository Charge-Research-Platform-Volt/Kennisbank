<script lang="ts">
	import { page } from '$app/state';
	import ChatView from '$lib/components/chatbot/chat-view.svelte';
</script>

<ChatView chatId={page.params.id!} initialMessage={page.state.initialMessage} initialAttachmentIds={page.state.initialAttachmentIds} />
