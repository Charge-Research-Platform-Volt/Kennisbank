<script lang="ts">
	import { goto } from '$app/navigation';
	import { api } from '$lib/api';
	import AsyncSelect from '$lib/components/ui/async-select.svelte';
	import Button from '$lib/components/ui/button/button.svelte';
	import { fly } from 'svelte/transition';
	import { LanguageCodes } from '$lib/lists/languageCodes';
	import type { DatePrecision, ExtractedMetadata } from '$lib/types/resource';
	import { User, Building2, X, Briefcase, Globe, Mail, Tag } from '@lucide/svelte';
	import BadgeInput from '$lib/components/ui/badge-input.svelte';
	import { toast } from 'svelte-sonner';
	import ProcessingPhase from './processing-phase.svelte';
	import SelectPhase from './select-phase.svelte';
	import DuplicatePhase from './duplicate-phase.svelte';
	import type { ProjectListResponse } from '$lib/types/project';
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import AsyncMultiSelect from '$lib/components/ui/async-multi-select.svelte';
	import type { ListItem, PagedResult } from '$lib/types/results';
	import RoleBadge from '$lib/components/role-badge.svelte';

	type Phase = 'select' | 'processing' | 'review' | 'duplicate';
	type EntityEntry = {
		extracted: string;
		value: string;
		displayValue: string;
		role?: string;
		authorType?: string;
		suggestedAlias?: string | null;
		reason?: string | null;
		occupation?: string | null;
		website?: string | null;
		email?: string | null;
	};

	let phase = $state<Phase>('select');
	let mode = $state<'file' | 'url'>('file');

	// URL mode
	let url = $state('');

	let jobId = $state<string | null>(null);
	let duplicateId = $state<string | null>(null);
	let fileId = $state<string | null>(null);
	let fileHash = $state<string | null>(null);
	let fileExtension = $state('');

	// Review phase
	let resourceTypeDisplay = $state<string | null>(null);
	let languageDisplay = $state<string | null>(null);
	let journalDisplay = $state<string | null>(null);

	const searchResourceTypes = async (q: string) => {
		const url = q
			? `/api/resource-types?search=${encodeURIComponent(q)}`
			: '/api/resource-types';
		const result = await api.get<PagedResult<ListItem>>(url);
		return result.items;
	};

	const createResourceType = async (name: string) => {
		const result = await api.put<string>('/api/resource-types', { name });
		return { id: result, name };
	};

	const searchLanguages = (q: string) =>
		Promise.resolve(
			LanguageCodes.filter((l) => l.label.toLowerCase().includes(q.toLowerCase())).map((l) => ({
				id: l.value,
				name: l.label
			}))
		);

