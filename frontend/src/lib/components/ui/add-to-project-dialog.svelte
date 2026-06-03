<script lang="ts">
	import { api } from '$lib/api';
	import type { ProjectListResponse, Project } from '$lib/types/project';
	import type { ResourceItem } from '$lib/types/resource';
	import { debounce } from '$lib/utils/debounce';
	import { toast } from 'svelte-sonner';
	import * as Command from '$lib/components/ui/command';
	import { Folder, BookMarked, ArrowLeft } from '@lucide/svelte';

	let { open = $bindable(false), item }: { open: boolean; item: ResourceItem | null } = $props();

	let step = $state<'project' | 'folder'>('project');

	// Step 1 - Project Selection
	let projectSearch = $state('');
	let projectResults = $state<Project[]>([]);
	let projectLoading = $state(false);
	const onProjectSearchInput = debounce(searchProjects);

	// Step 2 - Folder selection
	type FolderEntry = { id: string; title: string; depth: number };
	let selectedProject = $state<{ id: string; title: string } | null>(null);
	let folders = $state<FolderEntry[]>([]);
	let folderSearch = $state('');
	let folderLoading = $state(false);
	let filteredFolders = $derived(
		folders.filter((f) => f.title.toLowerCase().includes(folderSearch.toLowerCase()))
	);

	async function searchProjects() {
		projectLoading = true;

		try {
			const result = await api.post<ProjectListResponse>('/api/projects', {
				page: 1,
				pageSize: 20,
				searchQuery: projectSearch || undefined
			});

			projectResults = result.items;
		} finally {
			projectLoading = false;
		}
	}

	async function selectProject(project: Project) {
		selectedProject = { id: project.id, title: project.title };
		folderLoading = true;
		step = 'folder';

		try {
			const result = await api.get<FolderEntry[]>(`/api/projects/${project.id}/folders`);
			folders = result;
		} finally {
			folderLoading = false;
		}
	}

	async function addToLocation(locationId: string, locationTitle: string) {
		if (!item) return;

		try {
			await api.post(`/api/projects/${locationId}/items/${item.id}`, {});
			toast.success(`Added to ${locationTitle}`);
		} catch (e) {
			if (e instanceof Error && e.message.includes('already')) {
				toast.info(`This item already exists in ${locationTitle}`);
			} else {
				toast.error('Something went wrong');
			}
		} finally {
			open = false;
		}
	}

	$effect(() => {
		if (open) {
			step = 'project';
			projectSearch = '';
			projectResults = [];
			folderSearch = '';
			selectedProject = null;
			searchProjects();
		}
	});
</script>

<Command.Dialog bind:open shouldFilter={false}>
	{#if step === 'project'}
		<Command.Input
			placeholder="Search projects..."
			bind:value={projectSearch}
			oninput={onProjectSearchInput}
		/>

		<Command.List>
			{#if projectLoading}
				<Command.Loading class="py-5 text-center">Searching...</Command.Loading>
			{:else if projectResults.length === 0}
				<Command.Empty>No projects found.</Command.Empty>
			{:else}
				<Command.Group heading="Projects">
					{#each projectResults as project (project.id)}
						<Command.Item class="cursor-pointer gap-2" onSelect={() => selectProject(project)}>
							<BookMarked size={14} class="shrink-0 text-muted-foreground" />
							{project.title}
						</Command.Item>
					{/each}
				</Command.Group>
			{/if}
		</Command.List>
	{:else}
		<div class="flex items-center gap-2 border-b px-3 py-2 text-sm text-muted-foreground">
			<button onclick={() => (step = 'project')} class="cursor-pointer hover:text-foreground">
				<ArrowLeft size={14} />
			</button>
			{selectedProject?.title}
		</div>

		<Command.Input placeholder="Search folders..." bind:value={folderSearch} class="flex-1" />

		<Command.List>
			{#if folderLoading}
				<Command.Loading class="py-5 text-center">Loading...</Command.Loading>
			{:else}
				<Command.Item
					class="mt-2 cursor-pointer gap-2"
					onSelect={() => addToLocation(selectedProject!.id, selectedProject!.title)}
				>
					<BookMarked size={14} class="shrink-0 text-muted-foreground" />
					Project root
				</Command.Item>

				{#each filteredFolders as folder (folder.id)}
					<Command.Item
						class="cursor-pointer gap-2"
						onSelect={() => addToLocation(folder.id, folder.title)}
					>
						<span style="width: {(folder.depth - 1) * 16}px" class="shrink-0"></span>
						<Folder size={14} class="shrink-0 text-muted-foreground" />
						{folder.title}
					</Command.Item>
				{/each}
			{/if}
		</Command.List>
	{/if}
</Command.Dialog>
