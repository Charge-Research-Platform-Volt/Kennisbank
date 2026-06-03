<script lang="ts">
	import { api } from '$lib/api';
	import type { ResourceItem, TrashItem } from '$lib/types/resource';
	import { userState } from '$lib/state/user.svelte';
	import Spinner from '$lib/components/ui/spinner/spinner.svelte';
	import { RotateCcw, ArrowLeft } from '@lucide/svelte';
	import * as Table from '$lib/components/ui/table';
	import { getFileIcon } from '$lib/utils/icons';
	import { formatDate } from '$lib/utils/date';
	import { getContext } from 'svelte';
	import { toast } from 'svelte-sonner';

	let items = $state<TrashItem[]>([]);
	let loading = $state(false);

	const openInspector: (item: ResourceItem) => void = getContext('openInspector');
	const closeInspector: () => void = getContext('closeInspector');

	async function fetchItems() {
		loading = true;

		try {
			const result = await api.get<TrashItem[]>('/api/library/trash');
			items = result;
		} finally {
			loading = false;
		}
	}

	async function restore(item: TrashItem) {
		try {
			await api.patch(`/api/${item.type}s/${encodeURIComponent(item.id)}/untrash`);
			items = items.filter((i) => i.id !== item.id);
			closeInspector();
			toast.success('Successfully restored item.');
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to restore item.');
		}
	}

	// Fetch items on page load
	fetchItems();
</script>

<div class="flex h-full flex-1 flex-col gap-4 overflow-hidden p-4">
	<!-- Header -->
	<div class="flex items-center gap-3">
		<a href="/library" class="text-muted-foreground transition-colors hover:text-foreground">
			<ArrowLeft size={16} />
		</a>
		<h1 class="text-lg font-semibold">Trash</h1>
	</div>

	{#if userState.loading}
		<div class="flex h-full w-full items-center justify-center">
			<Spinner size={32} />
		</div>
	{:else if userState.role !== 'admin'}
		<div class="flex h-full flex-col items-center justify-center gap-2 text-muted-foreground">
			<p class="text-sm">You don't have permission to view this page.</p>
		</div>
	{:else if loading}
		<div class="flex h-full w-full items-center justify-center">
			<Spinner size={32} />
		</div>
	{:else if items.length === 0}
		<div class="flex h-full w-full items-center justify-center text-muted-foreground">
			<p class="text-sm">Trash is empty.</p>
		</div>
	{:else}
		<Table.Root>
			<Table.Header>
				<tr class="border-b">
					<Table.Head class="w-full">Name</Table.Head>
					<Table.Head class="w-px text-center whitespace-nowrap">Trashed on</Table.Head>
					<Table.Head class="w-px"></Table.Head>
				</tr>
			</Table.Header>
			<Table.Body>
				{#each items as item (item.id)}
					{@const Icon = getFileIcon(item.fileType)}
					<Table.Row
						class="cursor-pointer"
						onclick={() => openInspector({ ...item, trashed: true })}
					>
						<Table.Cell class="py-3">
							<div class="flex items-center gap-3">
								<Icon size={16} class="shrink-0 text-muted-foreground" />
								{item.name}
							</div>
						</Table.Cell>
						<Table.Cell class="px-5 py-3 text-center whitespace-nowrap"
							>{formatDate(item.trashDate)}</Table.Cell
						>
						<Table.Cell class="p-3">
							<div class="flex items-center justify-center">
								<button
									class="cursor-pointer text-muted-foreground transition-colors hover:text-foreground"
									title="Restore"
									onclick={(e) => {
										e.stopPropagation();
										restore(item);
									}}
								>
									<RotateCcw size={16} />
								</button>
							</div>
						</Table.Cell>
					</Table.Row>
				{/each}
			</Table.Body>
		</Table.Root>
	{/if}
</div>