async function searchTags(q: string) {
		const url = q
			? `/api/tags?page=1&pageSize=20&search=${encodeURIComponent(q)}`
			: '/api/tags?page=1&pageSize=20';
		const r = await api.get<PagedResult<ListItem>>(url);
		return r.items ?? [];
	}

	async function createTag(name: string): Promise<ListItem | null> {
		try {
			const id = await api.put<string>('/api/tags', { name });
			return { id, name };
		} catch {
			return null;
		}
	}

	async function searchRegions(q: string) {
		const url = q
			? `/api/regions?page=1&pageSize=20&search=${encodeURIComponent(q)}`
			: '/api/regions?page=1&pageSize=20';
		const r = await api.get<PagedResult<ListItem>>(url);
		return r.items ?? [];
	}

	async function createRegion(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/regions', { name });
		return { id: r, name };
	}

	async function searchJournals(q: string) {
		const url = q
			? `/api/journals?page=1&pageSize=20&search=${encodeURIComponent(q)}`
			: '/api/journals?page=1&pageSize=20';
		const r = await api.get<PagedResult<ListItem>>(url);
		return r.items ?? [];
	}

	async function createJournal(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/journals', { name });
		return { id: r, name };
	}

	let resourceInfo = $state({
		typeId: '',
		title: '',
		languageCode: '',
		publicationDate: '',
		publicationDatePrecision: 'Day' as DatePrecision,
		journalId: '',
		abstract: '',
		description: '',
		publicationCode: '',
		license: '',
		sourceUrl: '',
		note: ''
	});

	let tags = $state<{ id: string; name: string }[]>([]);
	let regions = $state<{ id: string; name: string }[]>([]);
	let authors = $state<EntityEntry[]>([]);
	let organisations = $state<EntityEntry[]>([]);
	let relatedPersons = $state<EntityEntry[]>([]);
	let dateDay = $state('');
	let dateMonth = $state('');
	let dateYear = $state('');

	let sourceProjectId = $state<string | null>(null);
	let projectIds = $state<string[]>([]);

	function autoresize(node: HTMLTextAreaElement) {
		function resize() {
			node.style.height = 'auto';
			node.style.height = node.scrollHeight + 'px';
		}
		node.addEventListener('input', resize);
		resize();
		return { destroy: () => node.removeEventListener('input', resize) };
	}

	function updateDate() {
		if (dateYear && dateMonth && dateDay) {
			resourceInfo.publicationDatePrecision = 'Day';
			resourceInfo.publicationDate = `${dateYear}-${dateMonth.padStart(2, '0')}-${dateDay.padStart(2, '0')}`;
		} else if (dateYear && dateMonth) {
			resourceInfo.publicationDatePrecision = 'Month';
			resourceInfo.publicationDate = `${dateYear}-${dateMonth.padStart(2, '0')}-01`;
		} else if (dateYear) {
			resourceInfo.publicationDatePrecision = 'Year';
			resourceInfo.publicationDate = `${dateYear}-01-01`;
		} else {
			resourceInfo.publicationDate = '';
		}
	}

	function setDateFromIso(date: string, precision: DatePrecision) {
		if (!date) return;
		const d = new Date(date);
		dateYear = String(d.getFullYear());
		dateMonth = precision !== 'Year' ? String(d.getMonth() + 1) : '';
		dateDay = precision === 'Day' ? String(d.getDate()) : '';
	}

	let isSubmitting = $state(false);

	async function handleSubmit() {
		isSubmitting = true;

		try {
			const createDto = {
				Title: resourceInfo.title,
				Description: resourceInfo.description || null,
				TypeId: resourceInfo.typeId,
				LanguageCode: resourceInfo.languageCode,
				PublicationCode: resourceInfo.publicationCode || null,
				PublicationDate: resourceInfo.publicationDate
					? new Date(resourceInfo.publicationDate).toISOString()
					: null,
				PublicationDatePrecision: resourceInfo.publicationDate
					? resourceInfo.publicationDatePrecision
					: null,
				JournalId: resourceInfo.journalId || null,
				License: resourceInfo.license || null,
				SourceUrl: mode === 'url' ? url : resourceInfo.sourceUrl || null,
				Note: resourceInfo.note || null,
				Tags: tags.map((t) => t.id),
				Authors: authors.map((a) => ({
					value: a.value,
					type: a.authorType?.toLowerCase() ?? 'person',
					suggestedAlias: a.suggestedAlias ?? null,
					occupation: a.occupation ?? null,
					email: a.email ?? null
				})),
				Organisations: organisations.map((o) => ({ 
					Id: o.value, 
					Relation: o.role || null, 
					SuggestedAlias: o.suggestedAlias ?? null,
					Occupation: o.occupation ?? null,
					Website: o.website ?? null,
					Email: o.email ?? null
				})),
				RelatedPersons: relatedPersons.map((p) => ({ 
					Id: p.value, 
					Relation: p.role || null, 
					SuggestedAlias: p.suggestedAlias ?? null,
					Occupation: p.occupation ?? null,
					Website: p.website ?? null,
					Email: p.email ?? null
				})),
				Regions: regions.map((r) => r.id),
				FileId: mode === 'file' ? fileId : null,
				Hash: mode === 'file' ? fileHash : null,
				fileExtension: mode === 'file' ? fileExtension : null
			};

			const result = await api.put<string>('/api/resources', createDto);

			// add to selected projects
			if (projectIds.length > 0) {
				const results = await Promise.allSettled(
					projectIds.map((pid) => api.post(`/api/projects/${pid}/items/${result}`, {}))
				);

				const failed = results.filter((r) => r.status === 'rejected').length;
				if (failed > 0)
					toast.error(
						`Added resource, but failed to link ${failed} project${failed > 1 ? 's' : ''}.`
					);
				else
					toast.success(
						`Linked resource to ${results.length} project${results.length > 1 ? 's' : ''}.`
					);
			}

			if (sourceProjectId)
				goto(`/projects/${sourceProjectId}?inspectorId=${result}&inspectorType=resource`);
			else goto(`/library?inspectorId=${result}&inspectorType=resource`);
		} catch (e) {
			toast.error(e instanceof Error ? e.message : 'Something went wrong');
		} finally {
			isSubmitting = false;
		}
	}

	function onSelectComplete(result: {
		mode: 'file' | 'url';
		url: string;
		jobId: string;
		fileId: string | null;
		fileHash: string | null;
		fileExtension: string;
	}) {
		mode = result.mode;
		url = result.url;
		jobId = result.jobId;
		fileId = result.fileId;
		fileHash = result.fileHash;
		fileExtension = result.fileExtension;
		phase = 'processing';
	}

	function onDuplicate(id: string) {
		duplicateId = id;
		phase = 'duplicate';
	}

	async function onProcessingComplete(metadata: ExtractedMetadata | null) {
		if (metadata) {
			if (mode === 'url') resourceInfo.sourceUrl = url;
			resourceInfo.title = metadata.title ?? '';
			resourceInfo.languageCode = metadata.languageCode ?? '';
			languageDisplay =
				LanguageCodes.find((l) => l.value === resourceInfo.languageCode)?.label ?? null;
			resourceInfo.publicationDate = metadata.publicationDate ?? '';
			resourceInfo.publicationDatePrecision = metadata.publicationDatePrecision ?? 'Year';
			setDateFromIso(resourceInfo.publicationDate, resourceInfo.publicationDatePrecision);
			resourceInfo.abstract = metadata.abstract ?? '';
			resourceInfo.description = metadata.description ?? '';
			resourceInfo.publicationCode = metadata.publicationCode ?? '';

			tags = await Promise.all(
				metadata.tags.map(async (name: string) => {
					const results = await searchTags(name);
					const exact = results.find((r) => r.name.toLowerCase() === name.toLowerCase());
					return exact ?? { id: name, name };
				})
			);

			regions = await Promise.all(
				(metadata.regions ?? []).map(async (name: string) => {
					const results = await searchRegions(name);
					const exact = results.find((r) => r.name.toLowerCase() === name.toLowerCase());
					return exact ?? { id: name, name };
				})
			);

			const toEntry = (e: {
				name: string;
				type: string;
				role?: string;
				reason?: string | null;
				occupation?: string | null;
				website?: string | null;
				email?: string | null;
				suggestedAlias?: string | null;
				confirmedMatch: { id: string; name: string; suggestedAlias?: string | null } | null;
			}): EntityEntry => {
				const confirmed = e.confirmedMatch;
				const meta = {
					reason: e.reason ?? null,
					occupation: e.occupation ?? null,
					website: e.website ?? null,
					email: e.email ?? null
				};
				return confirmed
					? {
							extracted: e.name,
							value: confirmed.id,
							displayValue: confirmed.name,
							authorType: e.type,
							role: e.role,
							suggestedAlias: confirmed.suggestedAlias ?? e.suggestedAlias ?? null,
							...meta
						}
					: {
							extracted: e.name,
							value: e.name,
							displayValue: e.name,
							authorType: e.type,
							role: e.role,
							suggestedAlias: e.suggestedAlias ?? null,
							...meta
						};
			};

			authors = metadata.authors.map(toEntry);
			organisations = metadata.organisations.map(toEntry);
			relatedPersons = metadata.relatedPersons.map(toEntry);

			if (metadata.license) resourceInfo.license = metadata.license;

			if (metadata.resourceTypeName) {
				const rtResults = await searchResourceTypes(metadata.resourceTypeName);
				const exactRt = rtResults.find(
					(r) => r.name.toLowerCase() === metadata.resourceTypeName!.toLowerCase()
				);
				if (exactRt) {
					resourceInfo.typeId = exactRt.id;
					resourceTypeDisplay = exactRt.name;
				}
			}

			if (metadata.journal) {
				const journalResults = await searchJournals(metadata.journal);
				const exactJournal = journalResults.find(
					(r) => r.name.toLowerCase() === metadata.journal!.toLowerCase()
				);
				if (exactJournal) {
					resourceInfo.journalId = exactJournal.id;
					journalDisplay = exactJournal.name;
				} else {
					const created = await createJournal(metadata.journal);
					if (created) {
						resourceInfo.journalId = created.id;
						journalDisplay = created.name;
					}
				}
			}
		}

		phase = 'review';
	}

	function onProcessingSkip() {
		phase = 'review';
	}

	async function searchProjects(q: string) {
		const result = await api.post<ProjectListResponse>('/api/projects', {
			page: 1,
			pageSize: 20,
			search: q || undefined
		});

		return result.items.map((p) => ({ id: p.id, name: p.title }));
	}

	onMount(() => {
		const paramId = page.url.searchParams.get('projectId');
		if (paramId) {
			projectIds = [paramId];
			sourceProjectId = paramId;
		}
	});
