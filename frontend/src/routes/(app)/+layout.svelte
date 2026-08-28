<script lang="ts">
	import type { User } from '$lib/state/user.svelte';
	import { userState } from '$lib/state/user.svelte';
	import { api } from '$lib/api';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import Sidebar from '$lib/components/sidebar/sidebar.svelte';
	import * as Dialog from '$lib/components/ui/dialog';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import Button from '$lib/components/ui/button/button.svelte';
	import { Sparkles } from '@lucide/svelte';
	import { createChatConnection } from '$lib/state/chat-connection.svelte';
	import { setContext, onMount } from 'svelte';

	type ChangelogEntry = { title: string; body: string; createdAt: string };

	let { children } = $props();

	let changelogEntries = $state<ChangelogEntry[]>([]);
	let changelogDialogOpen = $state(false);

	const chat = createChatConnection();
	onMount(() => chat.start());
	setContext('chatConnection', chat);

	// Fetch user role, which also automatically checks authentication
	api
		.get<User>('/api/users/me')
		.then((res) => {
			userState.user = res;
			userState.loading = false;
		})
		.catch(() => {
			userState.loading = false;
		});

	// Fetch changelog unseen
	api.get<ChangelogEntry[]>('/api/changelog/unseen').then((res) => {
		changelogEntries = res;

		if (changelogEntries.length > 0) changelogDialogOpen = true;
	});
</script>

<Tooltip.Provider>
	{#if userState.loading}
		<div class="flex h-screen w-full items-center justify-center">
			<Spinner class="h-10 w-10" />
		</div>
	{:else}
		<div class="flex h-screen w-full">
			<!-- Sidebar -->
			<Sidebar />

			<!-- Page content -->
			<main class="flex-1 overflow-y-auto">
				{@render children()}
			</main>
		</div>
	{/if}
</Tooltip.Provider>

<Dialog.Root bind:open={changelogDialogOpen}>
	<Dialog.Content class="flex max-h-[80vh] max-w-3xl! flex-col p-5">
		<Dialog.Header>
			<Dialog.Title class="flex items-center gap-2"
				><Sparkles size={14} class="text-amber-400" /> What's new</Dialog.Title
			>
			<Dialog.Description
				>These are all the changes since the last time you logged in.</Dialog.Description
			>
		</Dialog.Header>

		<div class="flex max-h-[60vh] flex-col overflow-y-auto p-2">
			{#each changelogEntries as entry, i (entry.title)}
				{#if i > 0}
					<hr class="my-3" />
				{/if}
				<div class="flex flex-col gap-1.5">
					<div class="flex items-center justify-between">
						<span class="font-medium">{entry.title}</span>
						<span class="text-xs text-muted-foreground"
							>{new Date(entry.createdAt).toLocaleDateString()}</span
						>
					</div>
					<p class="text-sm text-muted-foreground whitespace-pre-line">{entry.body}</p>
				</div>
			{/each}
		</div>

		<Dialog.Footer>
			<Button class="cursor-pointer" onclick={() => (changelogDialogOpen = false)}>Got it</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
