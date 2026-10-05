<script lang="ts">
	import * as Pagination from '$lib/components/ui/pagination';
	import { debounce } from '$lib/utils/debounce';
	import type { PageItem } from 'bits-ui';
	import { getParam, getParamInt, setParams } from '$lib/utils/urlState';
	import { onMount } from 'svelte';
	import { afterNavigate, goto } from '$app/navigation';
	import type { Project, ProjectListResponse } from '$lib/types/project';
	import { api } from '$lib/api';
	import { Search, X, BookMarked, Plus } from '@lucide/svelte';
	import { userState } from '$lib/state/user.svelte';
	import * as Table from '$lib/components/ui/table';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import { formatDate } from '$lib/utils/date';
	import Button from '$lib/components/ui/button/button.svelte';
	import * as Dialog from '$lib/components/ui/dialog';
	import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';
	import Textarea from '$lib/components/ui/textarea/textarea.svelte';
	import Input from '$lib/components/ui/input/input.svelte';
	import Label from '$lib/components/ui/label/label.svelte';
	import Avatar from '$lib/components/ui/avatar/avatar.svelte';
	import { toast } from 'svelte-sonner';
	import type { ListItem, PagedResult } from '$lib/types/results';

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
	let newMembers = $state<string[]>([]);
	let submitting = $state(false);

	const debouncedFetchProjects = debounce(fetchProjects);

	async function fetchProjects() {
		loading = true;

		try {
			const result = await api.post<ProjectListResponse>('/api/projects', {
				page: currentPage,
				pageSize: PAGE_SIZE,
				search: searchInput || undefined
			});

			items = result.items;
			totalItems = result.totalCount ?? 0;
		} finally {
			loading = false;
		}
	}

	async function searchTags(q: string) {
		const result = await api.get<PagedResult<ListItem>>(
			`/api/tags?search=${encodeURIComponent(q)}`
		);
		return result.items;
	}

	async function searchUsers(q: string) {
		const result = await api.get<{ users: { id: string; firstName: string; lastName: string }[] }>(
			`/api/users?search=${encodeURIComponent(q)}&excludeId=${userState.user?.id ?? ''}`
		);

		return result.users.map((u) => ({
			id: u.id.toString(),
			name: `${u.firstName} ${u.lastName}`
		}));
	}

	async function createProject() {
		if (!newTitle.trim()) return;

		submitting = true;

		try {
			await api.put('/api/projects', {
				title: newTitle.trim(),
				description: newDescription.trim() || null,
				projectType: 'root',
				tags: newTags,
				members: newMembers
			});

			dialogOpen = false;
			newTitle = '';
			newDescription = '';
			newTags = [];
			newMembers = [];
			await fetchProjects();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to create project.');
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

<div class="flex h-full flex-1 flex-col gap-4 overflow-hidden p-4">
	<!-- Search bar -->
	<div class="flex items-center gap-2">
		<div
			class="flex flex-1 items-center gap-2 rounded-md border border-input bg-background px-3 focus-within:border-ring focus-within:ring-2 focus-within:ring-ring/50"
		>
			<Search size={16} class="shrink-0 text-muted-foreground" />
			<input
				class="flex-1 bg-transparent py-1.5 text-sm outline-none placeholder:text-muted-foreground"
				value={searchInput}
				oninput={(e) => onSearchInput(e.currentTarget.value)}
				placeholder="Search projects..."
			/>
			{#if searchInput}
				<X
					size={16}
					class="shrink-0 cursor-pointer text-zinc-600"
					onclick={() => onSearchInput('')}
				/>
			{/if}
		</div>

		<Button class="flex cursor-pointer items-center" onclick={() => (dialogOpen = true)}>
			<Plus size={14} class="shrink-0" />
			Create Project
		</Button>
	</div>

	<!-- List -->
	{#if loading}
		<div class="flex h-full w-full items-center justify-center">
			<Spinner class="h-10 w-10" />
		</div>
	{:else}
		<Table.Root>
			<Table.Caption>
				<Pagination.Root
					count={totalItems}
					perPage={PAGE_SIZE}
					bind:page={currentPage}
					onPageChange={handlePageChange}
				>
					{#snippet children({ pages, currentPage }: { pages: PageItem[]; currentPage: number })}
						<Pagination.Content>
							<Pagination.Item><Pagination.Previous class="cursor-pointer" /></Pagination.Item>
							{#each pages as page (page.key)}
								{#if page.type === 'ellipsis'}
									<Pagination.Item><Pagination.Ellipsis /></Pagination.Item>
								{:else}
									<Pagination.Item>
										<Pagination.Link
											class="cursor-pointer"
											{page}
											isActive={currentPage === page.value}>{page.value}</Pagination.Link
										>
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
					<Table.Head class="w-px whitespace-nowrap">Members</Table.Head>
					<Table.Head class="w-px text-center whitespace-nowrap">Created</Table.Head>
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
							<div class="flex items-center gap-3">
								<BookMarked size={16} class="shrink-0 text-muted-foreground" />
								<div class="flex flex-col">
									<span>{item.title}</span>
									{#if item.description}
										<span class="max-w-sm truncate text-xs text-muted-foreground"
											>{item.description}</span
										>
									{/if}
								</div>
							</div>
						</Table.Cell>

						<Table.Cell class="py-3">
							<div class="flex items-center">
								{#each item.members ?? [] as member (member.id)}
									<Avatar
										userId={member.id.toString()}
										name="{member.firstName} {member.lastName}"
										customAvatarVersion={member.customAvatarVersion ?? null}
										size={24}
										class="-ml-2 first:ml-0"
									/>
								{/each}
							</div>
						</Table.Cell>

						<Table.Cell class="px-5 py-3 text-center whitespace-nowrap"
							>{formatDate(item.createdOn)}</Table.Cell
						>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	{/if}
</div>

<Dialog.Root bind:open={dialogOpen}>
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
				<Label for="newmembers">Co-members</Label>
				<AsyncMultiSelect
					bind:value={newMembers}
					search={searchUsers}
					placeholder="Search by name..."
				/>
			</div>
		</div>

		<Dialog.Footer>
			<Button class="cursor-pointer" variant="outline" onclick={() => (dialogOpen = false)}
				>Cancel</Button
			>
			<Button
				class="cursor-pointer"
				onclick={createProject}
				disabled={!newTitle.trim() || submitting}
			>
				{submitting ? 'Creating...' : 'Create'}
			</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
