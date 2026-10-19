import { afterNavigate } from "$app/navigation";
import { api } from "$lib/api";
import { getParam, setParams } from '$lib/utils/urlState';
import { onMount, setContext } from "svelte";
import type {
	ResourceItem,
	EntityType,
	NavigationTarget,
	RawResourceItem
} from '$lib/types/resource';

export async function fetchInspectorItem(target: NavigationTarget): Promise<ResourceItem> {
	const endpoint = target.type === 'resource' ? 'resources' : target.type === 'person' ? 'persons' : 'organisations';

	const result = await api.get<Partial<RawResourceItem>>(`/api/${endpoint}/${target.id}`);

	return {
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
}

export function createInspectorState() {
	let selectedItem = $state<ResourceItem | null>(null);
	let refreshFn: (() => void) | null = null;

	function openInspector(item: ResourceItem) {
		selectedItem = item;
		setParams({ inspectorId: item.id, inspectorTye: item.type });
	}

	function closeInspector() {
		selectedItem = null;
		setParams({ inspectorId: null, inspectorType: null });
	}

	setContext('openInspector', openInspector);
	setContext('closeInspector', closeInspector);
	setContext('registerRefresh', (fn: () => void) => (refreshFn = fn));

	// Runs on mount and on every navigation, so ?inspectorId links work both
	// on a fresh load and when navigating within the same layout
	function syncFromUrl() {
		const id = getParam('inspectorId');
		const rawType = getParam('inspectorType');
		const urlType = ['resource', 'person', 'organisation'].includes(rawType)
			? (rawType as EntityType)
			: null;

		if (!id || !urlType) return closeInspector();
		if (id === selectedItem?.id) return;

		fetchInspectorItem({ id, type: urlType, name: '' })
			.then((item) => (selectedItem = item))
			.catch(() => closeInspector());
	}

	onMount(() => syncFromUrl());
	afterNavigate(() => syncFromUrl());

	return {
		get selectedItem() {
			return selectedItem;
		},
		set selectedItem(v) {
			selectedItem = v;
		},
		openInspector,
		closeInspector,
		onAfterSave: () => refreshFn?.()
	};
}