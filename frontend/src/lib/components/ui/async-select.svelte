<script lang="ts">
	import { Popover } from 'bits-ui';
	import { debounce } from '$lib/utils/debounce';
	import { Check, ChevronDown, Search, X } from '@lucide/svelte';

	let {
		value = $bindable<string | null>(null),
		displayValue = $bindable<string | null>(null),
		search,
		placeholder = 'Select...',
		class: className = '',
		variant = 'default' as 'default' | 'ghost' | 'flat',
		onchange,
		oncreate,
		allowClear = true
	}: {
		value: string | null;
		displayValue: string | null;
		search: (q: string) => Promise<{ id: string; name: string }[]>;
		placeholder?: string;
		class?: string;
		variant?: 'default' | 'ghost' | 'flat';
		onchange?: (id: string | null, name: string | null) => void;
		oncreate?: (name: string) => Promise<{ id: string; name: string } | null>;
		allowClear?: boolean;
	} = $props();

	let open = $state(false);
	let options = $state<{ id: string; name: string }[]>([]);
	let searchQuery = $state('');
	const debouncedSearch = debounce((q: string) => search(q).then((r) => (options = r)));

	function onSearchInput(q: string) {
		searchQuery = q;
		debouncedSearch(q);
	}

	$effect(() => {
		if (open) {
			searchQuery = '';
			search('').then((r) => (options = r));
		}
	});

	function select(option: { id: string; name: string }) {
		value = option.id;
		displayValue = option.name;
		open = false;
		onchange?.(option.id, option.name);
	}

	async function create() {
		if (!oncreate || !searchQuery.trim()) return;
		const result = await oncreate(searchQuery.trim());
		if (result) {
			value = result.id;
			displayValue = result.name;
			open = false;
			onchange?.(result.id, result.name);
		}
	}

	function clear(e: MouseEvent) {
		e.stopPropagation();
		value = null;
		displayValue = null;
		onchange?.(null, null);
	}
</script>

<Popover.Root bind:open>
	<Popover.Trigger
		class="
        {variant === 'ghost'
			? 'flex w-full cursor-pointer items-center gap-1.5 bg-transparent text-sm'
			: variant === 'flat'
				? 'flex w-full cursor-pointer items-center gap-1.5 border-b bg-transparent text-sm transition-colors ' +
					(open ? 'border-border' : 'border-transparent')
				: 'flex h-9 w-full cursor-pointer items-center gap-1.5 rounded-md border border-input bg-background px-3 text-sm transition-colors hover:bg-accent'}
        {className}"
	>
		<span
			class="min-w-0 flex-1 truncate text-left {displayValue
				? ''
				: variant === 'flat'
					? 'text-muted-foreground/50'
					: 'text-muted-foreground'}">{displayValue ?? placeholder}</span
		>
		{#if displayValue && allowClear}
			<button
				onclick={clear}
				class="shrink-0 cursor-pointer text-muted-foreground transition-colors hover:text-foreground"
			>
				<X size={12} />
			</button>
		{:else}
			<ChevronDown size={12} class="shrink-0 text-muted-foreground" />
		{/if}
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
					placeholder={oncreate ? 'Search or type to create...' : 'Search...'}
				/>
			</div>
			<div class="max-h-64 overflow-y-auto">
				{#each options as option (option.id)}
					<button
						class="flex w-full cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-left text-xs hover:bg-accent"
						onclick={() => select(option)}
					>
						<div class="flex h-3.5 w-3.5 shrink-0 items-center justify-center">
							{#if value === option.id}
								<Check size={12} class="text-primary" />
							{/if}
						</div>
						{option.name}
					</button>
				{:else}
					<p class="text-xs text-muted-foreground px-2 py-1.5">No results.</p>
				{/each}
				{#if oncreate && searchQuery.trim()}
					<button
						class="flex w-full cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-left text-xs text-muted-foreground hover:bg-accent"
						onclick={create}
					>
						+ Create "{searchQuery.trim()}"
					</button>
				{/if}
			</div>
		</Popover.Content>
	</Popover.Portal>
</Popover.Root>
