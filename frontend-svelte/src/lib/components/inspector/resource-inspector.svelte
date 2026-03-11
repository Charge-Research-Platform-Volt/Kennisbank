<script lang="ts">
    import type { ResourceItem, ResourceDetail, RelationItem } from "$lib/types/resource";
    import { api } from "$lib/api";
    import Spinner from "../ui/spinner/spinner.svelte";
    import * as Tooltip from "$lib/components/ui/tooltip";
    import { Layers, Languages, Scale, FingerprintPattern, Link } from "lucide-svelte";
    import { formatLanguage } from "$lib/utils/locale";
    import BadgeSection from "./badge-section.svelte";
    import RelationSection from "./relation-section.svelte";
    import TextSection from "./text-section.svelte";

    const PROPERTIES = 'Description,LanguageCode,PublicationCode,License,Note,Trashed,SourceUrl,WebsiteMetadata.Url as Url,DocumentMetadata.Abstract as Abstract,ResourceAuthorRelations.Select(new(Author.Id, Author.Name, Author.EntityType as AuthorType)) as Authors,ResourceOrganisationRelations.Select(new(Organisation.Id, Organisation.Name, Role)) as Organisations,ResourceRegionRelations.Select(new(Region.Id, Region.Name)) as Regions,ResourceRelatedPersonRelations.Select(new(Person.Id, Person.Name, Role)) as RelatedPersons,ResourceTagRelations.Select(new(Tag.Id, Tag.Name)) as Tags,ResourceType.Id as ResourceTypeId,ResourceType.Name as ResourceTypeName';
    type LucideIcon = typeof Layers;

    let { item }: { item: ResourceItem } = $props();

    let detailPromise = $derived(
        api.get<ResourceDetail>(`/api/resources/info/${item.id}?properties=${encodeURIComponent(PROPERTIES)}`)
    );
    let similarPromise = $derived(
        api.get<RelationItem[]>(`/api/resources/${item.id}/relations/resource-similar-resources?properties=${encodeURIComponent('Id as id, Title as name, FileType as fileType')}`)
    );
</script>

{#snippet characteristic(icon: LucideIcon, label: string, value: string, isUrl: boolean = false)}
    {@const Icon = icon}
    <div class="flex items-center gap-2 overflow-hidden">
        <Tooltip.Root>
            <Tooltip.Trigger>
                <Icon size={14} class="text-muted-foreground shrink-0" />
            </Tooltip.Trigger>
            <Tooltip.Content>{label}</Tooltip.Content>
        </Tooltip.Root>
        {#if isUrl}
            <a href={value} target="_blank" class="text-sm truncate hover:underline min-w-0">{value}</a>
        {:else}
            <span class="text-sm truncate min-w-0">{value}</span>
        {/if}
    </div>
{/snippet}

{#await detailPromise}
    <div class="flex justify-center py-8">
        <Spinner class="h-6 w-6" />
    </div>
{:then result}
    {@const detail = result.body}
    {@const sourceUrl = detail.sourceUrl ?? detail.url ?? ''}

    <!-- Characteristics -->
    <div class="flex flex-col gap-2 px-3 py-3 border-b text-sm">
        {#if detail.resourceTypeName}{@render characteristic(Layers, 'Resource Type', detail.resourceTypeName)}{/if}
        {#if detail.languageCode}{@render characteristic(Languages, 'Language', formatLanguage(detail.languageCode))}{/if}
        {#if detail.license}{@render characteristic(Scale, 'License', detail.license)}{/if}
        {#if detail.publicationCode}{@render characteristic(FingerprintPattern, 'Publication Code', detail.publicationCode)}{/if}
        {#if sourceUrl}{@render characteristic(Link, 'Source URL', sourceUrl, true)}{/if}
    </div>

    <!-- Tags + Regions -->
    <BadgeSection label="Tags" items={detail.tags} />
    <BadgeSection label="Regions" items={detail.regions} />

    <!-- Authors, Related People, Organisations -->
    <RelationSection label="Authors" items={detail.authors} />
    <RelationSection label="Related People" items={detail.relatedPersons} itemType="person" />
    <RelationSection label="Organisations" items={detail.organisations} itemType="organisation" />

    <!-- Text sections -->
    <TextSection label="Description" value={detail.description ?? ''} />
    <TextSection label="Abstract" value={detail.abstract ?? ''} />
    <TextSection label="Note" value={detail.note ?? ''} />

    <!-- Similar Resources -->
    {#await similarPromise then result}
        <RelationSection label="Similar Resources" items={result.body} itemType="resource" />
    {/await}
{/await}
