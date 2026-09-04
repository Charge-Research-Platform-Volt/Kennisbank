<script lang="ts">
	import type { ResourceItem, NavigationTarget, RawResourceItem } from '$lib/types/resource';
	import {
		ArrowLeft,
		ArrowRight,
		Download,
		X,
		Pencil,
		PencilOff,
		Trash2,
		Ellipsis,
		Share2,
		BookmarkPlus,
		LoaderCircle,
		CircleAlert
	} from '@lucide/svelte';
	import { getFileAction, getFileIcon } from '$lib/utils/icons';
	import { untrack, setContext, getContext } from 'svelte';
	import { formatDate } from '$lib/utils/date';
	import ResourceInspector from './resource-inspector.svelte';
	import PersonInspector from './person-inspector.svelte';
	import OrganistationInspector from './organistation-inspector.svelte';
	import TooltipProvider from '../ui/tooltip/tooltip-provider.svelte';
	import { api } from '$lib/api';
	import { openFile } from '$lib/utils/openFile';
	import { setParams } from '$lib/utils/urlState';
	import EditableDate from './editable-date.svelte';
	import { userState } from '$lib/state/user.svelte';
	import * as DropdownMenu from '../ui/dropdown-menu';
	import { toast } from 'svelte-sonner';
	import AddToProjectDialog from '../ui/add-to-project-dialog.svelte';

	let {
		item = $bindable<ResourceItem | null>(null),
		onaftersave
	}: { item: ResourceItem | null; onaftersave?: () => void } = $props();

	let history = $state<ResourceItem[]>([]);
	let historyIndex = $state(-1);
	let editMode = $state(false);
	let contentKey = $state(0);
	setContext('getEditMode', () => editMode);

	let pendingSaves: Promise<void>[] = [];
	let dirty = $state(false);
	setContext('registerSave', (p: Promise<void>) => {
		dirty = true;
		pendingSaves.push(p.catch(() => {}));
	});

	let canGoBack = $derived(historyIndex > 0);
	let canGoForward = $derived(historyIndex < history.length - 1);

	let Icon = $derived(getFileIcon(item?.fileType ?? item?.type ?? ''));

	const action = $derived(getFileAction(item?.fileType ?? ''));

	const closeInspector: () => void = getContext('closeInspector');

	let nameInput = $state('');
	let addToProjectOpen = $state(false);

	// Called when item is set from outside (e.g., row click)
	$effect(() => {
		const current = item;
		if (!current) return;

		untrack(() => {
			if (current.id === history[historyIndex]?.id) return;

			history = [...history.slice(0, historyIndex + 1), current];
			historyIndex = history.length - 1;
			editMode = false;
			nameInput = current.name;
		});
	});

	setContext('navigate', navigate);

	export async function navigate(target: NavigationTarget) {
		const endpoint =
			target.type === 'resource'
				? 'resources'
				: target.type === 'person'
					? 'persons'
					: 'organisations';

		const result = await api.get<Partial<RawResourceItem>>(`/api/${endpoint}/${target.id}`);
		const fullItem: ResourceItem = {
			fileType: target.type,
			publicationDate: '',
			publicationDatePrecision: 'Day',
			createdOn: '',
			description: '',
			...target,
			...result,
			name: result.title ?? result.name ?? '',
			embeddingStatus: result.embeddingStatus ?? 'Completed'
		};

		history = [...history.slice(0, historyIndex + 1), fullItem];
		historyIndex = history.length - 1;
		item = fullItem;
		nameInput = fullItem.name;
		editMode = false;
	}

	function goBack() {
		if (!canGoBack) return;

		item = history[--historyIndex];
		nameInput = item.name;
		setParams({ inspectorId: item.id, inspectorType: item.type });
		editMode = false;
	}

	function goForward() {
		if (!canGoForward) return;

		item = history[++historyIndex];
		nameInput = item.name;
		setParams({ inspectorId: item.id, inspectorType: item.type });
		editMode = false;
	}

	function share() {
		navigator.clipboard.writeText(
			`${location.origin}/library?inspectorId=${item?.id}&inspectorType=${item?.type}`
		);
		toast.success('Link copied to clipboard!');
	}

	async function trashItem() {
		if (!item) return;
		const trashed = item;

		try {
			await api.patch(`/api/${item.type}s/${item.id}/trash`);
			closeInspector();
			onaftersave?.();

			toast.success('Moved to trash', {
				action: {
					label: 'Undo',
					onClick: async () => {
						try {
							await api.patch(`/api/${trashed.type}s/${trashed.id}/untrash`);
							toast.success('Restored');
							onaftersave?.();
						} catch {
							toast.error('Could not restore');
						}
					}
				}
			});
		} catch {
			toast.error('Something went wrong.');
		}
	}

	async function close() {
		if (editMode && dirty) {
			await Promise.all(pendingSaves);
			onaftersave?.();
		}
		closeInspector();
		editMode = false;
		dirty = false;
		pendingSaves = [];
	}
</script>

