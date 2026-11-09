<script lang="ts">
	import { untrack } from 'svelte';
	import { ChevronDown, ChevronRight } from '@lucide/svelte';
	import { toast } from 'svelte-sonner';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import Button from '#lib/components/ui/button/button.svelte';
	import Input from '#lib/components/ui/input/input.svelte';
	import Label from '#lib/components/ui/label/label.svelte';
	import Textarea from '#lib/components/ui/textarea/textarea.svelte';
	import FileDropzone from '#lib/components/ui/file-dropzone.svelte';
	import { uploadFile } from '#lib/upload.js';
	import {
		getVisibilityIcon,
		projectDocuments,
		supportedExtensions,
		titleFromFileName,
		visibilityDescriptions,
		visibilityLabels
	} from '#lib/project-documents.js';
	import type {
		DocumentAccessType,
		DocumentVisibility,
		ProjectDocument,
		ProjectDocumentType
	} from '#lib/types/project-document.js';

	let {
		open = $bindable(false),
		projectId,
		versionOf = null,
		initialFile = null,
		onuploaded
	}: {
		open: boolean;
		/** Where a new document lands: the root project or one of its folders */
		projectId: string;
		/** Set to upload a new version of this document instead of creating a new one */
		versionOf?: ProjectDocument | null;
		/** A file dropped onto the page before the dialog opened */
		initialFile?: File | null;
		onuploaded?: (documentId: string) => void;
	} = $props();

	const MAX_TITLE_LENGTH = 300;
	const visibilityOptions: DocumentVisibility[] = ['ProjectMembers', 'Restricted', 'Everyone'];
	const selectClass =
		'flex h-9 w-full cursor-pointer rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50';

	let file = $state<File | null>(null);
	let title = $state('');
	let titleEdited = $state(false);
	let typeId = $state('');
	let note = $state('');
	let visibility = $state<DocumentVisibility>('ProjectMembers');
	let generalAccessType = $state<DocumentAccessType>('Editor');
	let showSharing = $state(false);
	let types = $state<ProjectDocumentType[]>([]);
	let progress = $state<number | null>(null);
	let submitting = $state(false);

	// Kept between attempts, so a retry after an error (such as a version conflict) doesn't upload the file again
	let uploaded: { file: File; objectName: string } | null = null;

	const isNewVersion = $derived(versionOf != null);
	const SharingIcon = $derived(getVisibilityIcon(visibility));
	const canSubmit = $derived(
		file != null && !submitting && (isNewVersion || (title.trim().length > 0 && typeId !== ''))
	);

	// A fresh form every time the dialog opens
	$effect(() => {
		if (open) untrack(reset);
	});

	// The title follows the chosen file until the user types one themselves
	$effect(() => {
		const name = file?.name;
		if (name && !untrack(() => titleEdited)) title = titleFromFileName(name);
	});

	function reset() {
		file = initialFile;
		title = '';
		titleEdited = false;
		typeId = '';
		note = '';
		visibility = 'ProjectMembers';
		generalAccessType = 'Editor';
		showSharing = false;
		progress = null;
		uploaded = null;

		if (types.length === 0)
			projectDocuments
				.types()
				.then((t) => (types = t))
				.catch(() => toast.error('Failed to load document types.'));
	}

	async function submit() {
		const chosen = file;
		if (!chosen || !canSubmit) return;

		submitting = true;

		try {
			let objectName = uploaded?.file === chosen ? uploaded.objectName : null;
			if (!objectName) {
				progress = 0;
				objectName = await uploadFile(chosen, 'ProjectDocument', (f) => (progress = f));
				uploaded = { file: chosen, objectName };
			}

			const body = { objectName, fileName: chosen.name, note: note.trim() || undefined };
			let documentId: string;

			if (versionOf) {
				await projectDocuments.addVersion(projectId, versionOf.id, body);
				documentId = versionOf.id;
			} else {
				documentId = await projectDocuments.create(projectId, {
					...body,
					title: title.trim(),
					typeId,
					visibility,
					generalAccessType,
					grants: []
				});
			}

			open = false;
			toast.success(versionOf ? 'New version uploaded.' : 'Document uploaded.');
			onuploaded?.(documentId);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Upload failed.');
		} finally {
			submitting = false;
			progress = null;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>
				{versionOf ? `New version of "${versionOf.title}"` : 'Upload document'}
			</Dialog.Title>

			{#if versionOf}
				<Dialog.Description>
					The current version stays available in the version history.
				</Dialog.Description>
			{/if}
		</Dialog.Header>

		<div class="flex flex-col gap-4 py-2">
			<FileDropzone bind:file extensions={supportedExtensions} disabled={submitting} />

			{#if !isNewVersion}
				<div class="flex flex-col gap-1.5">
					<Label for="doc-title">Title</Label>
					<Input
						id="doc-title"
						bind:value={title}
						maxlength={MAX_TITLE_LENGTH}
						oninput={() => (titleEdited = true)}
					/>
				</div>

				<div class="flex flex-col gap-1.5">
					<Label for="doc-type">Type</Label>
					<select id="doc-type" bind:value={typeId} class={selectClass}>
						<option value="" disabled>Choose a type</option>
						{#each types as type (type.id)}
							<option value={type.id}>{type.name}</option>
						{/each}
					</select>
				</div>
			{/if}

			<div class="flex flex-col gap-1.5">
				<Label for="doc-note">{isNewVersion ? 'What changed? (optional)' : 'Note (optional)'}</Label
				>
				<Textarea id="doc-note" bind:value={note} rows={2} />
			</div>

			{#if !isNewVersion}
				<!-- Collapsed: most uploads keep the default, and people can be added later through Share -->
				<button
					type="button"
					class="flex cursor-pointer items-center gap-1.5 text-sm text-muted-foreground hover:text-foreground"
					onclick={() => (showSharing = !showSharing)}
				>
					{#if showSharing}<ChevronDown size={14} />{:else}<ChevronRight size={14} />{/if}
					<SharingIcon size={14} />
					{visibilityLabels[visibility]}{visibility === 'Restricted'
						? ''
						: ` can ${generalAccessType === 'Editor' ? 'edit' : 'view'}`}
				</button>

				{#if showSharing}
					<div class="flex flex-col gap-1.5 pl-5">
						<div class="flex gap-2">
							<select bind:value={visibility} class={selectClass}>
								{#each visibilityOptions as option (option)}
									<option value={option}>{visibilityLabels[option]}</option>
								{/each}
							</select>

							{#if visibility !== 'Restricted'}
								<select bind:value={generalAccessType} class="{selectClass} w-32">
									<option value="Viewer">can view</option>
									<option value="Editor">can edit</option>
								</select>
							{/if}
						</div>

						<p class="text-xs text-muted-foreground">
							{visibilityDescriptions[visibility]}. You can add specific people after uploading.
						</p>
					</div>
				{/if}
			{/if}

			{#if progress != null}
				<div class="h-1.5 w-full overflow-hidden rounded-full bg-muted">
					<div
						class="h-full bg-primary transition-all"
						style="width: {Math.round(progress * 100)}%"
					></div>
				</div>
			{/if}
		</div>

        <Dialog.Footer>
            <Button variant="outline" class="cursor-pointer" onclick={() => (open = false)}>Cancel</Button>
            <Button class="cursor-pointer" disabled={!canSubmit} onclick={submit}>
                {#if submitting}
                    {progress != null && progress < 1
                        ? `Uploading ${Math.round(progress * 100)}%`
                        : 'Saving...'}
                {:else}
                    {isNewVersion ? 'Upload version' : 'Upload'}
                {/if}
            </Button>
        </Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
