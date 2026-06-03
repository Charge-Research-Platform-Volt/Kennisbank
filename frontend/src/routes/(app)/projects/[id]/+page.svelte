<script lang="ts">
	import { getContext, onMount, tick } from 'svelte';
	import { debounce } from '$lib/utils/debounce';
	import { afterNavigate, goto } from '$app/navigation';
	import type { ProjectInfo } from '$lib/types/project';
	import { api } from '$lib/api';
	import {
		Search,
		X,
		Folder,
		ExternalLink,
		Download,
		Tags,
		Users,
		FolderPlus,
		FolderUp,
		Plus,
		Check,
		Pencil,
		CirclePlus,
		MessageSquare
	} from '@lucide/svelte';
	import type { ResourceItem } from '$lib/types/resource';
	import * as Breadcrumb from '$lib/components/ui/breadcrumb';
	import * as Table from '$lib/components/ui/table';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import { getFileIcon, getFileAction } from '$lib/utils/icons';
	import { page } from '$app/state';
	import { openFile } from '$lib/utils/openFile';
	import Avatar from '$lib/components/ui/avatar/avatar.svelte';
	import * as Popover from '$lib/components/ui/popover';
	import Label from '$lib/components/ui/label/label.svelte';
	import { userState } from '$lib/state/user.svelte';
	import { confirm } from '$lib/state/confirm.svelte';
	import * as Command from '$lib/components/ui/command';
	import * as ContextMenu from '$lib/components/ui/context-menu';
	import * as Dialog from '$lib/components/ui/dialog';
	import Button from '$lib/components/ui/button/button.svelte';
	import Input from '$lib/components/ui/input/input.svelte';
	import Textarea from '$lib/components/ui/textarea/textarea.svelte';
	import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';
	import { toast } from 'svelte-sonner';
	import type { ListItem, PagedResult } from '$lib/types/results';

	const openInspector: (item: ResourceItem) => void = getContext('openInspector');
	const registerRefresh: (fn: () => void) => void = getContext('registerRefresh');
	const toggleChat: () => void = getContext('toggleChat');

	const MAX_VISIBLE_TAGS = 5;

	let projectInfo = $state<ProjectInfo | null>(null);
	let loading = $state(false);

	let searchInput = $state('');

	let filteredFolders = $derived(
		projectInfo?.folders.filter((e) =>
			e.title.toLowerCase().includes(searchInput.toLowerCase())
		) ?? []
	);
	let filteredItems = $derived(
		projectInfo?.items.filter((e) =>
			e.name.toLowerCase().includes(searchInput.toLowerCase())
		) ?? []
	);

	let visibleTags = $derived(projectInfo?.tags.filter(Boolean).slice(0, MAX_VISIBLE_TAGS) ?? []);
	let hiddenTagCount = $derived((projectInfo?.tags.filter(Boolean).length ?? 0) - MAX_VISIBLE_TAGS);

	async function fetchProject() {
		loading = true;

		try {
			const result = await api.get<ProjectInfo>(`/api/projects/${page.params.id}`);
			projectInfo = result;
		} finally {
			loading = false;
		}
	}

	// Add folder
	let addingFolder = $state(false);
	let newFolderName = $state('');

	async function confirmAddFolder() {
		if (!newFolderName.trim()) {
			addingFolder = false;
			return;
		}

		try {
			await api.put(`/api/projects/${page.params.id}/folders`, { name: newFolderName.trim() });
			await fetchProject();
		} finally {
			addingFolder = false;
			newFolderName = '';
		}
	}

	async function cancelAddFolder() {
		addingFolder = false;
		newFolderName = '';
	}

	function autofocus(node: HTMLElement) {
		tick().then(() => {
			node.focus();
			if (node instanceof HTMLInputElement) node.select();
		});
	}

	// Add item
	let addItemOpen = $state(false);
	let itemSearch = $state('');
	let itemSearchResults = $state<ResourceItem[]>([]);
	let itemSearchLoading = $state(false);
	let addedItemIds = $derived(new Set(projectInfo?.items.map((e) => e.id) ?? []));

	async function searchItems() {
		itemSearchLoading = true;

		try {
			const result = await api.post<PagedResult<ResourceItem>>(
				'/api/library',
				{
					page: 1,
					pageSize: 20,
					search: itemSearch || undefined,
					filterOptions: {}
				}
			);

			itemSearchResults = result.items;
		} finally {
			itemSearchLoading = false;
		}
	}

	async function addItem(itemId: string) {
		try {
			await api.post(`/api/projects/${page.params.id}/items/${itemId}`, {});
			await fetchProject();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to add item.');
		}
	}

	const onItemSearchInput = debounce(searchItems);

	async function removeItem(itemId: string) {
		try {
			await api.delete(`/api/projects/${page.params.id}/items/${itemId}`);
			await fetchProject();
			toast.success('Successfully removed item.');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to remove item.');
		}
	}

	async function deleteFolder(folderId: string) {
		try {
			await api.delete(`/api/projects/${folderId}`);
			await fetchProject();
			toast.success('Successfully deleted folder.');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to delete folder.');
		}
	}

	// Rename
	let renamingFolderId = $state<string | null>(null);
	let renameFolderName = $state('');

	async function confirmRename() {
		if (!renameFolderName.trim() || !renamingFolderId) {
			renamingFolderId = null;
			return;
		}

		try {
			await api.patch(`/api/projects/${renamingFolderId}`, {
				title: renameFolderName.trim()
			});
			await fetchProject();

			cancelRename();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to rename folder.');
		}
	}

	function cancelRename() {
		renamingFolderId = null;
		renameFolderName = '';
	}

	// Edit project
	let editOpen = $state(false);
	let editTitle = $state('');
	let editDescription = $state('');
	let editTags = $state<string[]>([]);
	let editMembers = $state<string[]>([]);
	let editSubmitting = $state(false);

	function openEditDialog() {
		editTitle = projectInfo!.rootProject.title;
		editDescription = projectInfo!.rootProject.description ?? '';
		editTags = projectInfo!.tags.filter(Boolean).map((t) => t!.id);
		editMembers = projectInfo!.members.map((c) => c.id);
		editOpen = true;
	}

	async function submitEdit() {
		if (!editTitle.trim()) return;

		editSubmitting = true;

		try {
			await api.patch(`/api/projects/${projectInfo!.rootProject.id}`, {
				title: editTitle.trim(),
				description: editDescription.trim() || null,
				tags: editTags,
				members: editMembers
			});

			editOpen = false;
			toast.success('Project updated.');
			await fetchProject();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to update project.');
		} finally {
			editSubmitting = false;
		}
	}

	async function deleteProject() {
		try {
			await api.delete(`/api/projects/${projectInfo!.rootProject.id}`);
			toast.success('Successfully deleted project.');
			goto('/projects');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to delete the project.');
		}
	}

	async function searchTags(q: string) {
		const result = await api.get<PagedResult<ListItem>>(`/api/tags?search=${encodeURIComponent(q)}`);
		return result.items;
	}

	async function searchUsers(q: string) {
		const result = await api.get<{ users: { id: string; firstName: string; lastName: string }[] }>(
			`/api/users?page=1&pageSize=20&search=${encodeURIComponent(q)}&excludeId=${userState.user?.id ?? ''}`
		);

		return result.users.map((u) => ({ id: u.id, name: `${u.firstName} ${u.lastName}` }));
	}

	// Run on page load
	registerRefresh(fetchProject);
	onMount(() => fetchProject());
	afterNavigate(() => fetchProject());
