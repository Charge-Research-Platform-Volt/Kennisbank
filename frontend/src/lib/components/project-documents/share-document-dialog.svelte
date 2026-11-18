<script lang="ts">
	import { untrack } from 'svelte';
	import { Search, X } from '@lucide/svelte';
	import { toast } from 'svelte-sonner';
	import * as Dialog from '#lib/components/ui/dialog/index.js';
	import Avatar from '#lib/components/ui/avatar/avatar.svelte';
	import Button from '#lib/components/ui/button/button.svelte';
	import Spinner from '#lib/components/ui/spinner/spinner.svelte';
	import { api } from '#lib/api.js';
	import { debounce } from '#lib/utils/debounce.js';
	import {
		getVisibilityIcon,
		projectDocuments,
		visibilityDescriptions,
		visibilityLabels
	} from '#lib/project-documents.js';
	import type {
		DocumentAccessType,
		DocumentVisibility,
		ProjectDocumentDetail
	} from '#lib/types/project-document.js';

	let {
		open = $bindable(false),
		documentId,
		onsaved
	}: {
		open: boolean;
		documentId: string;
		onsaved?: () => void;
	} = $props();

	type Person = {
		userId: string;
		name: string;
		customAvatarVersion: number | null;
		accessType: DocumentAccessType;
	};
	type SearchResult = {
		id: string;
		firstName: string;
		lastName: string;
		customAvatarVersion: number | null;
	};

	const visibilityOptions: DocumentVisibility[] = ['Restricted', 'ProjectMembers', 'Everyone'];
	const selectClass =
		'flex h-9 cursor-pointer rounded-md border border-input bg-background px-3 py-1 text-sm shadow-xs outline-none focus-visible:border-ring focus-visible:ring-[3px] focus-visible:ring-ring/50';

	let detail = $state<ProjectDocumentDetail | null>(null);
	let people = $state<Person[]>([]);
	let visibility = $state<DocumentVisibility>('ProjectMembers');
	let generalAccessType = $state<DocumentAccessType>('Editor');
	let query = $state('');
	let results = $state<SearchResult[]>([]);
	let saving = $state(false);

	// The uploader of version 1 created the document: always an editor, never a grant
	const uploader = $derived(
		detail?.versions.find((v) => v.versionNumber === 1)?.uploadedBy ?? null
	);
	const VisibilityIcon = $derived(getVisibilityIcon(visibility));

	$effect(() => {
		if (open) untrack(load);
	});

	async function load() {
		detail = null;
		query = '';
		results = [];

		try {
			const d = await projectDocuments.get(documentId);
			detail = d;
			visibility = d.visibility;
			generalAccessType = d.generalAccessType;
			people = d.grants.map((g) => ({
				userId: g.userId,
				name: `${g.firstName} ${g.lastName}`,
				customAvatarVersion: g.customAvatarVersion,
				accessType: g.accessType
			}));
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to load sharing settings.');
			open = false;
		}
	}

	const search = debounce(async (q: string) => {
		if (!q.trim()) {
			results = [];
			return;
		}

		try {
			const result = await api.get<{ users: SearchResult[] }>(
				`/api/users?page=1&pageSize=8&search=${encodeURIComponent(q)}`
			);

			// A slower response for an older query must not replace the current one
			if (q !== query) return;

			const taken = new Set([detail?.createdBy, ...people.map((p) => p.userId)]);
			results = result.users.filter((u) => !taken.has(u.id));
		} catch {
			toast.error('Failed to search users.');
		}
	});

	function add(user: SearchResult) {
		people.push({
			userId: user.id,
			name: `${user.firstName} ${user.lastName}`,
			customAvatarVersion: user.customAvatarVersion,
			accessType: 'Viewer'
		});
		query = '';
		results = [];
	}

	function remove(userId: string) {
		people = people.filter((p) => p.userId !== userId);
	}

	async function save() {
		saving = true;

		try {
			await projectDocuments.setAccess(documentId, {
				visibility,
				generalAccessType,
				grants: people.map((p) => ({ userId: p.userId, accessType: p.accessType }))
			});

			open = false;
			toast.success('Sharing updated.');
			onsaved?.();
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to update sharing.');
		} finally {
			saving = false;
		}
	}
