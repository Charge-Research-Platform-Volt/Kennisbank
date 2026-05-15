<script lang="ts">
	import { FileText, User, Building2, type LucideIcon } from '@lucide/svelte';
	import { page } from '$app/state';

	const projectId = $derived(page.url.searchParams.get('projectId'));
	const suffix = $derived(projectId ? `?projectId=${projectId}` : '');
</script>

{#snippet card(href: string, icon: LucideIcon, title: string, subtext: string)}
	{@const LucideIcon = icon}

	<a {href} class="rounded-lg border border-border p-6 transition-colors hover:bg-accent">
		<LucideIcon class="mb-4 h-8 w-8 text-muted-foreground" />
		<h2 class="font-medium">{title}</h2>
		<p class="mt-1 text-sm text-muted-foreground">{subtext}</p>
	</a>
{/snippet}

<div class="mx-auto flex h-full max-w-2xl flex-col justify-center gap-8 p-8">
	<div>
		<h1 class="text-2xl font-semibold">Add New</h1>
		<p class="mt-1 text-sm text-muted-foreground">Choose what you want to add to the PowerGrid.</p>
	</div>

	<div class="grid grid-cols-1 gap-4 sm:grid-cols-3">
		{@render card(
			`/new/resource${suffix}`,
			FileText,
			'Resource',
			'Add a document or website to the library'
		)}
		{@render card(`/new/person${suffix}`, User, 'Person', 'Add a person to the library')}
		{@render card(
			`/new/organisation${suffix}`,
			Building2,
			'Organisation',
			'Add an organisation to the library'
		)}
	</div>
</div>
