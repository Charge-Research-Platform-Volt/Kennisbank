<script lang="ts">
	import type { ListItem, PagedResult } from '$lib/types/results';
	import { goto } from '$app/navigation';
	import { debounce } from '$lib/utils/debounce';
	import { api } from '$lib/api';
	import Input from '$lib/components/ui/input/input.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import Textarea from '$lib/components/ui/textarea/textarea.svelte';
	import Label from '$lib/components/ui/label/label.svelte';
	import { toast } from 'svelte-sonner';
	import type { ProjectListResponse } from '$lib/types/project';
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';

	let name = $state('');
	let occupation = $state('');
	let description = $state('');
	let email = $state('');
	let linkedin = $state('');
	let sourceProjectId = $state<string | null>(null);
	let projectIds = $state<string[]>([]);

	let isSubmitting = $state(false);
	let isCheckingDuplicate = $state(false);
	let duplicate = $state<{ exists: boolean; id: string } | null>(null);
	let similar = $state<ListItem[]>([]);

	const debouncedCheckDuplicate = debounce(checkDuplicate, 500);

	function onNameInput() {
		duplicate = null;
		similar = [];
		if (!name.trim()) return;
		debouncedCheckDuplicate();
	}

	async function checkDuplicate() {
		isCheckingDuplicate = true;

		try {
			const result = await api.get<PagedResult<ListItem>>(`/api/persons?search=${encodeURIComponent(name)}&pageSize=5`);
			const exact = result.items.find(i => i.name.toLowerCase() === name.toLowerCase());
			duplicate = exact ? { exists: true, id: exact.id } : { exists: false, id: '' };
			similar = result.items;
		} catch {
			// Silently ignore
		} finally {
			isCheckingDuplicate = false;
		}
	}

	async function handleSubmit(e: SubmitEvent) {
		e.preventDefault();

		if (!name.trim()) return;

		isSubmitting = true;
		try {
			const result = await api.put<string>('/api/persons', {
				Name: name,
				Occupation: occupation || undefined,
				Description: description || undefined,
				EmailAddress: email || undefined,
				Linkedin: linkedin || undefined
			});

			// Add to selected projects
			if (projectIds.length > 0) {
				const results = await Promise.allSettled(
					projectIds.map((pid) => api.post(`/api/projects/${pid}/items/${result}`, {}))
				);

				const failed = results.filter((r) => r.status === 'rejected').length;
				if (failed > 0)
					toast.error(
						`Added person, but failed to link ${failed} project${failed > 1 ? 's' : ''}.`
					);
				else
					toast.success(
						`Linked person to ${results.length} project${results.length > 1 ? 's' : ''}.`
					);
			}

			if (sourceProjectId)
				goto(`/projects/${sourceProjectId}?inspectorId=${result}&inspectorType=person`);
			else goto(`/library?inspectorId=${result}&inspectorType=person`);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Failed to create person');
		} finally {
			isSubmitting = false;
		}
	}

	async function searchProjects(q: string) {
		const result = await api.post<ProjectListResponse>('/api/projects', {
			page: 1,
			pageSize: 20,
			search: q || undefined
		});

		return result.items.map((p) => ({ id: p.id, name: p.title }));
	}

	const canSubmit = $derived(name.trim().length > 0 && !isSubmitting && !duplicate?.exists);

	onMount(() => {
		const paramId = page.url.searchParams.get('projectId');
		if (paramId) {
			projectIds = [paramId];
			sourceProjectId = paramId;
		}
	});
</script>

<div class="mx-auto flex h-full max-w-2xl flex-col justify-center gap-6 p-8">
	<!-- Header -->
	<div>
		<h1 class="text-2xl font-semibold">Add Person</h1>
		<p class="mt-1 text-sm text-muted-foreground">Add a new person to the library.</p>
	</div>

	<!-- Form -->
	<form onsubmit={handleSubmit} class="flex flex-col gap-6">
		<!-- Card -->
		<div class="flex flex-col gap-4 rounded-lg border border-border p-6">
			<!-- Name (Required) -->
			<div class="flex flex-col gap-1.5">
				<Label for="name">Name <span class="text-destructive">*</span></Label>
				<Input id="name" bind:value={name} oninput={onNameInput} placeholder="Full name" />
				{#if isCheckingDuplicate}
					<p class="text-xs text-muted-foreground">Checking for duplicates...</p>
				{:else if duplicate?.exists}
					<p class="text-xs text-destructive">
						A person with this name already exists.
						<a href="/library?inspectorId={duplicate.id}&inspectorType=person" class="underline"
							>View</a
						>
					</p>
				{:else if similar.length > 0}
					<p class="text-xs text-muted-foreground">
						Similar names already in the library:
						{#each similar as s, i (s.id)}
							<a href="/library?inspectorId={s.id}&inspectorType=person" class="underline"
								>{s.name}</a
							>{#if i < similar.length - 1},&nbsp;{/if}
						{/each}
					</p>
				{/if}
			</div>

			<!-- Occupation -->
			<div class="flex flex-col gap-1.5">
				<Label for="occupation">Occupation</Label>
				<Input id="occupation" bind:value={occupation} placeholder="e.g. Politician" />
			</div>

			<!-- Email -->
			<div class="flex flex-col gap-1.5">
				<Label for="email">Email</Label>
				<Input id="email" type="email" bind:value={email} placeholder="email@example.com" />
			</div>

			<!-- LinkedIn -->
			<div class="flex flex-col gap-1.5">
				<Label for="linkedin">LinkedIn</Label>
				<Input
					id="linkedin"
					type="url"
					bind:value={linkedin}
					placeholder="https://linkedin.com/in/username"
				/>
			</div>

			<!-- Description -->
			<div class="flex flex-col gap-1.5">
				<Label for="description">Description</Label>
				<Textarea
					id="description"
					bind:value={description}
					placeholder="Brief biography or description"
					rows={4}
				/>
			</div>

			<!-- Add to project -->
			<div class="flex flex-col gap-1.5">
				<Label>Add to projects</Label>
				<AsyncMultiSelect
					bind:value={projectIds}
					search={searchProjects}
					placeholder="Add to projects..."
				/>
			</div>
		</div>

		<!-- Actions -->
		<div class="flex justify-end gap-3">
			<Button variant="outline" type="button" onclick={() => history.back()} class="cursor-pointer"
				>Cancel</Button
			>
			<Button type="submit" disabled={!canSubmit} class="cursor-pointer">
				{isSubmitting ? 'Creating...' : 'Create Person'}
			</Button>
		</div>
	</form>
</div>
