<script lang="ts">
    import { getContext, onMount, tick } from 'svelte';
    import { afterNavigate, goto } from "$app/navigation";
    import type { ProjectInfo } from "$lib/types/project"
    import { api } from "$lib/api";
    import { Search, X, Folder, ExternalLink, Download, Tags, Users, FolderPlus, FolderUp, Plus, Check, Pencil } from 'lucide-svelte';
    import type { ResourceItem } from "$lib/types/resource";
    import * as Breadcrumb from "$lib/components/ui/breadcrumb";
    import * as Table from "$lib/components/ui/table";
    import Spinner from "$lib/components/ui/spinner/spinner.svelte";
    import { getFileIcon, getFileAction } from "$lib/utils/icons";
	import { page } from "$app/state";
	import { openFile } from '$lib/utils/openFile';
	import Avatar from '$lib/components/ui/avatar/avatar.svelte';
    import * as Popover from "$lib/components/ui/popover";
	import Label from '$lib/components/ui/label/label.svelte';
    import { userState } from '$lib/state/user.svelte';
    import * as Command from "$lib/components/ui/command";
    import * as ContextMenu from "$lib/components/ui/context-menu";
    import * as Dialog from "$lib/components/ui/dialog";
    import Button from '$lib/components/ui/button/button.svelte';
    import Input from '$lib/components/ui/input/input.svelte';
    import Textarea from '$lib/components/ui/textarea/textarea.svelte';
    import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';

    const openInspector: (item: ResourceItem) => void = getContext('openInspector');
    const registerTableRefresh: (fn: () => void) => void = getContext('registerTableRefresh');

    const MAX_VISIBLE_TAGS = 5;

    let projectInfo = $state<ProjectInfo | null>(null);
    let loading = $state(false);

    let searchInput = $state('');
    
    let filteredFolders = $derived(projectInfo?.folders.filter(e => e.folder.title.toLowerCase().includes(searchInput.toLowerCase())) ?? []);
    let filteredItems = $derived(projectInfo?.items.filter(e => e.item.name.toLowerCase().includes(searchInput.toLowerCase())) ?? []);

    let visibleTags = $derived(projectInfo?.tags.filter(Boolean).slice(0, MAX_VISIBLE_TAGS) ?? []);
    let hiddenTagCount = $derived((projectInfo?.tags.filter(Boolean).length ?? 0) - MAX_VISIBLE_TAGS);

    async function fetchProject() {
        loading = true;
        
        try {
            const result = await api.get<ProjectInfo>(`/api/project/info/${page.params.id}`);
            projectInfo = result.body;
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
            await api.put(`/api/project/add-folder/${page.params.id}`, { name: newFolderName.trim() });
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
    let addedItemIds = $derived(new Set(projectInfo?.items.map(e => e.item.id) ?? []));

    let debounceTimer: ReturnType<typeof setTimeout>;

    async function searchItems() {
        itemSearchLoading = true;
        
        try {
            const result = await api.post<{ items: ResourceItem[]; totalCount: number; }>('/api/resources/grid', {
                pageIndex: 1,
                pageSize: 20,
                searchQuery: itemSearch || undefined,
                filterOptions: {}
            });

            itemSearchResults = result.body?.items ?? [];
        } finally {
            itemSearchLoading = false;
        }
    }

    async function addItem(itemId: string) {
        await api.put(`/api/project/add-item/${page.params.id}/${itemId}`, {});
        await fetchProject();
    }

    function onItemSearchInput() {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(searchItems, 300);
    }

    // Confirm dialog
    let confirmOpen = $state(false);
    let confirmMessage = $state('');
    let pendingAction = $state<(() => Promise<void>) | null>(null);

    function askConfirm(message: string, action: () => Promise<void>) {
        confirmMessage = message;
        pendingAction = action;
        confirmOpen = true;
    }

    async function removeItem(itemId: string) {
        await api.delete(`/api/project/remove-item/${page.params.id}/${itemId}`);
        await fetchProject();
    }

    async function deleteFolder(folderId: string) {
        await api.delete(`/api/project/delete/${folderId}`);
        await fetchProject();
    }

    // Rename
    let renamingFolderId = $state<string | null>(null);
    let renameFolderName = $state('');

    async function confirmRename() {
        if (!renameFolderName.trim() || !renamingFolderId) {
            renamingFolderId = null;
            return;
        }

        await api.patch(`/api/project/update/${renamingFolderId}`, { title: renameFolderName.trim() });
        await fetchProject();

        cancelRename();
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
    let editCreators = $state<string[]>([]);
    let editSubmitting = $state(false);
    let editError = $state('');

    function openEditDialog() {
        editTitle = projectInfo!.rootProject.title;
        editDescription = projectInfo!.rootProject.description ?? '';
        editTags = projectInfo!.tags.filter(Boolean).map(t => t!.id);
        editCreators = projectInfo!.creators.map(c => c.id);
        editOpen = true;
    }

    async function submitEdit() {
        if (!editTitle.trim()) return;

        editSubmitting = true;
        editError = '';

        try {
            await api.patch(`/api/project/update/${projectInfo!.rootProject.id}`, {
                title: editTitle.trim(),
                description: editDescription.trim() || null,
                tags: editTags,
                creators: editCreators
            });

            editOpen = false;
            await fetchProject();
        } catch (e) {
            editError = e instanceof Error ? e.message : 'Failed to update project.';
        } finally {
            editSubmitting = false;
        }
    }

    async function deleteProject() {
        await api.delete(`/api/project/delete/${projectInfo!.rootProject.id}`);
        goto('/projects');
    }

    async function searchTags(q: string) {
        const result = await api.post<{ tags: { id: string; name: string; }[] }>('/api/tags/tags', {
            usePaging: true, pageIndex: 1, pageSize: 20,
            searchQuery: q, includeUsageCount: false, includeCanEditAndDelete: false
        });

        return result.body.tags;
    }

    async function searchUsers(q: string) {
        const result = await api.get<{ users: { id: string; firstName: string; lastName: string; }[] }>(
            `/api/user/list-paged?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&excludeId=${userState.user?.id ?? ''}`
        );

        return result.body.users.map(u => ({ id: u.id, name: `${u.firstName} ${u.lastName}` }));
    }

    // Run on page load
    registerTableRefresh(fetchProject);
    onMount(() => fetchProject());
    afterNavigate(() => fetchProject());
</script>

<div class="flex flex-col flex-1 h-full p-4 gap-4 overflow-hidden">
    <!-- Header -->
    <div class="flex items-center gap-4">
        <!-- Searchbar -->
        <div class="flex flex-1 items-center gap-2 border border-input rounded-md bg-background px-3 focus-within:ring-2 focus-within:ring-ring/50 focus-within:border-ring">
            <Search size={16} class="text-muted-foreground shrink-0" />
            <input
                class="flex-1 py-1.5 text-sm bg-transparent outline-none placeholder:text-muted-foreground"
                value={searchInput}
                oninput={(e) => searchInput = e.currentTarget.value}
                placeholder="Search..."
            />
            {#if searchInput}
                <X size={16} class="shrink-0 cursor-pointer text-zinc-600" onclick={() => searchInput = ''} />
            {/if}
        </div>

        <!-- Edit button -->
        {#if projectInfo}
            <Button class="cursor-pointer" onclick={openEditDialog}>
                <Pencil size={16} />
                Edit Project
            </Button>
        {/if}
    </div>
    

    {#if loading}
        <div class="flex w-full h-full justify-center items-center">
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
            <!-- Creator avatars -->
            <div class="flex items-center gap-2">
                <Users size={14} class="text-muted-foreground shrink-0" />
                <div class="flex items-center">
                    {#each projectInfo.creators as creator (creator.id)}
                        <Avatar
                            userId={creator.id.toString()}
                            name="{creator.firstName} {creator.lastName}"
                            customAvatarVersion={creator.customAvatarVersion ?? null}
                            size={28}
                            class="-ml-2 first:ml-0"
                        />
                    {/each}
                </div>
            </div>

            <!-- Tags -->
            <div class="flex items-center gap-2">
                <Tags size={14} class="text-muted-foreground shrink-0" />
                {#if visibleTags.length === 0}
                    <span class="text-xs text-muted-foreground">No tags</span>
                {:else}
                    <div class="flex flex-wrap gap-1">
                        {#each visibleTags as tag (tag?.id)}
                            <span class="text-xs px-2 py-0.5 rounded-full bg-primary/10 text-primary">{tag?.name}</span>
                        {/each}

                        {#if hiddenTagCount > 0}
                            <Popover.Root>
                                <Popover.Trigger class="text-xs px-2 py-0.5 rounded-full bg-muted text-muted-foreground cursor-pointer hover:bg-muted/80">
                                    +{hiddenTagCount} more
                                </Popover.Trigger>
                                <Popover.Content class="flex flex-wrap gap-1 w-64">
                                    <Label class="w-full pb-3">Tags:</Label>
                                    {#each projectInfo.tags.filter(Boolean) as tag (tag?.id)}
                                        <span class="text-xs px-2 py-0.5 rounded-full bg-primary/10 text-primary">{tag?.name}</span>
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
                            <button class="flex items-center gap-1.5 text-xs font-normal text-muted-foreground hover:text-foreground cursor-pointer" onclick={() => addingFolder = true}>
                                <FolderPlus size={13} class="shrink-0" />
                                New folder
                            </button>
                            <button class="flex items-center gap-1.5 text-xs font-normal text-muted-foreground hover:text-foreground cursor-pointer" onclick={() => {addItemOpen = true; searchItems(); }}>
                                <Plus size={13} class="shrink-0" />
                                Add item
                            </button>
                        </div>
                    </Table.Head>
                    <Table.Head class="w-px whitespace-nowrap">Added By</Table.Head>
                    <Table.Head class="w-px whitespace-nowrap"></Table.Head>
                </tr>
            </Table.Header>

            <Table.Body>
                {#if projectInfo?.ancestors && projectInfo?.ancestors.length > 0}
                    <Table.Row class="cursor-pointer" onclick={() => goto(`/projects/${projectInfo?.ancestors[projectInfo?.ancestors.length - 1].id}`)}>
                        <Table.Cell colspan={3} class="py-3">
                            <div class="flex gap-3 items-center">
                                <FolderUp size={16} class="text-muted-foreground shrink-0" />
                                ..
                            </div>
                        </Table.Cell>
                    </Table.Row>
                {/if}

                {#if addingFolder}
                    <Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
                        <Table.Cell class="py-3">
                            <div class="flex gap-3 items-center">
                                <Folder size={16} class="text-muted-foreground shrink-0" />
                                <input
                                    class="text-sm bg-transparent outline-none w-full"
                                    bind:value={newFolderName}
                                    placeholder="Folder name..."
                                    onkeydown={(e) => { if (e.key === 'Enter') confirmAddFolder(); else if (e.key === 'Escape') cancelAddFolder(); }}
                                    use:autofocus
                                />
                            </div>
                        </Table.Cell>
                        <Table.Cell class="whitespace-nowrap text-muted-foreground text-xs">
                            {userState.user?.firstName} {userState.user?.lastName}
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
                {#each filteredFolders as entry (entry.folder.id)}
                    <ContextMenu.Root>
                        <ContextMenu.Trigger>
                            {#snippet child({ props })}
                                <Table.Row {...props} class="cursor-pointer {props.class ?? ''}" onclick={() => goto(`/projects/${entry.folder.id}`)}>
                                    <Table.Cell class="py-3">
                                        <div class="flex gap-3 items-center">
                                            <Folder size={16} class="text-muted-foreground shrink-0" />
                                            {#if renamingFolderId === entry.folder.id}
                                                <input
                                                    class="text-sm bg-transparent outline-none w-full"
                                                    bind:value={renameFolderName}
                                                    onkeydown={(e) => { if (e.key === 'Enter') confirmRename(); else if (e.key === 'Escape') cancelRename()}}
                                                    use:autofocus
                                                />
                                            {:else}
                                                {entry.folder.title}
                                            {/if}
                                        </div>
                                    </Table.Cell>
                                    <Table.Cell class="whitespace-nowrap text-muted-foreground text-xs">{entry.addedBy}</Table.Cell>
                                    <Table.Cell></Table.Cell>
                                </Table.Row>
                            {/snippet}
                        </ContextMenu.Trigger>

                        <ContextMenu.Content>
                            <ContextMenu.Item onclick={() => { renamingFolderId = entry.folder.id; renameFolderName = entry.folder.title; }}>Rename</ContextMenu.Item>
                            <ContextMenu.Separator />
                            <ContextMenu.Item class="text-destructive focus:text-destructive" onclick={() => askConfirm(`Delete folder <strong>${entry.folder.title}</strong>? This cannot be undone.`, () => deleteFolder(entry.folder.id))}>Delete</ContextMenu.Item>
                        </ContextMenu.Content>
                    </ContextMenu.Root>
                {/each}

                <!-- Items -->
                {#each filteredItems as entry (entry.item.id)}
                    {@const Icon = getFileIcon(entry.item.fileType)}
                    {@const action = getFileAction(entry.item.fileType)}

                    <ContextMenu.Root>
                        <ContextMenu.Trigger>
                            {#snippet child({ props })}
                                <Table.Row {...props} class="cursor-pointer {props.class ?? ''}" onclick={() => openInspector(entry.item)}>
                                    <Table.Cell class="py-3">
                                        <div class="flex gap-3 items-center">
                                            <Icon size={16} class="text-muted-foreground shrink-0" />
                                            {entry.item.name}
                                        </div>
                                    </Table.Cell>
                                    <Table.Cell class="whitespace-nowrap text-muted-foreground text-xs">{entry.addedBy}</Table.Cell>
                                    <Table.Cell class="p-3">
                                        <div class="flex items-center justify-center">
                                            {#if action === 'open'}
                                                <button class="cursor-pointer" onclick={(e) => { e.stopPropagation(); openFile(entry.item.id, entry.item.fileType); }}>
                                                    <ExternalLink size={14} />
                                                </button>
                                            {:else if action === 'download'}
                                                <button class="cursor-pointer" onclick={(e) => { e.stopPropagation(); openFile(entry.item.id, entry.item.fileType); }}>
                                                    <Download size={14} />
                                                </button>
                                            {/if}
                                        </div>
                                    </Table.Cell>
                                </Table.Row>
                            {/snippet}
                        </ContextMenu.Trigger>

                        <ContextMenu.Content>
                            <ContextMenu.Item class="cursor-pointer text-destructive focus:text-destructive" onclick={() => askConfirm(`Remove <strong>${entry.item.name}</strong>? This will not delete it from the library.`, () => removeItem(entry.item.id))}>
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
    <Command.Dialog class="px-1 py-2" bind:open={addItemOpen} onOpenChange={() => { if (!addItemOpen) { itemSearch = ''; itemSearchResults = [];}}} shouldFilter={false}>
        <Command.Input placeholder="Search library..." bind:value={itemSearch} oninput={onItemSearchInput} />
        <Command.List>
            {#if itemSearchLoading}
                <Command.Loading class="text-center py-5">Searching...</Command.Loading>
            {:else if itemSearchResults.length === 0}
                <Command.Empty>No results found.</Command.Empty>
            {:else}
                <Command.Group heading="Results">
                    {#each itemSearchResults as item (item.id)}
                        {@const Icon = getFileIcon(item.fileType)}
                        {@const alreadyAdded = addedItemIds.has(item.id)}

                        <Command.Item class="py-2" onSelect={() => { if (!alreadyAdded) addItem(item.id); }} disabled={alreadyAdded}>
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

<!-- Confirm dialog -->
<Dialog.Root bind:open={confirmOpen}>
    <Dialog.Content>
        <Dialog.Header>
            <Dialog.Title>Are you sure?</Dialog.Title>
            <!-- eslint-disable-next-line svelte/no-at-html-tags -->
            <Dialog.Description>{@html confirmMessage}</Dialog.Description>
        </Dialog.Header>

        <Dialog.Footer>
            <Button variant="outline" class="cursor-pointer" onclick={() => confirmOpen = false}>Cancel</Button>
            <Button variant="destructive" class="cursor-pointer" onclick={async () => { await pendingAction?.(); confirmOpen = false; }}>Confirm</Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>

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
                <AsyncMultiSelect bind:value={editTags} search={searchTags} placeholder="Tags" class="w-full" />
            </div>
            <div class="flex flex-col gap-1.5">
                <Label>Co-creators</Label>
                <AsyncMultiSelect bind:value={editCreators} search={searchUsers} placeholder="Co-creators" class="w-full" />
            </div>
            {#if editError}
                <p class="text-sm text-destructive">{editError}</p>
            {/if}
        </div>

        <Dialog.Footer>
            <div class="flex w-full justify-between">
                <Button variant="outline" class="cursor-pointer border-destructive text-destructive hover:bg-destructive/10 hover:text-destructive" onclick={() => { editOpen = false; askConfirm(`Delete project <strong>${projectInfo?.project.title || 'Unknown'}</strong>? This cannot be undone.`, deleteProject)}}>
                    Delete project
                </Button>

                <div class="flex gap-2">
                    <Button class="cursor-pointer" variant="outline" onclick={() => editOpen = false}>Cancel</Button>
                    <Button class="cursor-pointer" onclick={submitEdit} disabled={editSubmitting}>Save</Button>
                </div>
            </div>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>
