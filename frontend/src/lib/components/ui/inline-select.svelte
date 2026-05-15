<script lang="ts">
	import { debounce } from '$lib/utils/debounce';

	let {
		value = $bindable<string | null>(null),
		displayValue = $bindable<string | null>(null),
		search,
		oncreate,
		placeholder = 'Search or create...'
	}: {
		value: string | null;
		displayValue: string | null;
		search: (q: string) => Promise<{ id: string; name: string }[]>;
		oncreate?: (name: string) => Promise<{ id: string; name: string } | null>;
		placeholder?: string;
	} = $props();

	let searchQuery = $state(displayValue ?? '');
	let results = $state<{ id: string; name: string }[]>([]);
	let open = $state(false);
	let highlightedIndex = $state(-1);

	// Sync input when displayValue changes externally (e.g. AI fill)
	$effect(() => {
		if (!open) searchQuery = displayValue ?? '';
	});

	// Reset highlight when results change
	$effect(() => {
		results;
		highlightedIndex = -1;
	});

	const debouncedSearch = debounce((q: string) => {
		search(q).then((r) => {
			results = r;
		});
	}, 200);

	function onInput(q: string) {
		searchQuery = q;
		debouncedSearch(q);
	}

	function select(option: { id: string; name: string }) {
		value = option.id;
		displayValue = option.name;
		searchQuery = option.name;
		open = false;
		highlightedIndex = -1;
	}

	async function commitNew() {
		const name = searchQuery.trim();
		if (!name) return;
		if (oncreate) {
			const result = await oncreate(name);
			if (result) {
				value = result.id;
				displayValue = result.name;
				searchQuery = result.name;
			}
		} else {
			value = name;
			displayValue = name;
		}
		open = false;
		highlightedIndex = -1;
	}

	function onKeydown(e: KeyboardEvent) {
		if (e.key === 'ArrowDown') {
			e.preventDefault();
			open = true;
			highlightedIndex = Math.min(highlightedIndex + 1, results.length - 1);
		} else if (e.key === 'ArrowUp') {
			e.preventDefault();
			highlightedIndex = Math.max(highlightedIndex - 1, -1);
		} else if (e.key === 'Enter') {
			e.preventDefault();
			if (highlightedIndex >= 0 && highlightedIndex < results.length)
				select(results[highlightedIndex]);
			else if (results.length > 0) select(results[0]);
			else if (searchQuery.trim()) commitNew();
		} else if (e.key === 'Escape') {
			open = false;
			searchQuery = displayValue ?? '';
			highlightedIndex = -1;
		}
	}

	function onFocus() {
		search(searchQuery).then((r) => {
			results = r;
			open = true;
		});
	}

	function onBlur() {
		setTimeout(() => {
			open = false;
			highlightedIndex = -1;
			const q = searchQuery.trim();
			if (!q) {
				value = null;
				displayValue = null;
			} else {
				const exact = results.find((r) => r.name.toLowerCase() === q.toLowerCase());
				if (exact) {
					value = exact.id;
					displayValue = exact.name;
					searchQuery = exact.name;
				} else {
					value = q;
					displayValue = q;
				}
			}
		}, 150);
	}
</script>

<div class="relative border-b border-border/60 transition-colors focus-within:border-border">
	<input
		type="text"
		value={searchQuery}
		oninput={(e) => onInput(e.currentTarget.value)}
		onkeydown={onKeydown}
		onfocus={onFocus}
		onblur={onBlur}
		{placeholder}
		class="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground/50"
	/>
	{#if open && results.length > 0}
		<div
			class="absolute top-full right-0 left-0 z-10 mt-0.5 max-h-40 overflow-y-auto rounded-sm border border-border bg-popover shadow-md"
		>
			{#each results as option, idx (option.id)}
				<button
					onmousedown={() => select(option)}
					class="w-full cursor-pointer px-2 py-1.5 text-left text-xs {idx === highlightedIndex
						? 'bg-accent'
						: 'hover:bg-accent'}"
				>
					{option.name}
				</button>
			{/each}
			{#if oncreate && searchQuery.trim() && !results.some((r) => r.name.toLowerCase() === searchQuery
							.trim()
							.toLowerCase())}
				<button
					onmousedown={commitNew}
					class="w-full cursor-pointer px-2 py-1.5 text-left text-xs text-muted-foreground italic hover:bg-accent"
				>
					Create "{searchQuery.trim()}"
				</button>
			{/if}
		</div>
	{:else if open && oncreate && searchQuery.trim()}
		<div
			class="absolute top-full right-0 left-0 z-10 mt-0.5 rounded-sm border border-border bg-popover shadow-md"
		>
			<button
				onmousedown={commitNew}
				class="w-full cursor-pointer px-2 py-1.5 text-left text-xs text-muted-foreground italic hover:bg-accent"
			>
				Create "{searchQuery.trim()}"
			</button>
		</div>
	{/if}
</div>
