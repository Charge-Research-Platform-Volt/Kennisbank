<script lang="ts">
	import { Popover } from 'bits-ui';
	import { debounce } from '$lib/utils/debounce';
	import { Check, ChevronDown, Search } from '@lucide/svelte';

	let {
		value = $bindable<string[]>([]),
		search,
		placeholder = 'Select...',
		class: className = '',
		onchange
	}: {
		value: string[];
		search: (q: string) => Promise<{ id: string; name: string }[]>;
		placeholder?: string;
		class?: string;
		onchange?: () => void;
	} = $props();

	let open = $state(false);
	let options = $state<{ id: string; name: string }[]>([]);
	let searchQuery = $state('');
	async function doSearch(q: string) {
		options = await search(q);
	}

	const debouncedSearch = debounce((q: string) => doSearch(q));

	function onSearchInput(q: string) {
		searchQuery = q;
		debouncedSearch(q);
	}

	function toggle(id: string) {
		value = value.includes(id) ? value.filter((v) => v !== id) : [...value, id];
		onchange?.();
	}

	$effect(() => {
		if (open) {
			searchQuery = '';
			doSearch('');
		}
	});
</script>

<Popover.Root bind:open>
	<Popover.Trigger
		class="flex h-9 cursor-pointer items-center gap-1.5 rounded-md border border-input bg-background px-3 text-sm transition-colors hover:bg-accent {className}"
	>
		<span class="min-w-0 flex-1 truncate text-left"
			>{placeholder}{value.length > 0 ? ` · ${value.length}` : ''}</span
		>
		<ChevronDown size={12} class="shrink-0 text-muted-foreground" />
	</Popover.Trigger>
	<Popover.Portal>
		<Popover.Content
			sideOffset={4}
			class="z-50 w-52 rounded-md border border-border bg-popover p-1 shadow-md outline-none"
		>
			<div
				class="mx-1 mb-1 flex items-center gap-1.5 rounded-sm border border-input px-2 focus-within:ring-1 focus-within:ring-ring"
			>
				<Search size={12} class="shrink-0 text-muted-foreground" />
				<input
					class="flex-1 bg-transparent py-1 text-xs outline-none placeholder:text-muted-foreground"
					value={searchQuery}
					oninput={(e) => onSearchInput(e.currentTarget.value)}
					placeholder="Search..."
				/>
			</div>
			<div class="max-h-48 overflow-y-auto">
				{#each options as option (option.id)}
					<button
						class="flex w-full cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-left text-xs hover:bg-accent"
						onclick={() => toggle(option.id)}
					>
						<div
							class="flex h-3.5 w-3.5 shrink-0 items-center justify-center rounded-sm border border-input {value.includes(
								option.id
							)
								? 'border-primary bg-primary'
								: ''}"
						>
							{#if value.includes(option.id)}
								<Check size={10} class="text-primary-foreground" />
							{/if}
						</div>
						{option.name}
					</button>
				{:else}
					<p class="text-xs text-muted-foreground px-2 py-1.5">No results.</p>
				{/each}
			</div>
		</Popover.Content>
	</Popover.Portal>
</Popover.Root>
