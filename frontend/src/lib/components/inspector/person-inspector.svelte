<script lang="ts">
	import type { ResourceItem, PersonDetail } from '$lib/types/resource';
	import { api } from '$lib/api';
	import Spinner from '../ui/spinner/spinner.svelte';
	import { BriefcaseBusiness, AtSign, Link } from '@lucide/svelte';
	import RelationSection from './relation-section.svelte';
	import TextSection from './text-section.svelte';
	import CharacteristicRow from './characteristic-row.svelte';
	import { getContext } from 'svelte';
	import type { PagedResult, ListItem } from '$lib/types/results';
	import AliasSection from './alias-section.svelte';

	let { item }: { item: ResourceItem } = $props();

	let detailPromise = $derived(api.get<PersonDetail>(`/api/persons/${item.id}`));

	const getEditMode = getContext<() => boolean>('getEditMode');
	let editMode = $derived(getEditMode());

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

	async function createPerson(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/persons', { name });
		return { id: r, name };
	}

	async function createOrg(name: string): Promise<ListItem | null> {
		const r = await api.put<string>('/api/organisations', { name });
		return { id: r, name };
	}

	async function searchResources(q: string) {
		const url = q
			? `/api/resources?page=1&pageSize=20&search=${encodeURIComponent(q)}`
			: '/api/resources?page=1&pageSize=20';
		const r = await api.get<PagedResult<{ id: string; title: string }>>(url);
		return r.items.map(i => ({ id: i.id, name: i.title }));
	}
</script>

{#await detailPromise}
	<div class="flex justify-center py-8">
		<Spinner class="h-3 w-3" />
	</div>
{:then result}
	{@const detail = result}

	<!-- Characteristics -->
	{#if detail.occupation || detail.emailAddress || detail.linkedin || editMode}
		<div class="flex flex-col gap-2 border-b px-3 py-3 text-sm">
			<CharacteristicRow
				icon={BriefcaseBusiness}
				label="Occupation"
				value={detail.occupation}
				onsave={async (v) => {
					await api.patch(`/api/persons/${item.id}`, { occupation: v });
				}}
			/>
			<CharacteristicRow
				icon={AtSign}
				label="Email"
				value={detail.emailAddress}
				href={detail.emailAddress ? `mailto:${detail.emailAddress}` : undefined}
				onsave={async (v) => {
					await api.patch(`/api/persons/${item.id}`, { emailAddress: v });
				}}
			/>
			<CharacteristicRow
				icon={Link}
				label="LinkedIn"
				value={detail.linkedin}
				href={detail.linkedin}
				onsave={async (v) => {
					await api.patch(`/api/persons/${item.id}`, { linkedin: v });
				}}
			/>
		</div>
	{/if}

	<!-- Aliases -->
	<AliasSection label="Aliases" value={detail.aliases} onsave={async (aliases) => { await api.patch(`/api/persons/${item.id}`, { aliases }); }} />

	<!-- Authored, Related Resources, Related People, Related Organisations -->
	<RelationSection
		label="Authored Resources"
		items={detail.authored}
		itemType="resource"
		search={searchResources}
		onadd={async (id) => {
			await api.post(`/api/persons/${item.id}/authored-resources/${id}`);
		}}
		onremove={async (rel) => {
			await api.delete(`/api/persons/${item.id}/authored-resources/${rel.id}`);
		}}
	/>
	<RelationSection
		label="Related Resources"
		items={detail.relatedResources}
		itemType="resource"
		hasRole
		search={searchResources}
		onadd={async (id) => {
			await api.post(`/api/persons/${item.id}/related-resources/${id}`);
		}}
		onremove={async (rel) => {
			await api.delete(`/api/persons/${item.id}/related-resources/${rel.id}`);
		}}
		onupdaterole={async (rel, role) => {
			await api.patch(
				`/api/persons/${item.id}/related-resources/${rel.id}/role?newRole=${encodeURIComponent(role)}`
			);
		}}
	/>
	<RelationSection
		label="Related People"
		items={[...detail.sourcePersons, ...detail.targetPersons]}
		itemType="person"
		hasRole
		search={searchPersons}
		onadd={async (id) => {
			await api.post(`/api/persons/${item.id}/related-persons/${id}`);
		}}
		oncreate={createPerson}
		onremove={async (rel) => {
			await api.delete(`/api/persons/${item.id}/related-persons/${rel.id}`);
		}}
		onupdaterole={async (rel, role) => {
			await api.patch(
				`/api/persons/${item.id}/related-persons/${rel.id}/role?newRole=${encodeURIComponent(role)}`
			);
		}}
	/>
	<RelationSection
		label="Related Organisations"
		items={detail.relatedOrganisations}
		itemType="organisation"
		hasRole
		search={searchOrgs}
		onadd={async (id) => {
			await api.post(`/api/persons/${item.id}/organisations/${id}`);
		}}
		oncreate={createOrg}
		onremove={async (rel) => {
			await api.delete(`/api/persons/${item.id}/organisations/${rel.id}`);
		}}
		onupdaterole={async (rel, role) => {
			await api.patch(
				`/api/persons/${item.id}/organisations/${rel.id}/role?newRole=${encodeURIComponent(role)}`
			);
		}}
	/>

	<!-- Description -->
	<TextSection
		label="Description"
		value={detail.description}
		onsave={async (description: string) => {
			await api.patch(`/api/persons/${item.id}`, { description });
		}}
	/>
{/await}
