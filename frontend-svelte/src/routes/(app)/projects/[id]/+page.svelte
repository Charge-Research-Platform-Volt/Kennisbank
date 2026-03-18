<script lang="ts">
    import { getContext, onMount } from 'svelte';
    import { afterNavigate, goto } from "$app/navigation";
    import type { ProjectInfo } from "$lib/types/project"
    import { api } from "$lib/api";
    import { Search, X, Folder, ExternalLink, Download, Tags, Users, FolderPlus, Plus } from 'lucide-svelte';
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

    let addingFolder = $state(false);
    let newFolderName = $state('');

    async function fetchProject() {
        loading = true;
        
        try {
            const result = await api.get<ProjectInfo>(`/api/project/info/${page.params.id}`);
            projectInfo = result.body;
        } finally {
            loading = false;
        }
    }

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
        node.focus();
    }

    registerTableRefresh(fetchProject);
    onMount(() => fetchProject());
    afterNavigate(() => fetchProject());
</script>

<div class="flex flex-col flex-1 h-full p-4 gap-4 overflow-hidden">
    <!-- Searchbar --> 
    <div class="flex items-center gap-2 border border-input rounded-md bg-background px-3 focus-within:ring-2 focus-within:ring-ring/50 focus-within:border-ring">
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
                            <button class="flex items-center gap-1.5 text-xs font-normal text-muted-foreground hover:text-foreground cursor-pointer">
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
                {#if addingFolder}
                    <Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
                        <Table.Cell class="py-3 flex gap-3 items-center">
                            <Folder size={16} class="text-muted-foreground shrink-0" />
                            <input
                                class="text-sm bg-transparent outline-none w-full"
                                bind:value={newFolderName}
                                placeholder="Folder name..."
                                onkeydown={(e) => { if (e.key === 'Enter') confirmAddFolder(); else if (e.key === 'Escape') cancelAddFolder(); }}
                                use:autofocus
                            />
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
                    <Table.Row class="cursor-pointer" onclick={() => goto(`/projects/${entry.folder.id}`)}>
                        <Table.Cell class="py-3 flex gap-3 items-center">
                            <Folder size={16} class="text-muted-foreground shrink-0" />
                            {entry.folder.title}
                        </Table.Cell>
                        <Table.Cell class="whitespace-nowrap text-muted-foreground text-xs">{entry.addedBy}</Table.Cell>
                    </Table.Row>
                {/each}

                <!-- Items -->
                {#each filteredItems as entry (entry.item.id)}
                    {@const Icon = getFileIcon(entry.item.fileType)}
                    {@const action = getFileAction(entry.item.fileType)}

                    <Table.Row class="cursor-pointer" onclick={() => openInspector(entry.item)}>
                        <Table.Cell class="py-3 flex gap-3 items-center">
                            <Icon size={16} class="text-muted-foreground shrink-0" />
                            {entry.item.name}
                        </Table.Cell>
                        <Table.Cell class="whitespace-nowrap text-muted-foreground text-xs">{entry.addedBy}</Table.Cell>
                        <Table.Cell class="p-3 text-center flex items-center">
                            {#if action === 'open'}
                                <button class="cursor-pointer" onclick={(e) => { e.stopPropagation(); openFile(entry.item.id, entry.item.fileType); }}>
                                    <ExternalLink size={14} />
                                </button>
                            {:else if action === 'download'}
                                <button class="cursor-pointer" onclick={(e) => { e.stopPropagation(); openFile(entry.item.id, entry.item.fileType); }}>
                                    <Download size={14} />
                                </button>
                            {/if}
                        </Table.Cell>
                    </Table.Row>
                {/each}
            </Table.Body>
        </Table.Root>
    {/if}
</div>