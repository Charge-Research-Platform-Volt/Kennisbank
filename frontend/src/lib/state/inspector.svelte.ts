import { afterNavigate } from '$app/navigation';
import { getParam, setParams } from '$lib/utils/urlState';
import { onMount, setContext } from 'svelte';
import type { ResourceItem, EntityType } from '$lib/types/resource';
import type Inspector from '$lib/components/inspector/inspector.svelte';

export function createInspectorState() {
	let selectedItem = $state<ResourceItem | null>(null);
	let inspectorRef: ReturnType<typeof Inspector> | undefined = $state();
	let refreshFn: (() => void) | null = null;

	function openInspector(item: ResourceItem) {
		selectedItem = item;
		setParams({ inspectorId: item.id, inspectorType: item.type });
	}

	function closeInspector() {
		selectedItem = null;
		setParams({ inspectorId: null, inspectorType: null });
	}

	setContext('openInspector', openInspector);
	setContext('closeInspector', closeInspector);
	setContext('registerRefresh', (fn: () => void) => (refreshFn = fn));

	onMount(() => {
		const id = getParam('inspectorId');
		const rawType = getParam('inspectorType');
		const urlType = ['resource', 'person', 'organisation'].includes(rawType)
			? (rawType as EntityType)
			: null;

		if (id && urlType) inspectorRef?.navigate({ id, type: urlType, name: '' });
		else selectedItem = null;
	});

	afterNavigate(() => {
		if (!getParam('inspectorId')) closeInspector();
	});

	return {
		get selectedItem() {
			return selectedItem;
		},
		set selectedItem(v) {
			selectedItem = v;
		},
		get inspectorRef() {
			return inspectorRef;
		},
		set inspectorRef(v) {
			inspectorRef = v;
		},
		openInspector,
		closeInspector,
		onAfterSave: () => refreshFn?.()
	};
}
