<script lang="ts">
	import type { ResourceItem, ResourceDetail } from '$lib/types/resource';
	import { api } from '$lib/api';
	import Spinner from '../ui/spinner/spinner.svelte';
	import { Layers, Languages, Scale, FingerprintPattern, Link, Newspaper } from '@lucide/svelte';
	import { formatLanguage } from '$lib/utils/locale';
	import { LanguageCodes } from '$lib/lists/languageCodes';
	import BadgeSection from './badge-section.svelte';
	import RelationSection from './relation-section.svelte';
	import TextSection from './text-section.svelte';
	import CharacteristicRow from './characteristic-row.svelte';
	import AsyncSelect from '$lib/components/ui/async-select.svelte';
	import { getContext } from 'svelte';

	const getEditMode = getContext<() => boolean>('getEditMode');
	let editMode = $derived(getEditMode());

	const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

	const PROPERTIES =
		'Description,LanguageCode,PublicationCode,License,Note,SourceUrl,WebsiteMetadata.Url as Url,DocumentMetadata.Abstract as Abstract,ResourceAuthorRelations.Select(new(Author.Id, Author.Name, Author.EntityType as AuthorType)) as Authors,ResourceOrganisationRelations.Select(new(Organisation.Id, Organisation.Name, Role)) as Organisations,ResourceRegionRelations.Select(new(Region.Id, Region.Name)) as Regions,ResourceRelatedPersonRelations.Select(new(Person.Id, Person.Name, Role)) as RelatedPersons,ResourceTagRelations.Select(new(Tag.Id, Tag.Name)) as Tags,ResourceType.Id as ResourceTypeId,ResourceType.Name as ResourceTypeName,JournalId,Journal.Name as JournalName';

	let { item }: { item: ResourceItem } = $props();

	async function searchResourceTypes(q: string) {
		const url = q
			? `/api/resources/types/list?search=${encodeURIComponent(q)}`
			: '/api/resources/types/list';
		const result = await api.get<{ id: string; name: string }[]>(url);
		return result.body.filter((t) => t.name !== 'Unknown');
	}

	function searchLanguages(q: string) {
		const results = q
			? LanguageCodes.filter(
					(l) =>
						l.label.toLowerCase().includes(q.toLowerCase()) ||
						l.value.toLowerCase().includes(q.toLowerCase())
				)
			: LanguageCodes;
		return Promise.resolve(results.map((l) => ({ id: l.value, name: l.label })));
	}

	async function searchTags(q: string) {
		const url = q
			? `/api/tags/tag-page?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}`
			: '/api/tags/tag-page?pageIndex=1&pageSize=20';
		const r = await api.get<{ tags: { id: string; name: string }[] }>(url);
		return r.body?.tags ?? [];
	}

	async function createTag(name: string): Promise<{ id: string; name: string } | null> {
		const response = await fetch('/api/tags/add-user-tag', {
			method: 'PUT',
			credentials: 'include',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ name })
		});
		const data = await response.json();
		if (response.ok || response.status === 409) return { id: data.body as string, name };
		return null;
	}

	async function searchRegions(q: string) {
		const url = q
			? `/api/regions/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Name`
			: '/api/regions/list?pageIndex=1&pageSize=20&properties=Id,Name';
		const r = await api.get<{ id: string; name: string }[]>(url);
		return r.body ?? [];
	}

	async function searchPersons(q: string) {
		const url = q
			? `/api/persons/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Name`
			: '/api/persons/list?pageIndex=1&pageSize=20&properties=Id,Name';
		const r = await api.get<{ id: string; name: string }[]>(url);
		return r.body ?? [];
	}

	async function searchOrgs(q: string) {
		const url = q
			? `/api/organisations/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Name`
			: '/api/organisations/list?pageIndex=1&pageSize=20&properties=Id,Name';
		const r = await api.get<{ id: string; name: string }[]>(url);
		return r.body ?? [];
	}

	async function searchJournals(q: string) {
		const url = q
			? `/api/journal/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Name`
			: '/api/journal/list?pageIndex=1&pageSize=20&properties=Id,Name';
		const r = await api.get<{ id: string; name: string }[]>(url);
		return r.body ?? [];
	}

	async function createPerson(name: string): Promise<{ id: string; name: string } | null> {
		const r = await api.put<string>('/api/persons/new', { name });
		return { id: r.body, name };
	}

	async function createOrg(name: string): Promise<{ id: string; name: string } | null> {
		const r = await api.put<string>('/api/organisations/new', { name });
		return { id: r.body, name };
	}

	async function createRegion(name: string): Promise<{ id: string; name: string } | null> {
		const r = await api.put<string>('/api/regions/new', { name });
		return { id: r.body, name };
	}

	async function createJournal(name: string): Promise<{ id: string; name: string } | null> {
		const r = await api.put<string>('/api/journal/new', { name });
		return { id: r.body, name };
	}

	let detailPromise = $derived(
		api.get<ResourceDetail>(
			`/api/resources/info/${item.id}?properties=${encodeURIComponent(PROPERTIES)}`
		)
	);
	let similarPromise = $derived(
		api.get<{ id: string; title: string; fileType: string }[]>(
			`/api/resources/${item.id}/relations/resource-similar-resources`
		)
	);