</script>

{#if phase === 'review'}
	<div class="flex h-full flex-col gap-4 p-6">
		<!-- Header -->
		<div class="flex items-center justify-between border-b border-border pb-4">
			<h1 class="text-2xl font-semibold">Review Resource</h1>

			<div class="flex items-center gap-4">
				<AsyncMultiSelect
					bind:value={projectIds}
					search={searchProjects}
					placeholder="Add to projects..."
				/>

				<Button onclick={handleSubmit} disabled={isSubmitting} class="cursor-pointer">
					{isSubmitting ? 'Saving...' : 'Save Resource'}
				</Button>
			</div>
		</div>

		<!-- Grid -->
		<div class="grid min-h-0 flex-1 grid-cols-[3fr_2fr] divide-x divide-border">
			<!-- Resource information -->
			<div class="flex flex-col gap-6 overflow-y-auto px-6 py-4 pb-10">
				<!-- Title -->
				<div
					class="flex flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
				>
					{#if resourceInfo.title}
						<span
							transition:fly={{ y: 4, duration: 150 }}
							class="pb-1 text-xs text-muted-foreground">Title</span
						>
					{/if}
					<input
						bind:value={resourceInfo.title}
						placeholder="Title"
						class="w-full bg-transparent text-xl font-semibold outline-none placeholder:text-muted-foreground/50"
					/>
				</div>

				<!-- Type + Language -->
				<div class="flex gap-4">
					<div
						class="flex min-w-0 flex-1 flex-col gap-0.5 border-b border-transparent transition-colors focus-within:border-border"
					>
						<span class="pb-1 text-xs text-muted-foreground">Type</span>
						<AsyncSelect
							bind:value={resourceInfo.typeId}
							bind:displayValue={resourceTypeDisplay}
							search={searchResourceTypes}
							oncreate={createResourceType}
							placeholder="Select or create type..."
							variant="flat"
						/>
					</div>
					<div
						class="flex min-w-0 flex-1 flex-col gap-0.5 border-b border-transparent transition-colors focus-within:border-border"
					>
						<span class="pb-1 text-xs text-muted-foreground">Language</span>
						<AsyncSelect
							bind:value={resourceInfo.languageCode}
							bind:displayValue={languageDisplay}
							search={searchLanguages}
							placeholder="Select language..."
							variant="flat"
						/>
					</div>
				</div>

				<!-- Abstract -->
				{#if mode === 'file'}
					<div
						class="flex flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
					>
						{#if resourceInfo.abstract}
							<span
								transition:fly={{ y: 4, duration: 150 }}
								class="pb-1 text-xs text-muted-foreground">Abstract</span
							>
						{/if}
						<textarea
							bind:value={resourceInfo.abstract}
							placeholder="Abstract"
							rows={1}
							use:autoresize
							class="w-full resize-none overflow-hidden bg-transparent text-sm outline-none placeholder:text-muted-foreground/50"
						></textarea>
					</div>
				{/if}

				<!-- Description -->
				<div
					class="flex flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
				>
					{#if resourceInfo.description}
						<span
							transition:fly={{ y: 4, duration: 150 }}
							class="pb-1 text-xs text-muted-foreground">Description</span
						>
					{/if}
					<textarea
						bind:value={resourceInfo.description}
						placeholder="Description"
						rows={1}
						use:autoresize
						class="w-full resize-none overflow-hidden bg-transparent text-sm outline-none placeholder:text-muted-foreground/50"
					></textarea>
				</div>

				<!-- Tags -->
				<div class="flex flex-col gap-0.5">
					<span class="pb-1 text-xs text-muted-foreground">Tags</span>
					<BadgeInput
						bind:items={tags}
						search={searchTags}
						oncreate={createTag}
						placeholder="Search or create tag..."
					/>
				</div>

				<!-- Regions -->
				<div class="flex flex-col gap-0.5">
					<span class="pb-1 text-xs text-muted-foreground">Regions</span>
					<BadgeInput
						bind:items={regions}
						search={searchRegions}
						oncreate={createRegion}
						placeholder="Search or create region..."
					/>
				</div>

				<!-- Publication Date + Publication Code -->
				<div class="flex items-end gap-4">
					<div
						class="flex min-w-0 flex-1 flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
					>
						<span class="pb-1 text-xs text-muted-foreground">Publication Date</span>
						<div class="flex items-center gap-1 text-sm">
							<input
								type="number"
								bind:value={dateDay}
								onblur={updateDate}
								min="1"
								max="31"
								placeholder="DD"
								class="w-8 bg-transparent outline-none placeholder:text-muted-foreground/50"
							/>
							<span class="text-muted-foreground/30">/</span>
							<input
								type="number"
								bind:value={dateMonth}
								onblur={updateDate}
								min="1"
								max="12"
								placeholder="MM"
								class="w-8 bg-transparent outline-none placeholder:text-muted-foreground/50"
							/>
							<span class="text-muted-foreground/30">/</span>
							<input
								type="number"
								bind:value={dateYear}
								onblur={updateDate}
								min="1000"
								max="2100"
								placeholder="YYYY"
								class="w-14 bg-transparent outline-none placeholder:text-muted-foreground/50"
							/>
						</div>
					</div>

					<div
						class="flex min-w-0 flex-1 flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
					>
						{#if resourceInfo.publicationCode}
							<span
								transition:fly={{ y: 4, duration: 150 }}
								class="pb-1 text-xs text-muted-foreground">Publication Code</span
							>
						{/if}
						<input
							bind:value={resourceInfo.publicationCode}
							placeholder="Publication Code"
							class="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground/50"
						/>
					</div>
				</div>

				<!-- License + Journal -->
				<div class="flex items-end gap-4">
					<div
						class="flex min-w-0 flex-1 flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
					>
						{#if resourceInfo.license}
							<span
								transition:fly={{ y: 4, duration: 150 }}
								class="pb-1 text-xs text-muted-foreground">License</span
							>
						{/if}
						<input
							bind:value={resourceInfo.license}
							placeholder="License"
							class="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground/50"
						/>
					</div>
					<div
						class="flex min-w-0 flex-1 flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
					>
						<span class="pb-1 text-xs text-muted-foreground">Journal</span>
						<AsyncSelect
							bind:value={resourceInfo.journalId}
							bind:displayValue={journalDisplay}
							search={searchJournals}
							oncreate={createJournal}
							placeholder="Select or create journal..."
							variant="flat"
						/>
					</div>
				</div>

				<!-- Source URL -->
				<div
					class="flex flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
				>
					{#if resourceInfo.sourceUrl}
						<span
							transition:fly={{ y: 4, duration: 150 }}
							class="pb-1 text-xs text-muted-foreground">Source URL</span
						>
					{/if}
					<input
						type="url"
						bind:value={resourceInfo.sourceUrl}
						placeholder="Source URL"
						class="w-full bg-transparent text-sm outline-none placeholder:text-muted-foreground/50"
					/>
				</div>

				<!-- Note -->
				<div
					class="flex flex-col gap-0.5 border-b border-transparent pb-1 transition-colors focus-within:border-border"
				>
					{#if resourceInfo.note}
						<span
							transition:fly={{ y: 4, duration: 150 }}
							class="pb-1 text-xs text-muted-foreground">Note</span
						>
					{/if}
					<textarea
						bind:value={resourceInfo.note}
						placeholder="Notes"
						rows={1}
						use:autoresize
						class="w-full resize-none overflow-hidden bg-transparent text-sm outline-none placeholder:text-muted-foreground/50"
					></textarea>
				</div>
			</div>

			<!-- Connections -->
			<div class="flex flex-col gap-4 overflow-y-auto px-4 py-2 pb-10">
				<!-- Authors -->
				<div class="flex flex-col gap-2">
					<div>
						<h3 class="text-xs font-medium tracking-wide text-foreground uppercase">Authors</h3>
						<p class="text-xs text-muted-foreground">Wrote or contributed to this resource</p>
					</div>
					{#each authors as entry, i (i)}
						{#if i > 0}<div class="border-t border-border/50"></div>{/if}
						<div class="flex flex-col gap-0.5">
							<div class="flex items-start justify-between gap-2">
								<div class="flex min-w-0 flex-1 flex-col gap-0.5">
									<span class="truncate text-sm font-medium">{entry.displayValue || entry.extracted}</span>
								</div>
								<button
									onclick={() => (authors = authors.filter((_, j) => j !== i))}
									class="mt-0.5 shrink-0 cursor-pointer text-muted-foreground hover:text-foreground"
								>
									<X size={14} />
								</button>
							</div>
							<div class="flex flex-wrap items-center gap-1 pt-0.5">
								{#if entry.authorType}
									<span class="flex items-center gap-1 rounded border border-border/50 px-1.5 py-0.5 text-xs text-muted-foreground">
										{#if entry.authorType === 'organisation'}
											<Building2 size={10} />Org
										{:else}
											<User size={10} />Person
										{/if}
									</span>
								{/if}
								{#if entry.suggestedAlias}
									<span class="flex items-center gap-1 rounded bg-green-500/15 px-1.5 py-0.5 text-xs font-medium text-green-400" title="Alias: will be added as alias to {entry.displayValue}"><Tag size={10} />{entry.suggestedAlias}</span>
								{/if}
								{#if entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
									<span class="rounded bg-amber-500/15 px-1.5 py-0.5 text-xs font-medium text-amber-400" title="Not present in the database">New</span>
								{/if}
							</div>
							{#if entry.occupation || entry.email}
								<div class="flex flex-wrap items-center gap-1 pt-0.5">
									{#if entry.occupation}
										<span class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground" title="Occupation">
											<Briefcase size={10} />{entry.occupation}
										</span>
									{/if}
									{#if entry.email}
										<span class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground" title="Email">
											<Mail size={10} />{entry.email}
										</span>
									{/if}
								</div>
							{/if}
						</div>
					{/each}
					<button
						onclick={() =>
							(authors = [
								...authors,
								{ extracted: '', value: '', displayValue: '', authorType: 'person' }
							])}
						class="w-full cursor-pointer rounded border border-dashed border-border py-1.5 text-xs text-muted-foreground transition-colors hover:border-foreground/40 hover:text-foreground"
						>+ Add</button
					>
				</div>

				<div class="border-t border-border"></div>

				<!-- Related Persons -->
				<div class="flex flex-col gap-2">
					<div>
						<h3 class="text-xs font-medium tracking-wide text-foreground uppercase">People</h3>
						<p class="text-xs text-muted-foreground">Mentioned or otherwise connected</p>
					</div>
					{#each relatedPersons as entry, i (i)}
						{#if i > 0}<div class="border-t border-border/50"></div>{/if}
						<div class="flex flex-col gap-0.5">
							<div class="flex items-start justify-between gap-2">
								<div class="flex min-w-0 flex-1 flex-col gap-0.5">
									<span class="truncate text-sm font-medium">{entry.displayValue || entry.extracted}</span>
									{#if entry.reason && entry.role !== 'production'}
										<span class="text-xs italic text-muted-foreground">{entry.reason}</span>
									{/if}
								</div>
								<button
									onclick={() => (relatedPersons = relatedPersons.filter((_, j) => j !== i))}
									class="mt-0.5 shrink-0 cursor-pointer text-muted-foreground hover:text-foreground"
								>
									<X size={14} />
								</button>
							</div>
							<div class="flex flex-wrap items-center gap-1 pt-0.5">
								<RoleBadge bind:role={entry.role} editable />
								{#if entry.suggestedAlias}
									<span class="flex items-center gap-1 rounded bg-green-500/15 px-1.5 py-0.5 text-xs font-medium text-green-400" title="Alias: will be added as alias to {entry.displayValue}"><Tag size={10} />{entry.suggestedAlias}</span>
								{/if}
								{#if entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
									<span class="rounded bg-amber-500/15 px-1.5 py-0.5 text-xs font-medium text-amber-400" title="Not present in the database">New</span>
								{/if}
							</div>
							{#if entry.occupation || entry.email}
								<div class="flex flex-wrap items-center gap-1 pt-0.5">
									{#if entry.occupation}
										<span class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground" title="Occupation">
											<Briefcase size={10} />{entry.occupation}
										</span>
									{/if}
									{#if entry.email}
										<span class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground" title="Email">
											<Mail size={10} />{entry.email}
										</span>
									{/if}
								</div>
							{/if}
						</div>
					{/each}
					<button
						onclick={() =>
							(relatedPersons = [
								...relatedPersons,
								{ extracted: '', value: '', displayValue: '' }
							])}
						class="w-full cursor-pointer rounded border border-dashed border-border py-1.5 text-xs text-muted-foreground transition-colors hover:border-foreground/40 hover:text-foreground"
						>+ Add</button
					>
				</div>

				<div class="border-t border-border"></div>

				<!-- Related Organisations -->
				<div class="flex flex-col gap-2">
					<div>
						<h3 class="text-xs font-medium tracking-wide text-foreground uppercase">
							Organisations
						</h3>
						<p class="text-xs text-muted-foreground">Mentioned or otherwise connected</p>
					</div>
					{#each organisations as entry, i (i)}
						{#if i > 0}<div class="border-t border-border/50"></div>{/if}
						<div class="flex flex-col gap-0.5">
							<div class="flex items-start justify-between gap-2">
								<div class="flex min-w-0 flex-1 flex-col gap-0.5">
									<span class="truncate text-sm font-medium">{entry.displayValue || entry.extracted}</span>
									{#if entry.reason && entry.role !== 'production'}
										<span class="text-xs italic text-muted-foreground">{entry.reason}</span>
									{/if}
								</div>
								<button
									onclick={() => (organisations = organisations.filter((_, j) => j !== i))}
									class="mt-0.5 shrink-0 cursor-pointer text-muted-foreground hover:text-foreground"
								>
									<X size={14} />
								</button>
							</div>
							<div class="flex flex-wrap items-center gap-1 pt-0.5">
								<RoleBadge bind:role={entry.role} editable />
								{#if entry.suggestedAlias}
									<span class="flex items-center gap-1 rounded bg-green-500/15 px-1.5 py-0.5 text-xs font-medium text-green-400" title="Alias: will be added as alias to {entry.displayValue}"><Tag size={10} />{entry.suggestedAlias}</span>
								{/if}
								{#if entry.value && !entry.value.match(/^[0-9a-f-]{36}$/i)}
									<span class="rounded bg-amber-500/15 px-1.5 py-0.5 text-xs font-medium text-amber-400" title="Not present in the database">New</span>
								{/if}
							</div>
							{#if entry.website || entry.email}
								<div class="flex flex-wrap items-center gap-1 pt-0.5">
									{#if entry.website}
										<a href={entry.website} target="_blank" rel="noreferrer" class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground hover:text-foreground" title="Website">
											<Globe size={10} />{entry.website}
										</a>
									{/if}
									{#if entry.email}
										<span class="flex items-center gap-1 rounded bg-muted px-1.5 py-0.5 text-xs text-muted-foreground" title="Email">
											<Mail size={10} />{entry.email}
										</span>
									{/if}
								</div>
							{/if}
						</div>
					{/each}
					<button
						onclick={() =>
							(organisations = [...organisations, { extracted: '', value: '', displayValue: '' }])}
						class="w-full cursor-pointer rounded border border-dashed border-border py-1.5 text-xs text-muted-foreground transition-colors hover:border-foreground/40 hover:text-foreground"
						>+ Add</button
					>
				</div>
			</div>
		</div>
	</div>
{:else}
	<div class="flex h-full items-start justify-center p-8 pt-24">
		<div class="flex w-full max-w-xl flex-col gap-6">
			<!-- Header -->
			<div>
				{#if phase === 'processing'}
					<h1 class="text-center text-2xl font-semibold">Analyzing Resource...</h1>
				{:else if phase === 'duplicate'}
					<h1 class="text-2xl font-semibold">Duplicate Found</h1>
				{:else}
					<h1 class="text-2xl font-semibold">Add Resource</h1>
				{/if}
			</div>

			{#if phase === 'select'}
				<SelectPhase oncomplete={onSelectComplete} onduplicate={onDuplicate} />
			{:else if phase === 'processing'}
				<ProcessingPhase
					jobId={jobId!}
					oncomplete={onProcessingComplete}
					onskip={onProcessingSkip}
				/>
			{:else if phase === 'duplicate'}
				<DuplicatePhase duplicateId={duplicateId!} onback={() => (phase = 'select')} />
			{/if}
		</div>
	</div>
{/if}

<style>
	input[type='number']::-webkit-inner-spin-button,
	input[type='number']::-webkit-outer-spin-button {
		-webkit-appearance: none;
		appearance: none;
		margin: 0;
	}
	input[type='number'] {
		-moz-appearance: textfield;
		appearance: textfield;
	}
</style>
