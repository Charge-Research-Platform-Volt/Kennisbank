<script lang="ts">
    import type { ResourceItem, PersonDetail } from "$lib/types/resource";
    import { api } from "$lib/api";
    import Spinner from "../ui/spinner/spinner.svelte";
    import { BriefcaseBusiness, AtSign, Link } from "lucide-svelte";
    import RelationSection from "./relation-section.svelte";
    import TextSection from "./text-section.svelte";
    import CharacteristicRow from "./characteristic-row.svelte";

    const PROPERTIES = 'Name,Description,Occupation,EmailAddress as Email,Linkedin,Trashed,ResourceAuthorRelations.Select(new(Resource.Id, Resource.Title as Name, Resource.FileType as FileType)) as Authored,ResourceRelatedPersonRelations.Select(new(Resource.Id, Resource.Title as Name, Resource.FileType as FileType, Role)) as RelatedResources,TargetRelationships.Select(new(TargetPerson.Id, TargetPerson.Name, Relation as relation)) as TargetPersons,SourceRelationships.Select(new(SourcePerson.Id, SourcePerson.Name, Relation as relation)) as SourcePersons,PersonOrganisationRelations.Select(new(Organisation.Id, Organisation.Name, Role)) as RelatedOrganisations';

    let { item }: { item: ResourceItem } = $props();
    
    let detailPromise = $derived(
        api.get<PersonDetail>(`/api/persons/info/${item.id}?properties=${encodeURIComponent(PROPERTIES)}`)
    );
</script>

{#await detailPromise}
    <div class="flex justify-center py-8">
        <Spinner class="h-3 w-3" />
    </div>
{:then result}
    {@const detail = result.body}
    
    <!-- Characteristics -->
    {#if detail.occupation || detail.email || detail.linkedin}
        <div class="flex flex-col gap-2 px-3 py-3 border-b text-sm">
            <CharacteristicRow icon={BriefcaseBusiness} label="Occupation" value={detail.occupation} />
            <CharacteristicRow icon={AtSign} label="Email" value={detail.email} href={detail.email ? `mailto:${detail.email}` : undefined} />
            <CharacteristicRow icon={Link} label="LinkedIn" value={detail.linkedin} href={detail.linkedin} />
        </div>
    {/if}
    
    <!-- Authored, Related Resources, Related People, Related Organisations -->
    <RelationSection label="Authored Resources" items={detail.authored} itemType="resource" />
    <RelationSection label="Related Resources" items={detail.relatedResources} itemType="resource" />
    <RelationSection label="Related People" items={detail.sourcePersons.concat(detail.targetPersons)} itemType="person" />
    <RelationSection label="Related Organisations" items={detail.relatedOrganisations} itemType="organisation" />
    
    <!-- Description -->
    <TextSection label="Description" value={detail.description} />
{/await}
    