</script>

<div class="flex h-full flex-1 flex-col gap-4 overflow-hidden p-4">
	<!-- Header -->
	<div class="flex items-center gap-4">
		<!-- Searchbar -->
		<div
			class="flex flex-1 items-center gap-2 rounded-md border border-input bg-background px-3 focus-within:border-ring focus-within:ring-2 focus-within:ring-ring/50"
		>
			<Search size={16} class="shrink-0 text-muted-foreground" />
			<input
				class="flex-1 bg-transparent py-1.5 text-sm outline-none placeholder:text-muted-foreground"
				value={searchInput}
				oninput={(e) => (searchInput = e.currentTarget.value)}
				placeholder="Search..."
			/>
			{#if searchInput}
				<X
					size={16}
					class="shrink-0 cursor-pointer text-zinc-600"
					onclick={() => (searchInput = '')}
				/>
			{/if}
		</div>

		<!-- Edit button -->
		{#if projectInfo}
			<Button class="cursor-pointer" onclick={openEditDialog}>
				<Pencil size={16} />
				Edit Project
			</Button>
			<Button variant="outline" class="cursor-pointer" onclick={toggleChat}>
				<MessageSquare size={16} />
				Chat
			</Button>
		{/if}
	</div>

	{#if loading}
		<div class="flex h-full w-full items-center justify-center">
			<Spinner class="h-10 w-10" />
		</div>
	{:else if projectInfo}
		<!-- Breadcrumbs -->
		<Breadcrumb.Root>
			<Breadcrumb.List>
				<Breadcrumb.Item>
					<Breadcrumb.Link href="/projects">Projects</Breadcrumb.Link>
				</Breadcrumb.Item>

				{#each projectInfo.ancestors as ancestor (ancestor.id)}
					<Breadcrumb.Separator />
					<Breadcrumb.Item>
						<Breadcrumb.Link href="/projects/{ancestor.id}">{ancestor.title}</Breadcrumb.Link>
					</Breadcrumb.Item>
				{/each}

				<Breadcrumb.Separator />
				<Breadcrumb.Item>
					<Breadcrumb.Page>{projectInfo.project.title}</Breadcrumb.Page>
				</Breadcrumb.Item>
			</Breadcrumb.List>
		</Breadcrumb.Root>

		<!-- Info strip -->
		<div class="flex items-center justify-start gap-10">
			<!-- Member avatars -->
			<div class="flex items-center gap-2">
				<Users size={14} class="shrink-0 text-muted-foreground" />
				<div class="flex items-center">
					{#each projectInfo.members as member (member.id)}
						<Avatar
							userId={member.id.toString()}
							name="{member.firstName} {member.lastName}"
							customAvatarVersion={member.customAvatarVersion ?? null}
							size={28}
							class="-ml-2 first:ml-0"
						/>
					{/each}
				</div>
			</div>

			<!-- Tags -->
			<div class="flex items-center gap-2">
				<Tags size={14} class="shrink-0 text-muted-foreground" />
				{#if visibleTags.length === 0}
					<span class="text-xs text-muted-foreground">No tags</span>
				{:else}
					<div class="flex flex-wrap gap-1">
						{#each visibleTags as tag (tag?.id)}
							<span class="rounded-full bg-primary/10 px-2 py-0.5 text-xs text-primary"
								>{tag?.name}</span
							>
						{/each}

						{#if hiddenTagCount > 0}
							<Popover.Root>
								<Popover.Trigger
									class="cursor-pointer rounded-full bg-muted px-2 py-0.5 text-xs text-muted-foreground hover:bg-muted/80"
								>
									+{hiddenTagCount} more
								</Popover.Trigger>
								<Popover.Content class="flex w-64 flex-wrap gap-1">
									<Label class="w-full pb-3">Tags:</Label>
									{#each projectInfo.tags.filter(Boolean) as tag (tag?.id)}
										<span class="rounded-full bg-primary/10 px-2 py-0.5 text-xs text-primary"
											>{tag?.name}</span
										>
									{/each}
								</Popover.Content>
							</Popover.Root>
						{/if}
					</div>
				{/if}
			</div>
		</div>

		<!-- Description -->
		{#if projectInfo.rootProject.description}
			<p class="text-sm text-muted-foreground">{projectInfo.rootProject.description}</p>
		{/if}

		<!-- Content -->
		<Table.Root>
			<Table.Header>
				<tr class="border-b">
					<Table.Head class="w-full">
						<div class="flex items-center gap-4">
							Name
							<button
								class="flex cursor-pointer items-center gap-1.5 text-xs font-normal text-muted-foreground hover:text-foreground"
								onclick={() => (addingFolder = true)}
							>
								<FolderPlus size={13} class="shrink-0" />
								New folder
							</button>
							<button
								class="flex cursor-pointer items-center gap-1.5 text-xs font-normal text-muted-foreground hover:text-foreground"
								onclick={() => {
									addItemOpen = true;
									searchItems();
								}}
							>
								<Plus size={13} class="shrink-0" />
								Add item
							</button>
							<button
								class="flex cursor-pointer items-center gap-1.5 text-xs font-normal text-muted-foreground hover:text-foreground"
								onclick={() => goto(`/new?projectId=${page.params.id}`)}
							>
								<CirclePlus size={13} class="shrink-0" />
								Create item
							</button>
						</div>
					</Table.Head>
					<Table.Head class="w-px whitespace-nowrap">Added By</Table.Head>
					<Table.Head class="w-px whitespace-nowrap"></Table.Head>
				</tr>
			</Table.Header>

			<Table.Body>
				{#if projectInfo?.ancestors && projectInfo?.ancestors.length > 0}
					<Table.Row
						class="cursor-pointer"
						onclick={() =>
							goto(`/projects/${projectInfo?.ancestors[projectInfo?.ancestors.length - 1].id}`)}
					>
						<Table.Cell colspan={3} class="py-3">
							<div class="flex items-center gap-3">
								<FolderUp size={16} class="shrink-0 text-muted-foreground" />
								..
							</div>
						</Table.Cell>
					</Table.Row>
				{/if}

				{#if addingFolder}
					<Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
						<Table.Cell class="py-3">
							<div class="flex items-center gap-3">
								<Folder size={16} class="shrink-0 text-muted-foreground" />
								<input
									class="w-full bg-transparent text-sm outline-none"
									bind:value={newFolderName}
									placeholder="Folder name..."
									onkeydown={(e) => {
										if (e.key === 'Enter') confirmAddFolder();
										else if (e.key === 'Escape') cancelAddFolder();
									}}
									use:autofocus
								/>
							</div>
						</Table.Cell>
						<Table.Cell class="text-xs whitespace-nowrap text-muted-foreground">
							{userState.user?.firstName}
							{userState.user?.lastName}
						</Table.Cell>
						<Table.Cell />
					</Table.Row>
				{/if}

				{#if filteredFolders.length === 0 && filteredItems.length === 0}
					<Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
						<Table.Cell colspan={3} class="py-12 text-center text-sm text-muted-foreground">
							{searchInput ? 'No results found.' : 'No folders or items yet.'}
						</Table.Cell>
					</Table.Row>
				{/if}

				<!-- Folders -->
				{#each filteredFolders as entry (entry.id)}
					<ContextMenu.Root>
						<ContextMenu.Trigger>
							{#snippet child({ props })}
								<Table.Row
									{...props}
									class="cursor-pointer {props.class ?? ''}"
									onclick={() => goto(`/projects/${entry.id}`)}
								>
									<Table.Cell class="py-3">
										<div class="flex items-center gap-3">
											<Folder size={16} class="shrink-0 text-muted-foreground" />
											{#if renamingFolderId === entry.id}
												<input
													class="w-full bg-transparent text-sm outline-none"
													bind:value={renameFolderName}
													onkeydown={(e) => {
														if (e.key === 'Enter') confirmRename();
														else if (e.key === 'Escape') cancelRename();
													}}
													use:autofocus
												/>
											{:else}
												{entry.title}
											{/if}
										</div>
									</Table.Cell>
									<Table.Cell class="text-xs whitespace-nowrap text-muted-foreground"
										>{entry.addedBy}</Table.Cell
									>
									<Table.Cell></Table.Cell>
								</Table.Row>
							{/snippet}
						</ContextMenu.Trigger>

						<ContextMenu.Content>
							<ContextMenu.Item
								onclick={() => {
									renamingFolderId = entry.id;
									renameFolderName = entry.title;
								}}>Rename</ContextMenu.Item
							>
							<ContextMenu.Separator />
							<ContextMenu.Item
								class="text-destructive focus:text-destructive"
								onclick={async () => {
									if (
										await confirm(`Delete folder "${entry.title}"? This cannot be undone.`)
									)
										await deleteFolder(entry.id);
								}}>Delete</ContextMenu.Item
							>
						</ContextMenu.Content>
					</ContextMenu.Root>
				{/each}

				<!-- Items -->
				{#each filteredItems as entry (entry.id)}
					{@const Icon = getFileIcon(entry.fileType)}
					{@const action = getFileAction(entry.fileType)}

					<ContextMenu.Root>
						<ContextMenu.Trigger>
							{#snippet child({ props })}
								<Table.Row
									{...props}
									class="cursor-pointer {props.class ?? ''}"
									onclick={() => openInspector(entry)}
								>
									<Table.Cell class="py-3">
										<div class="flex items-center gap-3">
											<Icon size={16} class="shrink-0 text-muted-foreground" />
											{entry.name}
										</div>
									</Table.Cell>
									<Table.Cell class="text-xs whitespace-nowrap text-muted-foreground"
										>{entry.addedBy}</Table.Cell
									>
									<Table.Cell class="p-3">
										<div class="flex items-center justify-center">
											{#if action === 'open'}
												<button
													class="cursor-pointer"
													onclick={(e) => {
														e.stopPropagation();
														openFile(entry.id, entry.fileType, entry.sourceUrl);
													}}
												>
													<ExternalLink size={14} />
												</button>
											{:else if action === 'download'}
												<button
													class="cursor-pointer"
													onclick={(e) => {
														e.stopPropagation();
														openFile(entry.id, entry.fileType, entry.sourceUrl);
													}}
												>
													<Download size={14} />
												</button>
											{/if}
										</div>
									</Table.Cell>
								</Table.Row>
							{/snippet}
						</ContextMenu.Trigger>

						<ContextMenu.Content>
							<ContextMenu.Item
								class="cursor-pointer text-destructive focus:text-destructive"
								onclick={async () => {
									if (
										await confirm(
											`Remove "${entry.name}"? This will not delete it from the library.`
										)
									)
										await removeItem(entry.id);
								}}
							>
								Remove
							</ContextMenu.Item>
						</ContextMenu.Content>
					</ContextMenu.Root>
				{/each}
			</Table.Body>
		</Table.Root>
	{/if}
</div>

<!-- Add item command dialog -->
{#key addItemOpen}
	<Command.Dialog
		class="px-1 py-2"
		bind:open={addItemOpen}
		onOpenChange={() => {
			if (!addItemOpen) {
				itemSearch = '';
				itemSearchResults = [];
			}
		}}
		shouldFilter={false}
	>
		<Command.Input
			placeholder="Search library..."
			bind:value={itemSearch}
			oninput={onItemSearchInput}
		/>
		<Command.List>
			{#if itemSearchLoading}
				<Command.Loading class="py-5 text-center">Searching...</Command.Loading>
			{:else if itemSearchResults.length === 0}
				<Command.Empty>No results found.</Command.Empty>
			{:else}
				<Command.Group heading="Results">
					{#each itemSearchResults as item (item.id)}
						{@const Icon = getFileIcon(item.fileType)}
						{@const alreadyAdded = addedItemIds.has(item.id)}

						<Command.Item
							class="py-2"
							onSelect={() => {
								if (!alreadyAdded) addItem(item.id);
							}}
							disabled={alreadyAdded}
						>
							<Icon size={14} class="text-muted-foreground" />
							{item.name}
							{#if alreadyAdded}
								<Check size={12} class="ml-auto text-primary" />
							{/if}
						</Command.Item>
					{/each}
				</Command.Group>
			{/if}
		</Command.List>
	</Command.Dialog>
{/key}

<!-- Edit project dialog -->
<Dialog.Root bind:open={editOpen}>
	<Dialog.Content>
		<Dialog.Header>
			<Dialog.Title>Edit Project</Dialog.Title>
		</Dialog.Header>

		<div class="flex flex-col gap-4 py-2">
			<div class="flex flex-col gap-1.5">
				<Label>Title</Label>
				<Input bind:value={editTitle} />
			</div>
			<div class="flex flex-col gap-1.5">
				<Label>Description</Label>
				<Textarea bind:value={editDescription} />
			</div>
			<div class="flex flex-col gap-1.5">
				<Label>Tags</Label>
				<AsyncMultiSelect
					bind:value={editTags}
					search={searchTags}
					placeholder="Tags"
					class="w-full"
				/>
			</div>
			<div class="flex flex-col gap-1.5">
				<Label>Co-members</Label>
				<AsyncMultiSelect
					bind:value={editMembers}
					search={searchUsers}
					placeholder="Co-members"
					class="w-full"
				/>
			</div>
		</div>

		<Dialog.Footer>
			<div class="flex w-full justify-between">
				<Button
					variant="outline"
					class="cursor-pointer border-destructive text-destructive hover:bg-destructive/10 hover:text-destructive"
					onclick={async () => {
						editOpen = false;
						if (
							await confirm(
								`Delete project "${projectInfo?.project.title || 'Unknown'}"? This cannot be undone.`
							)
						)
							await deleteProject();
					}}
				>
					Delete project
				</Button>

				<div class="flex gap-2">
					<Button class="cursor-pointer" variant="outline" onclick={() => (editOpen = false)}
						>Cancel</Button
					>
					<Button class="cursor-pointer" onclick={submitEdit} disabled={editSubmitting}>Save</Button
					>
				</div>
			</div>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
