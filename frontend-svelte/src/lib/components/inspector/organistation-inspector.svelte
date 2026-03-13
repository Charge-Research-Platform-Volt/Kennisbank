<script lang="ts">
    import type { ResourceItem, OrganisationDetail } from "$lib/types/resource";
    import { api } from "$lib/api";
    import Spinner from "../ui/spinner/spinner.svelte";
    import { AtSign, Link } from "lucide-svelte";
    import RelationSection from "./relation-section.svelte";
    import TextSection from "./text-section.svelte";
    import CharacteristicRow from "./characteristic-row.svelte";

    const PROPERTIES = 'Name,Website,Description,CreationDate,EmailAddress as Email,Trashed,ResourceAuthorRelations.Select(new(Resource.Id, Resource.Title as Name, Resource.FileType as FileType)) as Authored,ResourceOrganisationRelations.Select(new(Resource.Id, Resource.Title as Name, Resource.FileType as FileType, Role)) as RelatedResources,TargetRelationships.Select(new(TargetOrganisation.Id, TargetOrganisation.Name, Relation as relation)) as TargetOrganisations,SourceRelationships.Select(new(SourceOrganisation.Id, SourceOrganisation.Name, Relation as relation)) as SourceOrganisations,PersonOrganisationRelations.Select(new(Person.Id, Person.Name, Role)) as Persons';

    let { item }: { item: ResourceItem } = $props();
    
    let detailPromise = $derived(
        api.get<OrganisationDetail>(`/api/organisations/info/${item.id}?properties=${encodeURIComponent(PROPERTIES)}`)
    );
</script>

{#await detailPromise}
    <div class="flex justify-center py-8">
        <Spinner class="h-3 w-3" />
    </div>
{:then result}
    {@const detail = result.body}
    
    <!-- Characteristics -->
    {#if detail.email || detail.website}
        <div class="flex flex-col gap-2 px-3 py-3 border-b text-sm">
            <CharacteristicRow icon={AtSign} label="Email" value={detail.email} href={detail.email ? `mailto:${detail.email}` : undefined} />
            <CharacteristicRow icon={Link} label="Website" value={detail.website} href={detail.website} />
        </div>
    {/if}
    
    <!-- Authored, Related Resources, Related Organistaions, Related People -->
    <RelationSection label="Authored Resources" items={detail.authored} itemType="resource" />
    <RelationSection label="Related Resources" items={detail.relatedResources} itemType="resource" />
    <RelationSection label="Related Organisations" items={detail.sourceOrganisations.concat(detail.targetOrganisations)} itemType="organisation" />
    <RelationSection label="Related People" items={detail.persons} itemType="person" />

    <!-- Description -->
    <TextSection label="Description" value={detail.description} />
{/await}