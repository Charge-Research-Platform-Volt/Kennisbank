<script lang="ts">
    import { getContext } from 'svelte';
    import { HubConnection } from '@microsoft/signalr';
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
            goto(`/chatbot/${chatId}`, { state: { initialMessage: message }});
            chatRefresh.trigger();
        } catch (e) {
            toast.error(e instanceof Error ? e.message : "Failed to create chat.");
        } 
        finally {
            submitting = false;
        }
    }
</script>

<div class="flex flex-col flex-1 items-center justify-center p-8 gap-6">
    <div class="text-center">
        <div class="flex items-center gap-2">
            <img src="/img/charge-icon.webp" alt="Charge Icon" class="w-8 h-8" />
            <span class="text-4xl font-medium">GridAI</span>
        </div>
        <p class="text-muted-foreground text-sm mt-1">Ask me anything!</p>
    </div>

    <div class="w-full max-w-2xl flex flex-col gap-2">
        <ChatInput onSend={startChat} onStop={() => {}} />
    </div>
</div>

