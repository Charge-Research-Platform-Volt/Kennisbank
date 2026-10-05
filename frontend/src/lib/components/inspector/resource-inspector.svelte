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
	import type { PagedResult, ListItem } from '$lib/types/results';

	const getEditMode = getContext<() => boolean>('getEditMode');
	let editMode = $derived(getEditMode());

	const registerSave = getContext<(p: Promise<void>) => void>('registerSave');

	let { item }: { item: ResourceItem } = $props();

	async function searchResourceTypes(q: string) {
		const url = q ? `/api/resource-types?search=${encodeURIComponent(q)}` : '/api/resource-types';
		const result = await api.get<PagedResult<ListItem>>(url);
		return result.items;
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

	async function searchPersons(q: string) {
		const url = q
			? `/api/persons?page=1&pageSize=20&search=${encodeURIComponent(q)}`
			: '/api/persons?page=1&pageSize=20';
		const r = await api.get<PagedResult<ListItem>>(url);
		return r.items ?? [];
	}

	async function searchOrgs(q: string) {
		const url = q
			? `/api/organisations?page=1&pageSize=20&search=${encodeURIComponent(q)}`
			: '/api/organisations?page=1&pageSize=20';
		const r = await api.get<PagedResult<ListItem>>(url);
		return r.items ?? [];
	}

	async function searchJournals(q: string) {
		const url = q
			? `/api/journals?page=1&pageSize=20&search=${encodeURIComponent(q)}`
			: '/api/journals?page=1&pageSize=20';
		const r = await api.get<PagedResult<ListItem>>(url);
		return r.items ?? [];
	}

	async function createPerson(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/persons', { name });
		return { id: r, name };
	}

	async function createOrg(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/organisations', { name });
		return { id: r, name };
	}

	async function createRegion(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/regions', { name });
		return { id: r, name };
	}

	async function createJournal(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/journals', { name });
		return { id: r, name };
	}

	let detailPromise = $derived(api.get<ResourceDetail>(`/api/resources/${item.id}`));
	let similarPromise = $derived(
		api.get<{ id: string; title: string; fileType: string }[]>(`/api/resources/${item.id}/similar`)
	);
</script>

{#await detailPromise}
	<div class="flex justify-center py-8">
		<Spinner class="h-3 w-3" />
	</div>
{:then result}
	{@const detail = result}
	{@const sourceUrl = detail.sourceUrl ?? ''}

	<!-- Characteristics -->
	<div class="flex flex-col gap-3 border-b px-3 py-3 text-sm">
		<CharacteristicRow
			icon={Layers}
			label="Resource Type"
			value={detail.typeName !== 'Unknown' ? detail.typeName : undefined}
			internalHref={detail.typeId
				? `/library?resourceTypes=${detail.typeId}&type=resource`
				: undefined}
		>
			{#snippet editContent()}
				<AsyncSelect
					value={detail.typeId ?? null}
					displayValue={detail.typeName !== 'Unknown' ? (detail.typeName ?? null) : null}
					search={searchResourceTypes}
					placeholder="Resource Type"
					variant="ghost"
					allowClear={false}
					onchange={(id) =>
						registerSave(api.patch(`/api/resources/${item.id}`, { typeId: id }).then(() => {}))}
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
							api.patch(`/api/resources/${item.id}`, { languageCode: id }).then(() => {})
						)}
				/>
			{/snippet}
		</CharacteristicRow>
		<CharacteristicRow
			icon={Scale}
			label="License"
			value={detail.license}
			onsave={async (v) => {
				await api.patch(`/api/resources/${item.id}`, { license: v });
			}}
		/>
		<CharacteristicRow
			icon={FingerprintPattern}
			label="Publication Code"
			value={detail.publicationCode}
			onsave={async (v) => {
				await api.patch(`/api/resources/${item.id}`, { publicationCode: v });
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
						registerSave(api.patch(`/api/resources/${item.id}`, { journalId: id }).then(() => {}))}
				/>
			{/snippet}
		</CharacteristicRow>
		<CharacteristicRow
			icon={Link}
			label="Source URL"
			value={sourceUrl || undefined}
			href={sourceUrl || undefined}
			onsave={async (v) => {
				await api.patch(`/api/resources/${item.id}`, { sourceUrl: v });
			}}
		/>
	</div>

	<!-- Tags + Regions -->
	<BadgeSection
		label="Tags"
		items={detail.tags}
		search={searchTags}
		onadd={async (id) => {
			await api.post(`/api/resources/${item.id}/tags/${id}`);
		}}
		oncreate={createTag}
		onremove={async (rel) => {
			await api.delete(`/api/resources/${item.id}/tags/${rel.id}`);
		}}
		itemHref={(item) => `/library?tags=${item.id}&type=resource`}
	/>
	<BadgeSection
		label="Regions"
		items={detail.regions}
		search={searchRegions}
		onadd={async (id) => {
			await api.post(`/api/resources/${item.id}/regions/${id}`);
		}}
		oncreate={createRegion}
		onremove={async (rel) => {
			await api.delete(`/api/resources/${item.id}/regions/${rel.id}`);
		}}
		itemHref={(item) => `/library?regions=${item.id}&type=resource`}
	/>

	<!-- Authors, Related People, Organisations -->
	<RelationSection
		label="Authors"
		items={detail.authors}
		search={searchPersons}
		onadd={async (id) => {
			await api.post(`/api/resources/${item.id}/authors/${id}`);
		}}
		oncreate={createPerson}
		onremove={async (rel) => {
			await api.delete(`/api/resources/${item.id}/authors/${rel.id}`);
		}}
	/>
	<RelationSection
		label="Related People"
		items={detail.relatedPersons}
		itemType="person"
		hasRole
		search={searchPersons}
		onadd={async (id) => {
			await api.post(`/api/resources/${item.id}/related-persons/${id}`);
		}}
		oncreate={createPerson}
		onremove={async (rel) => {
			await api.delete(`/api/resources/${item.id}/related-persons/${rel.id}`);
		}}
		onupdaterole={async (rel, role) => {
			await api.patch(
				`/api/resources/${item.id}/related-persons/${rel.id}/role?newRole=${encodeURIComponent(role)}`
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
			await api.post(`/api/resources/${item.id}/organisations/${id}`);
		}}
		oncreate={createOrg}
		onremove={async (rel) => {
			await api.delete(`/api/resources/${item.id}/organisations/${rel.id}`);
		}}
		onupdaterole={async (rel, role) => {
			await api.patch(
				`/api/resources/${item.id}/organisations/${rel.id}/role?newRole=${encodeURIComponent(role)}`
			);
		}}
	/>

	<!-- Text sections -->
	<TextSection
		label="Description"
		value={detail.description}
		onsave={async (description: string) => {
			await api.patch(`/api/resources/${item.id}`, { description });
		}}
	/>
	{#if item.fileType !== 'website'}
		<TextSection
			label="Abstract"
			value={detail.abstract}
			onsave={async (abstract: string) => {
				await api.patch(`/api/resources/${item.id}`, { abstract });
			}}
		/>
	{/if}
	<TextSection
		label="Notes"
		value={detail.note}
		onsave={async (note: string) => {
			await api.patch(`/api/resources/${item.id}`, { note });
		}}
	/>

	<!-- Similar Resources -->
	{#if !editMode}
		{#await similarPromise then result}
			<RelationSection
				label="Similar Resources"
				items={result.map((i) => ({ id: i.id, name: i.title, fileType: i.fileType }))}
				itemType="resource"
			/>
		{/await}
	{/if}
{/await}
