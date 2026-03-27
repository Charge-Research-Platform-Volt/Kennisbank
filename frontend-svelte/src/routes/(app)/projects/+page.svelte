<script lang="ts">
    import * as Pagination from "$lib/components/ui/pagination";
    import { debounce } from '$lib/utils/debounce';
    import type { PageItem } from "bits-ui";
    import { getParam, getParamInt, setParams } from "$lib/utils/urlState";
    import { onMount } from "svelte";
    import { afterNavigate, goto } from "$app/navigation";
    import type { Project, ProjectListResponse } from "$lib/types/project";
	import { api } from "$lib/api";
    import { Search, X, BookMarked, Plus } from 'lucide-svelte';
    import { userState } from "$lib/state/user.svelte";
    import * as Table from "$lib/components/ui/table";
    import Spinner from "$lib/components/ui/spinner/spinner.svelte";
    import { formatDate } from "$lib/utils/date";
    import Button from "$lib/components/ui/button/button.svelte";
    import * as Dialog from "$lib/components/ui/dialog";
    import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';
    import Textarea from '$lib/components/ui/textarea/textarea.svelte';
    import Input from '$lib/components/ui/input/input.svelte';
    import Label from '$lib/components/ui/label/label.svelte';
	import Avatar from "$lib/components/ui/avatar/avatar.svelte";

    // Search
    let searchInput = $state('');
    
    // Pagination
    const PAGE_SIZE = 50;
    let currentPage = $state(1);
    let totalItems = $state(0);

    // Results
    let items = $state<Project[]>([]);
    let loading = $state(false);

    // Create dialog
    let dialogOpen = $state(false);
    let newTitle = $state('');
    let newDescription = $state('');
    let newTags = $state<string[]>([]);
    let newCreators = $state<string[]>([]);
    let submitting = $state(false);
    let createError = $state('');

    const debouncedFetchProjects = debounce(fetchProjects);

    async function fetchProjects() {
        loading = true;

        try {
            const result = await api.post<ProjectListResponse>('/api/project/list', {
                usePaging: true,
                pageIndex: currentPage,
                pageSize: PAGE_SIZE,
                searchQuery: searchInput || undefined
            });

            items = result.body.projects;
            totalItems = result.body.totalCount ?? 0;
        } finally {
            loading = false;
        }
    }

    async function searchTags(q: string) {
        const result = await api.post<{ tags: { id: string; name: string; }[] }>('/api/tags/tags', {
            usePaging: true, pageIndex: 1, pageSize: 20,
            searchQuery: q, includeUsageCount: false, includeCanEditAndDelete: false
        });

        return result.body.tags;
    }

    async function searchUsers(q: string) {
        const result = await api.get<{ users: { id: string; firstName: string; lastName: string }[] }>(
            `/api/user/list-paged?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&excludeId=${userState.user?.id ?? ''}`
        );

        return result.body.users.map(u => ({ id: u.id.toString(), name: `${u.firstName} ${u.lastName}` }));
    }

    async function createProject() {
        if (!newTitle.trim()) return;

        createError = '';
        submitting = true;

        try {
            const result = await api.put('/api/project/create', {
                title: newTitle.trim(),
                description: newDescription.trim() || null,
                projectType: 'root',
                tags: newTags,
                creators: newCreators
            });

            if (result.success) {
                dialogOpen = false;
                newTitle = '';
                newDescription = '';
                newTags = [];
                newCreators = [];
                await fetchProjects();
            } else {
                createError = result.message ?? 'Failed to create project.';
            }
        } catch (e) {
            createError = e instanceof Error ? e.message : 'Failed to create project.'
        } finally {
            submitting = false;
        }
    }

    function onSearchInput(value: string) {
        searchInput = value;
        currentPage = 1;
        setParams({ q: searchInput || null, page: currentPage });
        debouncedFetchProjects();
    }

    function handlePageChange() {
        setParams({ page: currentPage });
        fetchProjects();
    }

    function syncFromUrl() {
        searchInput = getParam('q');
        currentPage = getParamInt('page');
        fetchProjects();
    }

    onMount(() => syncFromUrl());
    afterNavigate(() => syncFromUrl());
</script>

