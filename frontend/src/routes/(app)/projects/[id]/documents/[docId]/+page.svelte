<script lang="ts">
	import { getContext, onMount } from 'svelte';
	import { afterNavigate, goto } from '$app/navigation';
	import { page } from '$app/state';
	import { toast } from 'svelte-sonner';
	import {
		TriangleAlert,
		Download,
		Ellipsis,
		MessageSquare,
		PanelRight,
		Upload,
		UserPlus,
		FolderInput,
		Pencil
	} from '@lucide/svelte';
	import { api } from '#lib/api.js';
	import { confirm } from '#lib/state/confirm.svelte.js';
	import { formatDate } from '#lib/utils/date.js';
	import * as Breadcrumb from '#lib/components/ui/breadcrumb/index.js';
	import * as DropdownMenu from '#lib/components/ui/dropdown-menu/index.js';
	import Button from '#lib/components/ui/button/button.svelte';
	import Spinner from '#lib/components/ui/spinner/spinner.svelte';
	import UploadDocumentDialog from '#lib/components/project-documents/upload-document-dialog.svelte';
	import {
		downloadUrl,
		formatFileSize,
		getVisibilityIcon,
		isProcessing,
		previewUrl,
		projectDocuments,
		visibilityLabels
	} from '#lib/project-documents.js';
	import type { ProjectInfo } from '#lib/types/project.js';
	import type { ProjectDocumentDetail } from '#lib/types/project-document.js';
	import ShareDocumentDialog from '#lib/components/project-documents/share-document-dialog.svelte';
	import MoveDocumentDialog from '#lib/components/project-documents/move-document-dialog.svelte';
	import EditDocumentDialog from '#lib/components/project-documents/edit-document-dialog.svelte';

	const toggleChat: () => void = getContext('toggleChat');

	let detail = $state<ProjectDocumentDetail | null>(null);
	/** The folder the document sits in (or the root), for the breadcrumb */
	let location = $state<ProjectInfo | null>(null);
	let notFound = $state(false);
	let selectedVersionId = $state<string | null>(null);
	let showDetails = $state(true);
	let uploadOpen = $state(false);
	let shareOpen = $state(false);
	let moveOpen = $state(false);
	let editOpen = $state(false);

	const selectedVersion = $derived(
		detail?.versions.find((v) => v.id === selectedVersionId) ?? detail?.latestVersion ?? null
	);
	const isLatest = $derived(selectedVersion?.id === detail?.latestVersion.id);
	const canEdit = $derived(detail?.accessLevel === 'Editor');
	const folderHref = $derived(
		detail ? `/projects/${detail.folderId ?? detail.projectId}` : '/projects'
	);
	const VisibilityIcon = $derived(detail ? getVisibilityIcon(detail.visibility) : null);

	// A PDF is its own preview; a Word file has one once the conversion has finished
	const previewReady = $derived(
		selectedVersion != null &&
			(selectedVersion.extension === 'pdf' || selectedVersion.previewStatus === 'Completed')
	);

	// Keep refreshing while the background pipeline works on any version, so the preview appears by itself
	const anyProcessing = $derived(detail?.versions.some(isProcessing) ?? false);

	$effect(() => {
		if (!anyProcessing) return;
		const timer = setInterval(load, 3000);
		return () => clearInterval(timer);
	});

	async function load() {
		const rootId = page.params.id!;
		const documentId = page.params.docId!;

		try {
			const doc = await projectDocuments.get(documentId);

			// One URL per document: under its own root project. Under any other project it doesn't exist,
			// which also keeps the chat pane next to it in the right project
			if (doc.projectId !== rootId) throw new Error('Not in this project');

			// The breadcrumb only needs reloading when the document is new to us or has moved
			const folderId = doc.folderId ?? doc.projectId;
			const folder =
				location?.project.id === folderId
					? location
					: await api.get<ProjectInfo>(`/api/projects/${folderId}`);

			// A slow response for a page we've already left must not overwrite this one
			if (rootId !== page.params.id || documentId !== page.params.docId) return;

			detail = doc;
			location = folder;
			notFound = false;
		} catch {
			// The API answers 404 both for documents that don't exist and ones the user can't view
			if (rootId === page.params.id && documentId === page.params.docId) notFound = true;
		}
	}

	function download() {
		if (!detail || !selectedVersion) return;
		window.location.href = downloadUrl(detail.id, selectedVersion.id);
	}

	async function deleteDocument() {
		if (!detail) return;
		if (!(await confirm(`Delete "${detail.title}" and all its versions? This cannot be undone.`)))
			return;

		try {
			await projectDocuments.delete(detail.id);
			toast.success('Successfully deleted document.');
			goto(folderHref);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to delete document.');
		}
	}

	async function onVersionUploaded() {
		// Show the new version as soon as it exists; its preview appears once processing is done
		selectedVersionId = null;
		await load();
	}

	onMount(load);

	afterNavigate(({ shallow, type }) => {
		if (shallow && type === 'goto') return;

		// Another document: start clean
		detail = null;
		location = null;
		notFound = false;
		selectedVersionId = null;
		return load();
	});
