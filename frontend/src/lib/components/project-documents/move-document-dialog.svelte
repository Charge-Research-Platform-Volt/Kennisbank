<script lang="ts">
	import { untrack } from 'svelte';
	import { Check, Folder, FolderRoot } from '@lucide/svelte';
	import { toast } from 'svelte-sonner';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import Button from '#lib/components/ui/button/button.svelte';
	import Spinner from '#lib/components/ui/spinner/spinner.svelte';
	import { api } from '#lib/api.js';
	import { projectDocuments } from '#lib/project-documents.js';
	import type { ProjectFolder } from '#lib/types/project.js';
	import type { ProjectDocument } from '#lib/types/project-document.js';

	let {
		open = $bindable(false),
		document: doc,
		onmoved
	}: {
		open: boolean;
		document: ProjectDocument;
		onmoved?: () => void;
	} = $props();

	let folders = $state<ProjectFolder[] | null>(null);
	/** The project root id, or a folder id */
	let destinationId = $state('');
	let moving = $state(false);

	// Where it is now: a folder, or the root when it has none
	const currentId = $derived(doc.folderId ?? doc.projectId);

	$effect(() => {
		if (!open) return;
		untrack(async () => {
			folders = null;
			destinationId = currentId;

			try {
				folders = await api.get<ProjectFolder[]>(`/api/projects/${doc.projectId}/folders`);
			} catch (e) {
				toast.error(e instanceof Error ? e.message : 'Failed to load folders.');
				open = false;
			}
		});
	});

	async function move() {
		if (destinationId === currentId) return;
		moving = true;

		try {
			await projectDocuments.move(doc.id, destinationId);
			const name =
				destinationId === doc.projectId
					? 'the project root'
					: (folders?.find((f) => f.id === destinationId)?.title ?? 'the folder');

			open = false;
			toast.success(`Moved to ${name}.`);
			onmoved?.();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to move document.');
		} finally {
			moving = false;
		}
	}
</script>

{#snippet option(id: string, title: string, depth: number, isRoot: boolean)}
	<button
		class="flex w-full cursor-pointer items-center gap-2 rounded-md py-1.5 pr-2 text-left text-sm transition-colors {destinationId ===
		id
			? 'bg-accent'
			: 'hover:bg-accent/50'}"
		style="padding-left: {0.5 + depth * 1}rem"
		onclick={() => (destinationId = id)}
	>
		{#if isRoot}
			<FolderRoot size={14} class="shrink-0 text-muted-foreground" />
		{:else}
			<Folder size={14} class="shrink-0 text-muted-foreground" />
		{/if}

		<span class="truncate">{title}</span>

		{#if id === currentId}
			<span class="ml-auto shrink-0 text-xs text-muted-foreground">Current</span>
		{:else if id === destinationId}
			<Check size={14} class="ml-auto shrink-0 text-primary" />
		{/if}
	</button>
{/snippet}

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Move "{doc.title}"</Dialog.Title>

			<Dialog.Description>
				Moving changes where it's listed, not who can open it.
			</Dialog.Description>
		</Dialog.Header>

		<div class="max-h-80 overflow-y-auto py-2">
			{#if folders === null}
				<div class="flex justify-center py-6"><Spinner class="h-5 w-5" /></div>
			{:else}
				{@render option(doc.projectId, 'Project root', 0, true)}
				{#each folders as folder (folder.id)}
					{@render option(folder.id, folder.title, folder.depth, false)}
				{/each}
			{/if}
		</div>

		<Dialog.Footer>
			<Button variant="outline" class="cursor-pointer" onclick={() => (open = false)}>
				Cancel
			</Button>
			<Button
				class="cursor-pointer"
				disabled={moving || destinationId === currentId}
				onclick={move}
			>
				Move here
			</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