<div class="flex flex-col flex-1 h-full p-4 gap-4 overflow-hidden">
    <!-- Search bar -->
    <div class="flex items-center gap-2">
        <div class="flex flex-1 items-center gap-2 border border-input rounded-md bg-background px-3 focus-within:ring-2 focus-within:ring-ring/50 focus-within:border-ring">
            <Search size={16} class="text-muted-foreground shrink-0" />
            <input
                class="flex-1 py-1.5 text-sm bg-transparent outline-none placeholder:text-muted-foreground"
                value={searchInput}
                oninput={(e) => onSearchInput(e.currentTarget.value)}
                placeholder="Search projects..."
            />
            {#if searchInput}
                <X size={16} class="shrink-0 cursor-pointer text-zinc-600" onclick={() => onSearchInput('')} />
            {/if}
        </div>

        <Button class="cursor-pointer flex items-center" onclick={() => dialogOpen = true}>
            <Plus size={14} class="shrink-0" />
            Create Project
        </Button>
    </div>

    <!-- List -->
    {#if loading}
        <div class="flex w-full h-full justify-center items-center">
            <Spinner class="h-10 w-10" />
        </div>
    {:else} 
        <Table.Root>
            <Table.Caption>
                <Pagination.Root count={totalItems} perPage={PAGE_SIZE} bind:page={currentPage} onPageChange={handlePageChange}>
                    {#snippet children({ pages, currentPage }: { pages: PageItem[]; currentPage: number; })}
                        <Pagination.Content>
                            <Pagination.Item><Pagination.Previous class="cursor-pointer" /></Pagination.Item>
                            {#each pages as page (page.key)}
                                {#if page.type === 'ellipsis'}
                                    <Pagination.Item><Pagination.Ellipsis /></Pagination.Item>
                                {:else} 
                                    <Pagination.Item>
                                        <Pagination.Link class="cursor-pointer" {page} isActive={currentPage === page.value}>{page.value}</Pagination.Link>
                                    </Pagination.Item>
                                {/if}
                            {/each}
                            <Pagination.Item><Pagination.Next class="cursor-pointer" /></Pagination.Item>
                        </Pagination.Content>
                    {/snippet}
                </Pagination.Root>
            </Table.Caption>

            <Table.Header>
                <tr class="border-b">
                    <Table.Head class="w-full">Name</Table.Head>
                    <Table.Head class="w-px whitespace-nowrap">Creators</Table.Head>
                    <Table.Head class="w-px whitespace-nowrap text-center">Created</Table.Head>
                </tr>
            </Table.Header>

            <Table.Body>
                {#if items.length === 0}
                    <Table.Row class="hover:[&,&>svelte-css-wrapper]:[&>th,td]:bg-transparent">
                        <Table.Cell colspan={3} class="py-12 text-center text-sm text-muted-foreground">
                            {searchInput ? 'No results found.' : 'No projects yet.'}
                        </Table.Cell>
                    </Table.Row>
                {/if}
                {#each items as item (item.id)}
                    <Table.Row class="cursor-pointer" onclick={() => goto(`/projects/${item.id}`)}>
                        <Table.Cell class="py-3">
                            <div class="flex gap-3 items-center">
                                <BookMarked size={16} class="text-muted-foreground shrink-0" />
                                <div class="flex flex-col">
                                    <span>{item.title}</span>
                                    {#if item.description}
                                        <span class="text-xs text-muted-foreground truncate max-w-sm">{item.description}</span>
                                    {/if}
                                </div>
                            </div>
                        </Table.Cell>

                        <Table.Cell class="py-3">
                            <div class="flex items-center">
                                {#each (item.creators ?? []) as creator (creator.id)}
                                    <Avatar
                                        userId={creator.id.toString()}
                                        name="{creator.firstName} {creator.lastName}"
                                        customAvatarVersion={creator.customAvatarVersion ?? null}
                                        size={24}
                                        class="-ml-2 first:ml-0"
                                    />
                                {/each}
                            </div>
                        </Table.Cell>

                        <Table.Cell class="whitespace-nowrap text-center px-5 py-3">{formatDate(item.creationDate)}</Table.Cell>
                    </Table.Row>
                {/each}
            </Table.Body>
        </Table.Root>
    {/if}
</div>

<Dialog.Root bind:open={dialogOpen} onOpenChange={() => createError = ''}>
    <Dialog.Content>
        <Dialog.Header>
            <Dialog.Title>New Project</Dialog.Title>
        </Dialog.Header>

        <div class="flex flex-col gap-4 py-2">
            <div class="flex flex-col gap-1.5">
                <Label for="new-title">Title <span class="text-destructive">*</span></Label>
                <Input bind:value={newTitle} placeholder="Project title" id="new-title" />
            </div>
            <div class="flex flex-col gap-1.5">
                <Label for="new-description">Description</Label>
                <Textarea bind:value={newDescription} placeholder="Description" id="new-description" />
            </div>
            <div class="flex flex-col gap-1.5">
                <Label>Tags</Label>
                <AsyncMultiSelect bind:value={newTags} search={searchTags} placeholder="Add tags..." />
            </div>
            <div class="flex flex-col gap-1.5">
                <Label for="newCreators">Co-Creators</Label>
                <AsyncMultiSelect bind:value={newCreators} search={searchUsers} placeholder="Search by name..." />
            </div>

            {#if createError}
                <p class="text-sm text-destructive">{createError}</p>
            {/if}
        </div>

        <Dialog.Footer>
            <Button class="cursor-pointer" variant="outline" onclick={() => dialogOpen = false}>Cancel</Button>
            <Button class="cursor-pointer" onclick={createProject} disabled={!newTitle.trim() || submitting}>
                {submitting ? 'Creating...' : 'Create'}
            </Button>
        </Dialog.Footer>
    </Dialog.Content>
</Dialog.Root>