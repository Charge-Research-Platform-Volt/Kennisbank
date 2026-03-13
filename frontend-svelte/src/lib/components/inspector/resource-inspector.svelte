<script lang="ts">
    import type { ResourceItem, ResourceDetail, RelationItem } from "$lib/types/resource";
    import { api } from "$lib/api";
    import Spinner from "../ui/spinner/spinner.svelte";
    import { Layers, Languages, Scale, FingerprintPattern, Link } from "lucide-svelte";
    import { formatLanguage } from "$lib/utils/locale";
    import BadgeSection from "./badge-section.svelte";
    import RelationSection from "./relation-section.svelte";
    import TextSection from "./text-section.svelte";
    import CharacteristicRow from "./characteristic-row.svelte";

    const PROPERTIES = 'Description,LanguageCode,PublicationCode,License,Note,Trashed,SourceUrl,WebsiteMetadata.Url as Url,DocumentMetadata.Abstract as Abstract,ResourceAuthorRelations.Select(new(Author.Id, Author.Name, Author.EntityType as AuthorType)) as Authors,ResourceOrganisationRelations.Select(new(Organisation.Id, Organisation.Name, Role)) as Organisations,ResourceRegionRelations.Select(new(Region.Id, Region.Name)) as Regions,ResourceRelatedPersonRelations.Select(new(Person.Id, Person.Name, Role)) as RelatedPersons,ResourceTagRelations.Select(new(Tag.Id, Tag.Name)) as Tags,ResourceType.Id as ResourceTypeId,ResourceType.Name as ResourceTypeName';

    let { item }: { item: ResourceItem } = $props();

    let detailPromise = $derived(
        api.get<ResourceDetail>(`/api/resources/info/${item.id}?properties=${encodeURIComponent(PROPERTIES)}`)
    );
    let similarPromise = $derived(
        api.get<RelationItem[]>(`/api/resources/${item.id}/relations/resource-similar-resources?properties=${encodeURIComponent('Id as id, Title as name, FileType as fileType')}`)
    );
</script>

{#await detailPromise}
    <div class="flex justify-center py-8">
        <Spinner class="h-6 w-6" />
    </div>
{:then result}
    {@const detail = result.body}
    {@const sourceUrl = detail.sourceUrl ?? detail.url ?? ''}

    <!-- Characteristics -->
    <div class="flex flex-col gap-2 px-3 py-3 border-b text-sm">
        <CharacteristicRow icon={Layers} label="Resource Type" value={detail.resourceTypeName !== 'Unknown' ? detail.resourceTypeName : undefined} />
        <CharacteristicRow icon={Languages} label="Language" value={detail.languageCode ? formatLanguage(detail.languageCode) : undefined} />
        <CharacteristicRow icon={Scale} label="License" value={detail.license} />
        <CharacteristicRow icon={FingerprintPattern} label="Publication Code" value={detail.publicationCode} />
        <CharacteristicRow icon={Link} label="Source URL" value={sourceUrl || undefined} href={sourceUrl || undefined} />
    </div>

    <!-- Tags + Regions -->
    <BadgeSection label="Tags" items={detail.tags} />
    <BadgeSection label="Regions" items={detail.regions} />

    <!-- Authors, Related People, Organisations -->
    <RelationSection label="Authors" items={detail.authors} />
    <RelationSection label="Related People" items={detail.relatedPersons} itemType="person" />
    <RelationSection label="Organisations" items={detail.organisations} itemType="organisation" />

    <!-- Text sections -->
    <TextSection label="Description" value={detail.description} />
    <TextSection label="Abstract" value={detail.abstract} />
    <TextSection label="Note" value={detail.note} />

    <!-- Similar Resources -->
    {#await similarPromise then result}
        <RelationSection label="Similar Resources" items={result.body} itemType="resource" />
    {/await}
{/await}