</script>

{#await detailPromise}
	<div class="flex justify-center py-8">
		<Spinner class="h-3 w-3" />
	</div>
{:then result}
	{@const detail = result.body}
	{@const sourceUrl = detail.sourceUrl ?? detail.url ?? ''}

	<!-- Characteristics -->
	<div class="flex flex-col gap-3 border-b px-3 py-3 text-sm">
		<CharacteristicRow
			icon={Layers}
			label="Resource Type"
			value={detail.resourceTypeName !== 'Unknown' ? detail.resourceTypeName : undefined}
		>
			{#snippet editContent()}
				<AsyncSelect
					value={detail.resourceTypeId ?? null}
					displayValue={detail.resourceTypeName !== 'Unknown'
						? (detail.resourceTypeName ?? null)
						: null}
					search={searchResourceTypes}
					placeholder="Resource Type"
					variant="ghost"
					allowClear={false}
					onchange={(id) =>
						registerSave(
							api.patch(`/api/resources/update/${item.id}`, { typeId: id }).then(() => {})
						)}
				/>
			{/snippet}
		</CharacteristicRow>
		<CharacteristicRow
			icon={Languages}
			label="Language"
			value={detail.languageCode ? formatLanguage(detail.languageCode) : undefined}
		>
			{#snippet editContent()}
				<AsyncSelect
					value={detail.languageCode ?? null}
					displayValue={detail.languageCode ? formatLanguage(detail.languageCode) : null}
					search={searchLanguages}
					placeholder="Language"
					variant="ghost"
					allowClear={false}
					onchange={(id) =>
						registerSave(
							api.patch(`/api/resources/update/${item.id}`, { languageCode: id }).then(() => {})
						)}
				/>
			{/snippet}
		</CharacteristicRow>
		<CharacteristicRow
			icon={Scale}
			label="License"
			value={detail.license}
			onsave={async (v) => {
				await api.patch(`/api/resources/update/${item.id}`, { license: v });
			}}
		/>
		<CharacteristicRow
			icon={FingerprintPattern}
			label="Publication Code"
			value={detail.publicationCode}
			onsave={async (v) => {
				await api.patch(`/api/resources/update/${item.id}`, { publicationCode: v });
			}}
		/>
		<CharacteristicRow icon={Newspaper} label="Journal" value={detail.journalName}>
			{#snippet editContent()}
				<AsyncSelect
					value={detail.journalId ?? null}
					displayValue={detail.journalName ?? null}
					search={searchJournals}
					oncreate={createJournal}
					placeholder="Journal"
					variant="ghost"
					onchange={(id) =>
						registerSave(
							api.patch(`/api/resources/update/${item.id}`, { journalId: id }).then(() => {})
						)}
				/>
			{/snippet}
		</CharacteristicRow>
		<CharacteristicRow
			icon={Link}
			label="Source URL"
			value={sourceUrl || undefined}
			href={sourceUrl || undefined}
			onsave={async (v) => {
				await api.patch(
					`/api/resources/update/${item.id}`,
					item.fileType === 'website' ? { url: v } : { sourceUrl: v }
				);
			}}
		/>
	</div>

	<!-- Tags + Regions -->
	<BadgeSection
		label="Tags"
		items={detail.tags}
		search={searchTags}
		onadd={async (id) => {
			await api.get(`/api/resources/${item.id}/relations/add/tags/${id}`);
		}}
		oncreate={createTag}
		onremove={async (rel) => {
			await api.get(`/api/resources/${item.id}/relations/remove/tags/${rel.id}`);
		}}
	/>
	<BadgeSection
		label="Regions"
		items={detail.regions}
		search={searchRegions}
		onadd={async (id) => {
			await api.get(`/api/resources/${item.id}/relations/add/regions/${id}`);
		}}
		oncreate={createRegion}
		onremove={async (rel) => {
			await api.get(`/api/resources/${item.id}/relations/remove/regions/${rel.id}`);
		}}
	/>

	<!-- Authors, Related People, Organisations -->
	<RelationSection
		label="Authors"
		items={detail.authors}
		search={searchPersons}
		onadd={async (id) => {
			await api.get(`/api/resources/${item.id}/relations/add/authors/${id}`);
		}}
		oncreate={createPerson}
		onremove={async (rel) => {
			await api.get(`/api/resources/${item.id}/relations/remove/authors/${rel.id}`);
		}}
	/>
	<RelationSection
		label="Related People"
		items={detail.relatedPersons}
		itemType="person"
		hasRole
		search={searchPersons}
		onadd={async (id) => {
			await api.get(`/api/resources/${item.id}/relations/add/related-persons/${id}`);
		}}
		oncreate={createPerson}
		onremove={async (rel) => {
			await api.get(`/api/resources/${item.id}/relations/remove/related-persons/${rel.id}`);
		}}
		onupdaterole={async (rel, role) => {
			await api.patch(
				`/api/resources/${item.id}/relations/update-role/related-persons/${rel.id}?newRole=${encodeURIComponent(role)}`
			);
		}}
	/>
	<RelationSection
		label="Organisations"
		items={detail.organisations}
		itemType="organisation"
		hasRole
		search={searchOrgs}
		onadd={async (id) => {
			await api.get(`/api/resources/${item.id}/relations/add/organisations/${id}`);
		}}
		oncreate={createOrg}
		onremove={async (rel) => {
			await api.get(`/api/resources/${item.id}/relations/remove/organisations/${rel.id}`);
		}}
		onupdaterole={async (rel, role) => {
			await api.patch(
				`/api/resources/${item.id}/relations/update-role/organisations/${rel.id}?newRole=${encodeURIComponent(role)}`
			);
		}}
	/>

	<!-- Text sections -->
	<TextSection
		label="Description"
		value={detail.description}
		onsave={async (description: string) => {
			await api.patch(`/api/resources/update/${item.id}`, { description });
		}}
	/>
	{#if item.fileType !== 'website'}
		<TextSection
			label="Abstract"
			value={detail.abstract}
			onsave={async (abstract: string) => {
				await api.patch(`/api/resources/update/${item.id}`, { abstract });
			}}
		/>
	{/if}
	<TextSection
		label="Notes"
		value={detail.note}
		onsave={async (note: string) => {
			await api.patch(`/api/resources/update/${item.id}`, { note });
		}}
	/>

	<!-- Similar Resources -->
	{#if !editMode}
		{#await similarPromise then result}
			<RelationSection
				label="Similar Resources"
				items={result.body.map((i) => ({ id: i.id, name: i.title, fileType: i.fileType }))}
				itemType="resource"
			/>
		{/await}
	{/if}
{/await}
