<script lang="ts">
	import type { User } from '$lib/state/user.svelte';
	import { userState } from '$lib/state/user.svelte';
	import { api } from '$lib/api';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import Sidebar from '$lib/components/sidebar/sidebar.svelte';
	import * as Dialog from '$lib/components/ui/dialog';
	import * as Tooltip from '$lib/components/ui/tooltip';
	import Button from '$lib/components/ui/button/button.svelte';
	import { Sparkles, TriangleAlert, X } from '@lucide/svelte';
	import { createHubConnection, onHubEvent } from '$lib/state/hub-connection.svelte';
	import { renderChangelog } from '$lib/changelog';
	import { setContext, onMount } from 'svelte';
	import { mistralStatusState } from '$lib/state/mistral-status.svelte';
	import type { MistralStatus } from '$lib/state/mistral-status.svelte';

	type ChangelogEntry = { title: string; body: string; createdAt: string };

	let { children } = $props();

	let changelogEntries = $state<ChangelogEntry[]>([]);
	let changelogDialogOpen = $state(false);

	const hub = createHubConnection();
	onMount(() => hub.start());
	setContext('hubConnection', hub);

	api
		.get<MistralStatus>('/api/status/mistral')
		.then((res) => {
			mistralStatusState.current = res;
		})
		.catch(() => {});

	onHubEvent(hub, 'MistralStatusChanged', (status) => {
		mistralStatusState.current = status;
	});

	let mistralDegradedDismissed = $state(false);
	$effect(() => {
		if (mistralStatusState.current.status === 'operational') mistralDegradedDismissed = false;
	});

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
			<div class="flex flex-1 flex-col">
				<!-- Mistral status banner -->
				{#if mistralStatusState.current.status === 'down' && !mistralDegradedDismissed}
					<div
						class="flex items-center gap-2 border-b border-amber-500/50 bg-amber-500/10 px-4 py-2 text-sm text-amber-600 dark:text-amber-400"
					>
						<TriangleAlert size={14} class="shrink-0" />
						<span>
							Mistral's AI service currently appears to be having trouble. Chat and document
							processing may be affected, but some requests might still go through. This is not a
							bug on our end. Check
							<a
								href="https://status.mistral.ai"
								target="_blank"
								rel="noopener noreferrer"
								class="underline">status.mistral.ai</a
							>
							for the latest status.
						</span>
						<button
							type="button"
							onclick={() => (mistralDegradedDismissed = true)}
							class="ml-auto shrink-0 cursor-pointer rounded p-0.5 hover:bg-amber-500/20"
							aria-label="Dismiss"
						>
							<X size={14} />
						</button>
					</div>
				{/if}

				<main class="flex-1 overflow-y-auto">
					{@render children()}
				</main>
			</div>
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
					<div class="prose prose-sm max-w-none text-sm text-muted-foreground">
						<!-- eslint-disable-next-line svelte/no-at-html-tags -->
						{@html renderChangelog(entry.body)}
					</div>
				</div>
			{/each}
		</div>

		<Dialog.Footer>
			<Button class="cursor-pointer" onclick={() => (changelogDialogOpen = false)}>Got it</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