<aside class="flex h-full flex-col overflow-hidden border-l border-border bg-background">
	<TooltipProvider>
		{#if item}
			<!-- Toolbar row -->
			<div class="flex shrink-0 items-center justify-between border-b px-3 py-2">
				<div class="flex items-center gap-1">
					<!-- Navigation buttons -->
					<button
						onclick={goBack}
						disabled={!canGoBack}
						class="cursor-pointer p-1 text-muted-foreground transition-colors hover:text-foreground disabled:cursor-not-allowed disabled:opacity-30"
					>
						<ArrowLeft size={16} />
					</button>
					<button
						onclick={goForward}
						disabled={!canGoForward}
						class="cursor-pointer p-1 text-muted-foreground transition-colors hover:text-foreground disabled:cursor-not-allowed disabled:opacity-30"
					>
						<ArrowRight size={16} />
					</button>
				</div>

				<!-- Download, edit, close buttons -->
				<div class="flex items-center gap-1">
					{#if action && item.fileType !== 'website'}
						<button
							onclick={() => item && openFile(item.id, item.fileType, item.sourceUrl)}
							class="cursor-pointer p-1 text-muted-foreground transition-colors hover:text-foreground"
						>
							<Download size={16} />
						</button>
					{/if}

					<button
						onclick={async () => {
							if (editMode) {
								await Promise.all(pendingSaves);
								pendingSaves = [];
								if (dirty) {
									contentKey++;
									onaftersave?.();
									dirty = false;
								}
							}
							editMode = !editMode;
						}}
						class="cursor-pointer p-1 text-muted-foreground transition-colors hover:text-foreground"
					>
						{#if editMode}
							<PencilOff size={16} />
						{:else}
							<Pencil size={16} />
						{/if}
					</button>

					<DropdownMenu.Root>
						<DropdownMenu.Trigger
							class="cursor-pointer p-1 text-muted-foreground transition-colors hover:text-foreground"
						>
							<Ellipsis size={16} />
						</DropdownMenu.Trigger>

						<DropdownMenu.Content align="end">
							<DropdownMenu.Item class="cursor-pointer" onclick={share}>
								<Share2 size={14} class="mr-2" /> Copy link
							</DropdownMenu.Item>

							<DropdownMenu.Item class="cursor-pointer" onclick={() => (addToProjectOpen = true)}>
								<BookmarkPlus size={14} class="mr-2" /> Add to project
							</DropdownMenu.Item>

							{#if userState.role === 'admin' && !item?.trashed}
								<DropdownMenu.Separator />

								<DropdownMenu.Item
									class="cursor-pointer text-destructive focus:text-destructive"
									onclick={trashItem}
								>
									<Trash2 size={14} class="mr-2" /> Move to trash
								</DropdownMenu.Item>
							{/if}
						</DropdownMenu.Content>
					</DropdownMenu.Root>

					<button
						onclick={close}
						class="cursor-pointer p-1 text-muted-foreground transition-colors hover:text-foreground"
					>
						<X size={16} />
					</button>
				</div>
			</div>

			<!-- Scrollable body -->
			<div class="flex flex-1 flex-col overflow-y-auto">
				<!-- Title row -->
				<div class="flex items-start gap-2 border-b px-3 py-3">
					<Icon size={16} class="mt-0.75 shrink-0 text-muted-foreground" />
					{#if editMode}
						<input
							bind:value={nameInput}
							onblur={() => {
								if (!item) return;
								const endpoint =
									item.type === 'resource'
										? 'resources'
										: item.type === 'person'
											? 'persons'
											: 'organisations';
								const field = item.type === 'resource' ? 'title' : 'name';
								item.name = nameInput;
								dirty = true;
								pendingSaves.push(
									api
										.patch(`/api/${endpoint}/${item.id}`, { [field]: nameInput })
										.then(() => {})
										.catch(() => {})
								);
							}}
							class="min-w-0 flex-1 border-b border-input bg-transparent py-0.5 text-sm font-medium focus:border-ring focus:outline-none"
						/>
					{:else}
						<span class="line-clamp-2 text-sm font-medium" title={item.name}>{item.name}</span>
					{/if}
				</div>

				<!-- Trashed banner -->
				{#if item.trashed}
					<div
						class="flex items-center gap-2 border-b border-destructive/20 bg-destructive/10 px-3 py-2 text-xs text-destructive"
					>
						<Trash2 size={12} class="shrink-0" />
						This item is in the trash.
					</div>
				{/if}

				<!-- Embedding status banner -->
				{#if item.embeddingStatus === 'Pending' || item.embeddingStatus === 'Processing'}
					<div
						class="flex items-center gap-2 border-b bg-muted/50 px-3 py-2 text-xs text-muted-foreground"
					>
						<LoaderCircle size={12} class="shrink-0 animate-spin" />
						Still processing embeddings…
					</div>
				{:else if item.embeddingStatus === 'Failed'}
					<div
						class="flex items-center gap-2 border-b border-destructive/20 bg-destructive/10 px-3 py-2 text-xs text-destructive"
					>
						<CircleAlert size={12} class="shrink-0" />
						Embedding failed.
					</div>
				{/if}

				<!-- Content -->
				<div class="flex-1">
					{#key contentKey}
						{#if item.type === 'resource'}
							<ResourceInspector {item} />
						{:else if item.type === 'person'}
							<PersonInspector {item} />
						{:else if item.type === 'organisation'}
							<OrganistationInspector {item} />
						{/if}
					{/key}
				</div>

				<!-- Footer -->
				<div
					class="flex shrink-0 px-3 py-2 text-xs text-muted-foreground {editMode
						? ''
						: 'flex-row justify-center gap-4'}"
				>
					{#if item.type === 'resource'}
						<EditableDate
							date={item.publicationDate}
							precision={item.publicationDatePrecision}
							onsave={async (d, p) => {
								await api.patch(`/api/resources/${item!.id}`, {
									publicationDate: d,
									publicationDatePrecision: p
								});
								item!.publicationDate = d ?? '';
								item!.publicationDatePrecision = p;
								dirty = true;
							}}
						/>
					{/if}
					{#if !editMode}
						<span>Added: {formatDate(item.createdOn) ?? '-'}</span>
					{/if}
				</div>
			</div>
		{/if}

		<AddToProjectDialog bind:open={addToProjectOpen} {item} />
	</TooltipProvider>
</aside>
