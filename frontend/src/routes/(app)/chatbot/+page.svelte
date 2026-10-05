<script lang="ts">
	import { getContext } from 'svelte';
	import { goto } from '$app/navigation';
	import { api } from '$lib/api';
	import ChatInput from '$lib/components/chatbot/chat-input.svelte';
	import type { PendingAttachment } from '$lib/components/chatbot/chat-input.svelte';
	import { chatRefresh } from '$lib/state/chat-refresh.svelte';
	import { toast } from 'svelte-sonner';
	import type { HubConnectionContext } from '$lib/state/hub-connection.svelte';

	const hub = getContext<HubConnectionContext>('hubConnection');

	let submitting = $state(false);

	async function startChat(message: string, attachments: PendingAttachment[]) {
		if (!hub.connection || submitting) return;

		submitting = true;

		try {
			const chatId: string = await hub.connection.invoke('CreateChat', message, null);

			const attachmentIds = await Promise.all(
				attachments.map(async (a) => {
					if (a.attachedId) return a.attachedId;
					const result = await api.post<{ id: string; fileName: string }>(
						`/api/chats/${chatId}/attachments`,
						{ ObjectName: a.objectName, FileName: a.fileName }
					);
					return result.id;
				})
			);

			goto(`/chatbot/${chatId}`, {
				state: { initialMessage: message, initialAttachmentIds: attachmentIds }
			});

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
		<div class="flex items-center justify-center gap-2">
			<img src="/img/charge-icon.webp" alt="Charge Icon" class="h-8 w-8" />
			<span class="text-4xl font-medium">GridAI</span>
		</div>

		<p class="mt-1 text-sm text-muted-foreground">An AI chatbot powered by the library.</p>
	</div>

	<div class="flex w-full max-w-2xl flex-col gap-2">
		<ChatInput onSend={startChat} onStop={() => {}} loading={submitting} />
		<p class="text-center text-xs text-muted-foreground">
			AI can make mistakes. Verify important information.
		</p>
	</div>
</div>
