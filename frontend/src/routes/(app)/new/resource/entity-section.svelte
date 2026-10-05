<script lang="ts">
	import { X, User, Building2, Briefcase, Globe, Mail, Tag, Pencil } from '@lucide/svelte';
	import RoleBadge from '$lib/components/role-badge.svelte';
	import type { EntityEntry } from './types.js';

	let {
		title,
		subtitle,
		entries = $bindable<EntityEntry[]>([]),
		variant,
		onadd,
		onedit
	}: {
		title: string;
		subtitle: string;
		entries: EntityEntry[];
		variant: 'authors' | 'persons' | 'organisations';
		onadd: () => void;
		onedit: (index: number) => void;
	} = $props();
</script>

<div class="flex flex-col gap-2">
	<div>
		<h3 class="text-xs font-medium tracking-wide text-foreground uppercase">{title}</h3>
		<p class="text-xs text-muted-foreground">{subtitle}</p>
	</div>
	{#each entries as entry, i (i)}
		{#if i > 0}<div class="border-t border-border/50"></div>{/if}
		<div class="flex flex-col gap-0.5">
			<div class="flex items-start justify-between gap-2">
				<div class="flex min-w-0 flex-1 flex-col gap-0.5">
					<span class="truncate text-sm font-medium">{entry.displayValue || entry.extracted}</span>
					{#if variant !== 'authors' && entry.reason && entry.role !== 'production'}
						<span class="text-xs text-muted-foreground italic">{entry.reason}</span>
					{/if}
				</div>
				<button
					onclick={() => onedit(i)}
					class="mt-0.5 shrink-0 cursor-pointer text-muted-foreground hover:text-foreground"
				>
					<Pencil size={14} />
				</button>
				<button
					onclick={() => (entries = entries.filter((_, j) => j !== i))}
					class="mt-0.5 shrink-0 cursor-pointer text-muted-foreground hover:text-foreground"
				>
					<X size={14} />
				</button>
			</div>
			<div class="flex flex-wrap items-center gap-1 pt-0.5">
				{#if variant === 'authors' && entry.authorType}
					<button
						onclick={() => {
							entry.authorType = entry.authorType === 'organisation' ? 'person' : 'organisation';
						}}
						class="flex cursor-pointer items-center gap-1 rounded border border-border/50 px-1.5 py-0.5 text-xs text-muted-foreground"
					>
						{#if entry.authorType === 'organisation'}
							<Building2 size={10} />Org
						{:else}
							<User size={10} />Person
						{/if}
					</button>
				{/if}
				{#if variant !== 'authors'}
					<RoleBadge bind:role={entry.role} editable />
				{/if}
				{#each entry.suggestedAliases ?? [] as alias (alias)}
					<span
						class="flex items-center gap-1 rounded bg-green-500/15 px-1.5 py-0.5 text-xs font-medium text-green-400"
						title="Alias: will be added as alias to {entry.displayValue}"
					>
						<Tag size={10} />{alias}
						<button
							onclick={() =>
								(entries = entries.map((e, j) =>
									j === i
										? { ...e, suggestedAliases: e.suggestedAliases?.filter((a) => a !== alias) }
										: e
								))}
							class="cursor-pointer opacity-60 hover:opacity-100"
						>
							<X size={10} />
						</button>
					</span>
				{/each}
				{#if entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
					<span
						class="rounded bg-amber-500/15 px-1.5 py-0.5 text-xs font-medium text-amber-400"
						title="Not present in the database">New</span
					>
				{/if}
			</div>
			{#if variant === 'organisations'}
				{#if entry.website || entry.email}
					<div class="flex flex-wrap items-center gap-1 pt-0.5">
						{#if entry.website}
							<a
								href={entry.website.startsWith('http') ? entry.website : 'https://' + entry.website}
								target="_blank"
								rel="noreferrer"
								class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground hover:text-foreground"
								title="Website"
							>
								<Globe size={10} />{entry.website}
							</a>
						{/if}
						{#if entry.email}
							<span
								class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground"
								title="Email"
							>
								<Mail size={10} />{entry.email}
							</span>
						{/if}
					</div>
				{/if}
			{:else if entry.occupation || entry.email}
				<div class="flex flex-wrap items-center gap-1 pt-0.5">
					{#if entry.occupation}
						<span
							class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground"
							title="Occupation"
						>
							<Briefcase size={10} />{entry.occupation}
						</span>
					{/if}
					{#if entry.email}
						<span
							class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground"
							title="Email"
						>
							<Mail size={10} />{entry.email}
						</span>
					{/if}
				</div>
			{/if}
		</div>
	{/each}
	<button
		onclick={onadd}
		class="w-full cursor-pointer rounded border border-dashed border-border py-1.5 text-xs text-muted-foreground transition-colors hover:border-foreground/40 hover:text-foreground"
		>+ Add</button
	>
</div>
