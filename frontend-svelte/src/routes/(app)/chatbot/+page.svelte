<script lang="ts">
    import { getContext } from 'svelte';
    import { HubConnection } from '@microsoft/signalr';
    import { goto } from '$app/navigation';
    import ChatInput from '$lib/components/chatbot/chat-input.svelte';
    import { chatRefresh } from '$lib/state/chat-refresh.svelte';

    const ctx = getContext<{ connection: HubConnection | null }>('chatConnection');

    let submitting = $state(false);

    async function startChat(message: string, contentBased: boolean) {
        if (!ctx.connection || submitting) return;

        submitting = true;

        try {
            const chatId: string = await ctx.connection.invoke('CreateChat', message);
            goto(`/chatbot/${chatId}`, { state: { initialMessage: message, contentBased }});
            chatRefresh.trigger();
        } finally {
            submitting = false;
        }
    }
</script>

<div class="flex flex-col flex-1 items-center justify-center p-8 gap-6">
    <div class="text-center">
        <h1 class="text-2xl font-semibold">Knowledge Bank Assistant</h1>
        <p class="text-muted-foreground text-sm mt-1">Ask me anything!</p>
    </div>

    <div class="w-full max-w-2xl flex flex-col gap-2">
        <ChatInput onSend={startChat} onStop={() => {}} />
    </div>
</div>