</script>

<div class="flex h-full flex-1 flex-col gap-3 overflow-hidden p-4">
	{#if notFound}
		<div class="flex h-full flex-col items-center justify-center gap-3 text-center">
			<p class="text-sm text-muted-foreground">
				This document doesn't exist, or you don't have access to it.
			</p>
			<Button variant="outline" class="cursor-pointer" href="/projects/{page.params.id}">
				Back to project
			</Button>
		</div>
	{:else if !detail || !location || !selectedVersion}
		<div class="flex h-full w-full items-center justify-center">
			<Spinner class="h-10 w-10" />
		</div>
	{:else}
		<!-- Breadcrumbs: where the document really is -->
		<Breadcrumb.Root>
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/projects">Projects</Breadcrumb.Link>
				</Breadcrumb.Item>

				{#each location.ancestors as ancestor (ancestor.id)}
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						<Breadcrumb.Link href="/projects/{ancestor.id}">{ancestor.title}</Breadcrumb.Link>
					</Breadcrumb.Item>
				{/each}

				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Link href={folderHref}>{location.project.title}</Breadcrumb.Link>
				</Breadcrumb.Item>

				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page>{detail.title}</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>

		<!-- Header -->
		<div class="flex items-center gap-3">
			<h1 class="min-w-0 truncate text-lg font-semibold">{detail.title}</h1>
			<span class="shrink-0 rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground">
				{detail.typeName}
			</span>
			<span class="shrink-0 text-xs text-muted-foreground">v{selectedVersion.versionNumber}</span>

			<div class="ml-auto flex shrink-0 items-center gap-2">
				{#if canEdit}
					<Button variant="outline" class="cursor-pointer" onclick={() => (shareOpen = true)}>
						<UserPlus size={16} />
						Share
					</Button>
				{/if}

				<Button variant="outline" class="cursor-pointer" onclick={download}>
					<Download size={16} />
					Download
				</Button>

				<Button variant="outline" class="cursor-pointer" onclick={toggleChat}>
					<MessageSquare size={16} />
					Chat
				</Button>

				<Button
					variant="ghost"
					size="icon"
					class="cursor-pointer"
					aria-label="Toggle details"
					onclick={() => (showDetails = !showDetails)}
				>
					<PanelRight size={16} />
				</Button>

				{#if canEdit}
					<DropdownMenu.Root>
						<DropdownMenu.Trigger
							class="flex h-9 w-9 cursor-pointer items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground"
							aria-label="More actions"
						>
							<Ellipsis size={16} />
						</DropdownMenu.Trigger>

						<DropdownMenu.Content align="end">
							<DropdownMenu.Item class="cursor-pointer" onclick={() => (editOpen = true)}>
								<Pencil size={14} />
								Edit details
							</DropdownMenu.Item>

							<DropdownMenu.Item class="cursor-pointer" onclick={() => (moveOpen = true)}>
								<FolderInput size={14} />
								Move
							</DropdownMenu.Item>

							<DropdownMenu.Item class="cursor-pointer" onclick={() => (uploadOpen = true)}>
								<Upload size={14} />
								Upload new version
							</DropdownMenu.Item>

							<DropdownMenu.Separator />

							<DropdownMenu.Item
								class="cursor-pointer text-destructive focus:text-destructive"
								onclick={deleteDocument}
							>
								Delete
							</DropdownMenu.Item>
						</DropdownMenu.Content>
					</DropdownMenu.Root>
				{/if}
			</div>
		</div>

		{#if !isLatest}
			<div class="flex items-center gap-3 rounded-md bg-muted px-3 py-2 text-sm">
				You're viewing version {selectedVersion.versionNumber}, not the current version.
				<button
					class="cursor-pointer text-primary underline-offset-2 hover:underline"
					onclick={() => (selectedVersionId = null)}
				>
					Go to current version
				</button>
			</div>
		{/if}

		<!-- Body -->
		<div class="flex min-h-0 flex-1 gap-4">
			<!-- Preview -->
			<div class="flex min-w-0 flex-1 flex-col overflow-hidden rounded-md border">
				{#if previewReady}
					<!-- The browser's own PDF viewer: search, zoom and print without a library -->
					{#key selectedVersion.id}
						<iframe
							src={previewUrl(detail.id, selectedVersion.id)}
							title="{detail.title} v{selectedVersion.versionNumber}"
							class="h-full w-full"
						></iframe>
					{/key}
				{:else if selectedVersion.previewStatus === 'Failed'}
					<div class="flex h-full flex-col items-center justify-center gap-3 p-6 text-center">
						<TriangleAlert size={24} class="text-muted-foreground" />
						<p class="max-w-sm text-sm text-muted-foreground">
							The preview couldn't be created. It's retried automatically; in the meantime you can
							download the original file.
						</p>

						<Button variant="outline" class="cursor-pointer" onclick={download}>
							<Download size={16} />
							Download
						</Button>
					</div>
				{:else}
					<dic class="flex h-full flex-col items-center justify-center gap-3 text-center">
						<Spinner class="h-6 w-6" />
						<p class="text-sm text-muted-foreground">Preparing the preview...</p>
					</dic>
				{/if}
			</div>

			<!-- Details -->
			{#if showDetails}
				<aside class="flex w-72 shrink-0 flex-col gap-6 overflow-y-auto text-sm">
					<section class="flex flex-col gap-2">
						<h2 class="text-xs font-medium tracking-wide text-muted-foreground uppercase">
							Details
						</h2>
						<dl class="grid grid-cols-[auto_1fr] gap-x-3 gap-y-2">
							<dt class="text-muted-foreground">Type</dt>
							<dd>{detail.typeName}</dd>

							<dt class="text-muted-foreground">Created</dt>
							<dd>{formatDate(detail.createdOn)}</dd>

							<dt class="text-muted-foreground">File</dt>
							<dd class="truncate">
								{selectedVersion.originalFileName}.{selectedVersion.extension} ({formatFileSize(
									selectedVersion.sizeBytes
								)})
							</dd>

							<dt class="text-muted-foreground">Chat</dt>
							<dd>
								{#if detail.latestVersion.embeddingStatus === 'Completed'}
									Searchable in the project chat
								{:else if detail.latestVersion.embeddingStatus === 'Failed'}
									Not searchable yet, retried automatically
								{:else}
									Being indexed...
								{/if}
							</dd>
						</dl>
					</section>

					<section class="flex flex-col gap-2">
						<h2 class="text-xs font-medium tracking-wide text-muted-foreground uppercase">
							Access
						</h2>
						<div class="flex items-center gap-2">
							{#if VisibilityIcon}<VisibilityIcon
									size={14}
									class="shrink-0 text-muted-foreground"
								/>{/if}
							<span>
								{visibilityLabels[detail.visibility]}{detail.visibility === 'Restricted'
									? ''
									: ` can ${detail.generalAccessType === 'Editor' ? 'edit' : 'view'}`}
							</span>
						</div>

						{#if detail.grants.length > 0}
							<p class="text-muted-foreground">
								Plus {detail.grants.length}
								{detail.grants.length === 1 ? 'person' : 'people'} with personal access
							</p>
						{/if}

						<p class="text-xs text-muted-foreground">
							You can {detail.accessLevel === 'Editor' ? 'edit' : 'view'} this document.
						</p>

						{#if canEdit}
							<button
								class="cursor-pointer self-start text-xs text-primary underline-offset-2 hover:underline"
								onclick={() => (shareOpen = true)}
							>
								Manage access
							</button>
						{/if}
					</section>

					<section class="flex flex-col gap-2">
						<div class="flex items-center justify-between">
							<h2 class="text-xs font-medium tracking-wide text-muted-foreground uppercase">
								Versions
							</h2>

							{#if canEdit}
								<button
									class="flex cursor-pointer items-center gap-1 text-xs text-muted-foreground hover:text-foreground"
									onclick={() => (uploadOpen = true)}
								>
									<Upload size={12} />
									New version
								</button>
							{/if}
						</div>

						<ul class="flex flex-col gap-1">
							{#each detail.versions as version (version.id)}
								<li>
									<button
										class="flex w-full cursor-pointer flex-col items-start gap-0.5 rounded-md px-2 py-1.5 text-left transition-colors {version.id ===
										selectedVersion.id
											? 'bg-accent'
											: 'hover:bg-accent/50'}"
										onclick={() => (selectedVersionId = version.id)}
									>
										<span class="flex items-center gap-2">
											<span class="font-medium">v{version.versionNumber}</span>

											{#if version.id === detail.latestVersion.id}
												<span class="rounded-full bg-primary/10 px-1.5 text-xs text-primary"
													>Current</span
												>
											{/if}

											{#if isProcessing(version)}<Spinner class="h-3 w-3" />{/if}
										</span>

										<span class="text-xs text-muted-foreground">
											{version.uploadedBy.name} · {formatDate(version.uploadedOn)}
										</span>

										{#if version.note}
											<span class="text-xs">{version.note}</span>
										{/if}
									</button>
								</li>
							{/each}
						</ul>
					</section>
				</aside>
			{/if}
		</div>
	{/if}
</div>

{#if detail}
	<UploadDocumentDialog
		bind:open={uploadOpen}
		projectId={detail.projectId}
		versionOf={detail}
		onuploaded={onVersionUploaded}
	/>

	<ShareDocumentDialog bind:open={shareOpen} documentId={detail.id} onsaved={load} />
	<MoveDocumentDialog bind:open={moveOpen} document={detail} onmoved={load} />
	<EditDocumentDialog bind:open={editOpen} document={detail} onsaved={load} />
{/if}
