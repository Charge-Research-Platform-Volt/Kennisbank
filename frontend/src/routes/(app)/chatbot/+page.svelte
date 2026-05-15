<script lang="ts">
	import { getContext } from 'svelte';
	import type { HubConnection } from '@microsoft/signalr';
	import { goto } from '$app/navigation';
	import ChatInput from '$lib/components/chatbot/chat-input.svelte';
	import { chatRefresh } from '$lib/state/chat-refresh.svelte';
	import { toast } from 'svelte-sonner';

	const ctx = getContext<{ connection: HubConnection | null }>('chatConnection');

	let submitting = $state(false);

	async function startChat(message: string) {
		if (!ctx.connection || submitting) return;

		submitting = true;

		try {
			const chatId: string = await ctx.connection.invoke('CreateChat', message);
			goto(`/chatbot/${chatId}`, { state: { initialMessage: message } });
			chatRefresh.trigger();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to create chat.');
		} finally {
			submitting = false;
		}
	}
</script>

<div class="flex flex-1 flex-col items-center justify-center gap-6 p-8">
	<div class="text-center">
		<div class="flex items-center gap-2">
			<img src="/img/charge-icon.webp" alt="Charge Icon" class="h-8 w-8" />
			<span class="text-4xl font-medium">GridAI</span>
		</div>
		<p class="mt-1 text-sm text-muted-foreground">Ask me anything!</p>
	</div>

	<div class="flex w-full max-w-2xl flex-col gap-2">
		<ChatInput onSend={startChat} onStop={() => {}} />
	</div>
</div>
