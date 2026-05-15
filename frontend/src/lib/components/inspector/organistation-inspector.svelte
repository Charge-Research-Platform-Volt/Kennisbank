<script lang="ts">
    import type { ResourceItem, OrganisationDetail } from "$lib/types/resource";
    import { api } from "$lib/api";
    import Spinner from "../ui/spinner/spinner.svelte";
    import { AtSign, Link } from "lucide-svelte";
    import RelationSection from "./relation-section.svelte";
    import TextSection from "./text-section.svelte";
    import CharacteristicRow from "./characteristic-row.svelte";
    import { getContext } from "svelte";

    const PROPERTIES = 'Website,Description,CreationDate,EmailAddress as Email,ResourceAuthorRelations.Select(new(Resource.Id, Resource.Title as Name, Resource.FileType as FileType)) as Authored,ResourceOrganisationRelations.Select(new(Resource.Id, Resource.Title as Name, Resource.FileType as FileType, Role)) as RelatedResources,TargetRelationships.Select(new(TargetOrganisation.Id, TargetOrganisation.Name, Relation as relation)) as TargetOrganisations,SourceRelationships.Select(new(SourceOrganisation.Id, SourceOrganisation.Name, Relation as relation)) as SourceOrganisations,PersonOrganisationRelations.Select(new(Person.Id, Person.Name, Role)) as Persons';

    let { item }: { item: ResourceItem } = $props();

    let detailPromise = $derived(
        api.get<OrganisationDetail>(`/api/organisations/info/${item.id}?properties=${encodeURIComponent(PROPERTIES)}`)
    );

    const getEditMode = getContext<() => boolean>('getEditMode');
    let editMode = $derived(getEditMode());

    async function searchPersons(q: string) {
        const url = q ? `/api/persons/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Name` : '/api/persons/list?pageIndex=1&pageSize=20&properties=Id,Name';
        const r = await api.get<{ id: string; name: string }[]>(url);
        return r.body ?? [];
    }

    async function searchOrgs(q: string) {
        const url = q ? `/api/organisations/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Name` : '/api/organisations/list?pageIndex=1&pageSize=20&properties=Id,Name';
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

    async function searchResources(q: string) {
        const url = q ? `/api/resources/list?pageIndex=1&pageSize=20&searchQuery=${encodeURIComponent(q)}&properties=Id,Title as Name` : '/api/resources/list?pageIndex=1&pageSize=20&properties=Id,Title as Name';
        const r = await api.get<{ id: string; name: string }[]>(url);
        return r.body ?? [];
    }
</script>

{#await detailPromise}
    <div class="flex justify-center py-8">
        <Spinner class="h-3 w-3" />
    </div>
{:then result}
    {@const detail = result.body}

    <!-- Characteristics -->
    {#if detail.email || detail.website || editMode}
        <div class="flex flex-col gap-2 px-3 py-3 border-b text-sm">
            <CharacteristicRow icon={AtSign} label="Email" value={detail.email} href={detail.email ? `mailto:${detail.email}` : undefined} onsave={async (v) => { await api.patch(`/api/organisations/update/${item.id}`, { emailAddress: v }); }} />
            <CharacteristicRow icon={Link} label="Website" value={detail.website} href={detail.website} onsave={async (v) => { await api.patch(`/api/organisations/update/${item.id}`, { website: v }); }} />
        </div>
    {/if}

    <!-- Authored, Related Resources, Related Organisations, Related People -->
    <RelationSection
        label="Authored Resources"
        items={detail.authored}
        itemType="resource"
        search={searchResources}
        onadd={async (id) => { await api.get(`/api/organisations/${item.id}/relations/add/authored-resources/${id}`); }}
        onremove={async (rel) => { await api.get(`/api/organisations/${item.id}/relations/remove/authored-resources/${rel.id}`); }}
    />
    <RelationSection
        label="Related Resources"
        items={detail.relatedResources}
        itemType="resource"
        hasRole
        search={searchResources}
        onadd={async (id) => { await api.get(`/api/organisations/${item.id}/relations/add/related-resources/${id}`); }}
        onremove={async (rel) => { await api.get(`/api/organisations/${item.id}/relations/remove/related-resources/${rel.id}`); }}
        onupdaterole={async (rel, role) => { await api.patch(`/api/organisations/${item.id}/relations/update-role/related-resources/${rel.id}?newRole=${encodeURIComponent(role)}`); }}
    />
    <RelationSection
        label="Related Organisations"
        items={[...detail.sourceOrganisations, ...detail.targetOrganisations]}
        itemType="organisation"
        hasRole
        search={searchOrgs}
        onadd={async (id) => { await api.get(`/api/organisations/${item.id}/relations/add/related-organisations/${id}`); }}
        oncreate={createOrg}
        onremove={async (rel) => { await api.get(`/api/organisations/${item.id}/relations/remove/related-organisations/${rel.id}`); }}
        onupdaterole={async (rel, role) => { await api.patch(`/api/organisations/${item.id}/relations/update-role/related-organisations/${rel.id}?newRole=${encodeURIComponent(role)}`); }}
    />
    <RelationSection
        label="Related People"
        items={detail.persons}
        itemType="person"
        hasRole
        search={searchPersons}
        onadd={async (id) => { await api.get(`/api/organisations/${item.id}/relations/add/related-persons/${id}`); }}
        oncreate={createPerson}
        onremove={async (rel) => { await api.get(`/api/organisations/${item.id}/relations/remove/related-persons/${rel.id}`); }}
        onupdaterole={async (rel, role) => { await api.patch(`/api/organisations/${item.id}/relations/update-role/related-persons/${rel.id}?newRole=${encodeURIComponent(role)}`); }}
    />

    <!-- Description -->
    <TextSection label="Description" value={detail.description} onsave={async (description: string) => { await api.patch(`/api/organisations/update/${item.id}`, { description })}} />
{/await}