</script>

<Dialog.Root bind:open>
	<Dialog.Content class="sm:max-w-lg">
		<Dialog.Header>
			<Dialog.Title>Share "{detail?.title ?? ''}"</Dialog.Title>
		</Dialog.Header>

		{#if !detail}
			<div class="flex justify-center py-8">
				<Spinner class="h-6 w-6" />
			</div>
		{:else}
			<div class="flex flex-col gap-5 py-2">
				<!-- Add people -->
				<div class="relative">
					<div
						class="flex items-center gap-2 rounded-md border border-input px-3 focus-within:border-ring focus-within:ring-2 focus-within:ring-ring/50"
					>
						<Search size={14} class="shrink-0 text-muted-foreground" />
						<input
							class="flex-1 bg-transparent py-1.5 text-sm outline-none placeholder:text-muted-foreground"
							placeholder="Add people..."
							bind:value={query}
							oninput={() => search(query)}
						/>
					</div>

					{#if results.length > 0}
						<ul
							class="absolute z-10 mt-1 w-full rounded-md border border-border bg-popover p-1 shadow-md"
						>
							{#each results as user (user.id)}
								<li>
									<button
										class="flex w-full cursor-pointer items-center gap-2 rounded px-2 py-1.5 text-left text-sm hover:bg-accent"
										onclick={() => add(user)}
									>
										<Avatar
											userId={user.id}
											name="{user.firstName} {user.lastName}"
											customAvatarVersion={user.customAvatarVersion}
											size={22}
										/>
										{user.firstName}
										{user.lastName}
									</button>
								</li>
							{/each}
						</ul>
					{/if}
				</div>

				<!-- People with access -->
				<section class="flex flex-col gap-2">
					<h3 class="text-sm font-medium">People with access</h3>
					<ul class="flex flex-col gap-1">
						{#if uploader}
							<li class="flex items-center gap-2 py-1 text-sm">
								<Avatar
									userId={uploader.id ?? ''}
									name={uploader.name}
									customAvatarVersion={uploader.customAvatarVersion}
									size={28}
								/>
								<span class="truncate">{uploader.name}</span>
								<span class="text-xs text-muted-foreground">Creator</span>
								<span class="ml-auto text-xs text-muted-foreground">Editor</span>
							</li>
						{/if}

						{#each people as person (person.userId)}
							<li class="flex items-center gap-2 py-1 text-sm">
								<Avatar
									userId={person.userId}
									name={person.name}
									customAvatarVersion={person.customAvatarVersion}
									size={28}
								/>
								<span class="truncate">{person.name}</span>
								<select bind:value={person.accessType} class="{selectClass} ml-auto w-28">
									<option value="Viewer">Viewer</option>
									<option value="Editor">Editor</option>
								</select>
								<button
									class="cursor-pointer text-muted-foreground hover:text-foreground"
									aria-label="Remove {person.name}"
									onclick={() => remove(person.userId)}
								>
									<X size={14} />
								</button>
							</li>
						{/each}
					</ul>
				</section>

				<!-- General access -->
				<section class="flex flex-col gap-2">
					<h3 class="text-sm font-medium">General access</h3>
					<div class="flex items-center gap-2">
						<VisibilityIcon size={16} class="shrink-0 text-muted-foreground" />
						<select bind:value={visibility} class="{selectClass} flex-1">
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
					<p class="text-xs text-muted-foreground">{visibilityDescriptions[visibility]}</p>
				</section>
			</div>
		{/if}

		<Dialog.Footer>
			<Button variant="outline" class="cursor-pointer" onclick={() => (open = false)}>
				Cancel
			</Button>
			<Button class="cursor-pointer" disabled={!detail || saving} onclick={save}>Save</Button>
		</Dialog.Footer>
	</Dialog.Content>
</Dialog.Root>
