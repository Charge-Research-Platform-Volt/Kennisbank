<script lang="ts">
	import { api } from '$lib/api';
	import { goto } from '$app/navigation';
	import { page } from '$app/state';
	import { Trash2 } from '@lucide/svelte';
	import Button from '../ui/button/button.svelte';
	import { SvelteDate } from 'svelte/reactivity';
	import { chatRefresh } from '$lib/state/chat-refresh.svelte';

	function focus(el: HTMLInputElement) {
		el.focus();
		el.select();
	}

	let clickTimer: ReturnType<typeof setTimeout> | null = null;

	function handleClick(chat: Chat) {
		if (clickTimer) return; // part of a double-click, skip
		clickTimer = setTimeout(() => {
			clickTimer = null;
			goto(`/chatbot/${chat.id}`);
		}, 220);
	}

	function handleDblClick(chat: Chat) {
		if (clickTimer) {
			clearTimeout(clickTimer);
			clickTimer = null;
		}
		startEdit(chat);
	}

	type Chat = { id: string; title: string; createdOn: string };

	let chatHistory = $state<Record<string, Chat[]>>({});
	let editingId = $state<string | null>(null);
	let editingTitle = $state('');

	async function fetchHistory() {
		const result = await api.get<Record<string, Chat[]>>('/api/chats');
		chatHistory = result ?? {};
	}

	async function deleteChat(id: string) {
		await api.delete(`/api/chats/${id}`);
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

		await api.patch(`/api/chats/${id}/title`, title);
		await fetchHistory();
	}

	function formatDateGroup(dateKey: string): string {
		const date = new SvelteDate(dateKey);
		const today = new SvelteDate();
		const yesterday = new SvelteDate(today);
		yesterday.setDate(today.getDate() - 1);

		if (date.toDateString() === today.toDateString()) return 'Today';
		if (date.toDateString() === yesterday.toDateString()) return 'Yesterday';
		return date.toLocaleDateString(undefined, { day: 'numeric', month: 'short', year: 'numeric' });
	}

	$effect(() => {
		void chatRefresh.tick;
		fetchHistory();
	});
</script>
·
<div class="flex h-full flex-col overflow-hidden">
	<div class="border-b border-border p-3">
		<Button class="w-full cursor-pointer" onclick={() => goto('/chatbot')}>+ New Chat</Button>
	</div>

	{#if Object.entries(chatHistory).length > 0}
		<div class="flex flex-1 flex-col gap-4 overflow-y-auto p-2">
			{#each Object.entries(chatHistory) as [dateKey, chats] (dateKey)}
				<div>
					<p class="px-2 py-1 text-xs text-muted-foreground">{formatDateGroup(dateKey)}</p>

					{#each chats as chat (chat.id)}
						<div class="group relative">
							{#if editingId === chat.id}
								<input
									class="w-full rounded-md border border-border bg-background px-2 py-1.5 text-sm outline-none focus:border-primary"
									bind:value={editingTitle}
									onblur={() => saveEdit(chat.id)}
									onkeydown={(e) => {
										if (e.key === 'Enter') saveEdit(chat.id);
										if (e.key === 'Escape') cancelEdit();
									}}
									use:focus
								/>
							{:else}
								<button
									title={chat.title}
									class="flex w-full cursor-pointer items-center gap-1 rounded-md px-2 py-1.5 text-left hover:bg-accent {page
										.params.id === chat.id
										? 'bg-accent'
										: ''}"
									onclick={() => handleClick(chat)}
									ondblclick={() => handleDblClick(chat)}
								>
									<span class="flex-1 truncate pr-5 text-sm">{chat.title}</span>
								</button>
								<button
									class="absolute top-1/2 right-2 -translate-y-1/2 cursor-pointer text-muted-foreground opacity-0 group-hover:opacity-100 hover:text-destructive"
									onclick={() => deleteChat(chat.id)}
								>
									<Trash2 size={13} />
								</button>
							{/if}
						</div>
					{/each}
				</div>
			{/each}
		</div>
	{:else}
		<p class="pt-5 text-center text-xs text-muted-foreground">No chats yet</p>
	{/if}
</div>
