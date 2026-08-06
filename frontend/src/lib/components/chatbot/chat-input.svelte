<script module lang="ts">
	export type PendingAttachment = {
		tempId: string;
		objectName: string;
		fileName: string;
		uploading: boolean;
		attachedId: string | null;
	};
</script>

<script lang="ts">
	import { ArrowUp, CircleX, Paperclip, X } from '@lucide/svelte';
	import { api } from "$lib/api";
	import { uploadFile } from '$lib/upload';

	let {
		chatId = null,
		onSend,
		onStop,
		loading = false
	}: {
		chatId?: string | null;
		onSend: (message: string, attachments: PendingAttachment[]) => void;
		onStop: () => void;
		loading?: boolean;
	} = $props();

	let message = $state('');
	let textarea = $state<HTMLTextAreaElement | null>(null);
	let fileInput = $state<HTMLInputElement | null>(null);
	let pendingAttachments = $state<PendingAttachment[]>([]);

	export function focus() {
		textarea?.focus();
	}

	async function handleFileSelect(e: Event) {
		const input = e.target as HTMLInputElement;
		const file = input.files?.[0];
		input.value = '';
		if (!file) return;

		const tempId = crypto.randomUUID();
		pendingAttachments = [
			...pendingAttachments,
			{ tempId, objectName: '', fileName: file.name, uploading: true, attachedId: null }
		];

		try {
			const objectName = await uploadFile(file);

			if (chatId) {
				const result = await api.post<{ id: string; fileName: string }>(`/api/chats/${chatId}/attachments`, { ObjectName: objectName, FileName: file.name });
				pendingAttachments = pendingAttachments.map((a) => a.tempId === tempId ? { ... a, objectName, uploading: false, attachedId: result.id } : a);
			}
			else {
				pendingAttachments = pendingAttachments.map((a) => a.tempId === tempId ? { ...a, objectName, uploading: false } : a);
			}
		}
		catch {
			pendingAttachments = pendingAttachments.filter((a) => a.tempId !== tempId);
		}
	}

	function removeAttachment(tempId: string) {
		pendingAttachments = pendingAttachments.filter((a) => a.tempId !== tempId);
	} 

	function handleSend() {
		if (!message.trim() || loading || pendingAttachments.some((a) => a.uploading)) return;
		onSend(message, pendingAttachments);
		message = '';
		pendingAttachments = [];
	}
</script>

<div class="rounded-lg border border-input bg-background shadow-sm">
	{#if pendingAttachments.length > 0}
		<div class="flex flex-wrap gap-2 px-3 pt-3">
			{#each pendingAttachments as attachment (attachment.tempId)}
				<div class="flex items-center gap-1.5 rounded-md border bg-accent/50 px-2 py-1 text-xs">
					{#if attachment.uploading}
						<span class="status-spinner"></span>
					{/if}
					<span class="max-w-40 truncate">{attachment.fileName}</span>
					<button type="button" class="cursor-pointer text-muted-foreground hover:text-foreground" onclick={() => removeAttachment(attachment.tempId)}>
						<X size={12} />
					</button>
				</div>
			{/each}
		</div>
	{/if}

	<textarea
		bind:this={textarea}
		class="w-full resize-none bg-transparent px-4 pt-3 pb-2 text-sm outline-none placeholder:text-muted-foreground"
		placeholder={loading ? 'Thinking...' : 'Ask me anything!'}
		rows={3}
		bind:value={message}
		onkeydown={(e) => {
			if (e.key === 'Enter' && !e.shiftKey && !loading) {
				e.preventDefault();
				handleSend();
			}
		}}
		disabled={loading}
	></textarea>

	<div class="flex items-center justify-between px-3 pb-2">
		<!-- Toolbar -->
		<div class="flex items-center gap-2 px-3 pb-3">
			<button type="button" class="cursor-pointer text-muted-foreground hover:text-foreground disabled:opacity-40" onclick={() => fileInput?.click()} disabled={loading}>
				<Paperclip size={16} />
			</button>
			<input bind:this={fileInput} type="file" class="hidden" onchange={handleFileSelect} />
		</div>

		{#if loading}
			<button
				class="cursor-pointer rounded-md bg-foreground p-1.5 text-background hover:bg-foreground/90 disabled:opacity-40"
				onclick={onStop}
			>
				<CircleX size={18} />
			</button>
		{:else}
			<button
				class="cursor-pointer rounded-md bg-primary p-1.5 text-primary-foreground hover:bg-primary/90 disabled:cursor-not-allowed disabled:opacity-40"
				onclick={handleSend}
				disabled={!message.trim() || pendingAttachments.some((a) => a.uploading)}
			>
				<ArrowUp size={18} />
			</button>
		{/if}
	</div>
</div>
