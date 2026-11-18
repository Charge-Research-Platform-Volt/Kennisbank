<script lang="ts">
	import { untrack } from 'svelte';
	import { toast } from 'svelte-sonner';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import Button from '#lib/components/ui/button/button.svelte';
	import Input from '#lib/components/ui/input/input.svelte';
	import Label from '#lib/components/ui/label/label.svelte';
	import { projectDocuments } from '#lib/project-documents.js';
	import type { ProjectDocument, ProjectDocumentType } from '#lib/types/project-document.js';

	let {
		open = $bindable(false),
		document: doc,
		onsaved
	}: {
		open: boolean;
		document: ProjectDocument;
		onsaved?: () => void;
	} = $props();

	const MAX_TITLE_LENGTH = 300;
	const selectClass =
		'flex h-9 w-full cursor-pointer rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50';

	let title = $state('');
	let typeId = $state('');
	let types = $state<ProjectDocumentType[]>([]);
	let saving = $state(false);

	const canSave = $derived(
		!saving && title.trim().length > 0 && (title.trim() !== doc.title || typeId !== doc.typeId)
	);

	// Start from the document's current values every time the dialog opens
	$effect(() => {
		if (!open) return;

		untrack(() => {
			title = doc.title;
			typeId = doc.typeId;
			if (types.length === 0)
				projectDocuments
					.types()
					.then((t) => (types = t))
					.catch(() => toast.error('Failed to load document types.'));
		});
	});

	async function save() {
		if (!canSave) return;
		saving = true;

		try {
			// Only send what changed
			await projectDocuments.update(doc.id, {
				title: title.trim() !== doc.title ? title.trim() : undefined,
				typeId: typeId !== doc.typeId ? typeId : undefined
			});

			open = false;
			toast.success('Document updated.');
			onsaved?.();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to update document.');
		} finally {
			saving = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Edit details</Dialog.Title>
		</Dialog.Header>

		<div class="flex flex-col gap-4 py-2">
			<div class="flex flex-col gap-1.5">
				<Label for="edit-doc-title">Title</Label>
				<Input
					id="edit-doc-title"
					bind:value={title}
					maxlength={MAX_TITLE_LENGTH}
					onkeydown={(e) => e.key === 'Enter' && save()}
				/>
			</div>

			<div class="flex flex-col gap-1.5">
				<Label for="edit-doc-type">Type</Label>
				<select id="edit-doc-type" bind:value={typeId} class={selectClass}>
					{#each types as type (type.id)}
						<option value={type.id}>{type.name}</option>
					{/each}
				</select>
			</div>
		</div>

		<Dialog.Footer>
			<Button variant="outline" class="cursor-pointer" onclick={() => (open = false)}>
				Cancel
			</Button>
			<Button class="cursor-pointer" disabled={!canSave} onclick={save}>Save</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
