<script lang="ts">
    import { api } from "$lib/api";
    import { goto } from '$app/navigation';
    import { page } from '$app/state';
    import { Trash2 } from '@lucide/svelte';
    import Button from "../ui/button/button.svelte";
    import { SvelteDate } from "svelte/reactivity";
    import { chatRefresh } from '$lib/state/chat-refresh.svelte';

    function focus(el: HTMLInputElement) { el.focus(); el.select(); }

    let clickTimer: ReturnType<typeof setTimeout> | null = null;

    function handleClick(chat: Chat) {
        if (clickTimer) return; // part of a double-click, skip
        clickTimer = setTimeout(() => {
            clickTimer = null;
            goto(`/chatbot/${chat.id}`);
        }, 220);
    }

    function handleDblClick(chat: Chat) {
        if (clickTimer) { clearTimeout(clickTimer); clickTimer = null; }
        startEdit(chat);
    }

    type Chat = { id: string; title: string; creationDate: string };

    let chatHistory = $state<Record<string, Chat[]>>({});
    let editingId = $state<string | null>(null);
    let editingTitle = $state('');

    async function fetchHistory() {
        const result = await api.get<{ chats: Record<string, Chat[]> }>('/api/ai/all-chats');
        chatHistory = result.body?.chats ?? {};
    }

    async function deleteChat(id: string) {
        await api.delete(`/api/ai/delete-chat/${id}`);
        await fetchHistory();

        if (page.params.id === id) goto('/chatbot');
    }

    function startEdit(chat: Chat) {
        editingId = chat.id;
        editingTitle = chat.title;
    }

    function cancelEdit() {
        editingId = null;
        editingTitle = '';
    }

    async function saveEdit(id: string) {
        const title = editingTitle.trim();
        cancelEdit();
        if (!title) return;

        await api.patch(`/api/ai/rename-chat/${id}`, title);
        await fetchHistory();
    }

    function formatDateGroup(dateKey: string): string {
        const date = new SvelteDate(dateKey);
        const today = new SvelteDate();
        const yesterday = new SvelteDate(today);
        yesterday.setDate(today.getDate() - 1);

        if (date.toDateString() === today.toDateString()) return 'Today';
        if (date.toDateString() === yesterday.toDateString()) return 'Yesterday';
        return date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric'});
    }

    $effect(() => {
        void chatRefresh.tick;
        fetchHistory();
    });
</script>

<div class="flex flex-col h-full overflow-hidden">
    <div class="p-3 border-b border-border">
        <Button class="w-full cursor-pointer" onclick={() => goto('/chatbot')}>
            + New Chat
        </Button>
    </div>

    {#if Object.entries(chatHistory).length > 0}
        <div class="flex-1 overflow-y-auto p-2 flex flex-col gap-4">
            {#each Object.entries(chatHistory) as [dateKey, chats] (dateKey)}
                <div>
                    <p class="text-xs text-muted-foreground px-2 py-1">{formatDateGroup(dateKey)}</p>

                    {#each chats as chat (chat.id)}
                        <div class="group relative">
                            {#if editingId === chat.id}
                                <input
                                    class="w-full rounded-md px-2 py-1.5 text-sm bg-background border border-border outline-none focus:border-primary"
                                    bind:value={editingTitle}
                                    onblur={() => saveEdit(chat.id)}
                                    onkeydown={(e) => { if (e.key === 'Enter') saveEdit(chat.id); if (e.key === 'Escape') cancelEdit(); }}
                                    use:focus
                                />
                            {:else}
                                <button title={chat.title} class="flex items-center gap-1 w-full rounded-md px-2 py-1.5 hover:bg-accent cursor-pointer text-left {page.params.id === chat.id ? 'bg-accent' : ''}" onclick={() => handleClick(chat)} ondblclick={() => handleDblClick(chat)}>
                                    <span class="flex-1 text-sm truncate pr-5">{chat.title}</span>
                                </button>
                                <button class="absolute right-2 top-1/2 -translate-y-1/2 opacity-0 group-hover:opacity-100 cursor-pointer text-muted-foreground hover:text-destructive" onclick={() => deleteChat(chat.id)}>
                                    <Trash2 size={13} />
                                </button>
                            {/if}
                        </div>
                    {/each}
                </div>
            {/each}
        </div>
    {:else}
        <p class="text-center pt-5 text-xs text-muted-foreground">No chats yet</p>
    {/if}
</div>
