"use client"

import Expandable from "./expandable"
import BadgeList from "./BadgeList"
import { ListItem } from "./BadgeList"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import { getRelation } from "@/actions/right-sidebarActions"
import React, {useState, useEffect, useCallback} from "react"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'
import ResourceList from "./ResourceList"
import { Button } from "@/components/ui/button"
import { useUserRole } from "@/context/user-role-context"
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog"
import { ApiResponse } from "@/types/apiResponse.type"
import { useArchive } from "@/context/archive-provider"
import Edit from "./Edit";
import { TrashResource } from "@/actions/trashResourceActions"


export function ResourceContent()
{
    const { currentId, rightSidebarOpen, setRightSidebarOpen } = useSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload, trashOpen } = useArchive();
    const [confirmDialogOpen, setConfirmDialogOpen] = useState<boolean>(false);

    const [ fileType, setFileType] = useState<string | null>(null);
    const [ title, setTitle ] = useState<string | null>(null);
    const [ url, setUrl ] = useState<string | undefined>(undefined);
    const [ description, setDescription ] = useState<string | null>(null);
    const [ note, setNote ] = useState<string | null>(null);
    const [ authors, setAuthors ] = useState<ListItem[] | null>(null);
    const [ tags, setTags ] = useState<ListItem[] | null>(null);
    const [ aiTags, setAiTags ] = useState<ListItem[] | null>(null);
    const [ organisations, setOrganisations ] = useState<ListItem[] | null>(null);
    const [ relatedOrganisations, setRelatedOrganisations ] = useState<ListItem[] | null>(null);
    const [ relatedPersons, setRelatedPersons ] = useState<ListItem[] | null>(null);
    const [ relatedResources, setRelatedResources ] = useState<ListItem[] | null>(null);
    const [ sourceList, setSourceList ] = useState<ListItem[] | null>(null);
    const [ regions, setRegions ] = useState<ListItem[] | null>(null);
    const [ langCode, setLangCode ] = useState<string | null>(null); //ToDo
    const [ pubCode, setPubCode ] = useState<string | null>(null);
    const [ pubDate, setPubDate ] = useState<Date | null>(null); //ToDo
    const [ creationDate, setCreationDate ] = useState<Date | null>(null); //ToDo
    const [ license, setLicense ] = useState<string | null>(null);
    const [ trashed, setTrashed ] = useState<string | null>(null);

    
    const loadContent = useCallback(async () => 
    {
        const params: URLSearchParams = new URLSearchParams();
        
        params.append('properties', `
            Title,
            Description,
            LanguageCode,
            PublicationCode,
            PublicationDate,
            License,
            CreationDate,
            Note,
            FileType,
            Trashed,
            WebsiteMetadata.Url as Url,
            ResourceAuthorRelations.Select(new(Person.Id, Person.Name)) as Authors,
            ResourceOrganisationRelations.Select(new(Organisation.Id, Organisation.Name)) as Organisations,
            ResourceRegionRelations.Select(new(Region.Id, Region.Name)) as Regions,
            ResourceRelatedPersonRelations.Select(new(Person.Id, Person.Name)) as RelatedPersons,
            ResourceRelatedOrganisationRelations.Select(new(Organisation.Id, Organisation.Name)) as RelatedOrganisations,
            ResourceSourceRelations.Select(Url) as Sources,
            ResourceTagRelations.Select(new(Tag.Id, Tag.Name)) as Tags
        `);
    
        const response = await fetch(`/api/resources/info/${currentId}?${params.toString()}`,
            {
                method: 'GET',
                credentials: 'include'
            });
        
        if (response.ok) 
        {
            const data: ApiResponse = await response.json();
            
            setTitle(data.body.title || "Title missing.");
            setDescription(data.body.description || "No description.");
            setLangCode(data.body.languageCode || "Unknown.");
            setPubCode(data.body.publicationCode || "Unknown.");
            setPubDate(data.body.publicationDate || "Unknown");
            setCreationDate(data.body.creationDate || "Unknown.");
            setNote(data.body.note || "No notes.");
            setFileType(data.body.fileType || "Unknown.");
            setTrashed(data.body.trashed || "false");
            setUrl(data.body.url || "No URL found.");
            setAuthors(data.body.authors || []);
            setOrganisations(data.body.organisations || []);
            setRegions(data.body.regions || []);
            setRelatedPersons(data.body.relatedPersons || []);
            setRelatedOrganisations(data.body.relatedOrganisations || []);
            setSourceList(data.body.sources || []);
            setTags(data.body.tags || []);
            setLicense(data.body.license || "Unknown.");
        }
    }, [currentId]);
    
    const loadRelatedResources = useCallback(async () =>
    {
        const relatedResourcesPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "resource-related-resources");
    
        relatedResourcesPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { id: string; title: string; fileType: string }) => ({
                id: item.id,
                name: item.title,
                type: item.fileType,
            }))
            setRelatedResources(list);
        }).catch(error =>
        {
            console.error("Error loading related resources: ", error);
        });
    }, [currentId]);
    
    const loadAiTags = useCallback(async () =>
    {
        const aiTagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "ai-tags");
    
        aiTagsPromise.then(response =>
        {
            //Add translation logic from ai tags to ListItem[] here
            const list: ListItem[] = response.body.map((item: string) => ({
                id: item,
                name: item,
                type: "ai-tag",
            }))

            const finalList = list.length > 10
                ? list.sort(() => 0.5 - Math.random()).slice(0, 10)
                : list;

            setAiTags(finalList);
        }).catch(error =>
        {
            console.error("Error loading tags: ", error);
        });
    }, [currentId]);

    useEffect(() =>
    {
        if (rightSidebarOpen)
        {
            // Clear everything
            setFileType(null);
            setUrl(undefined);
            setTitle(null);
            setDescription(null);
            setNote(null);
            setAuthors(null);
            setTags(null);
            setAiTags(null);
            setOrganisations(null);
            setRelatedOrganisations(null);
            setRelatedPersons(null);
            setRelatedResources(null);
            setSourceList(null);
            setRegions(null);
            setLangCode(null);
            setPubCode(null);
            setPubDate(null);
            setCreationDate(null);
            setLicense(null);

            // Load content
            loadContent();
            loadRelatedResources();
            loadAiTags();
        }
    }, [currentId, loadContent, loadRelatedResources, loadAiTags, rightSidebarOpen]);
    
    const confirmDelete = async () => 
    {
        if (await TrashResource(currentId, MetadataTypeEnum.RESOURCE)) 
        {
            setRightSidebarOpen(false);
            triggerGridReload();
        }
    }

    return (
        <>
            <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />
        
            <h1 className="pb-2 font-bold select-none text-2xl">{title || <Skeleton />}</h1>
            
            { fileType === "website" &&
                <a href={url} className="select-none" target="_blank" rel="noreferror">
                    <h1 className="mb-8 select-none text-blue-500 underline">{url}</h1>
                </a>
            }
        
            <Expandable editButton={<Edit setNewText={setDescription} currentText={description} property="description" />} title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Tags">
                <BadgeList listType="tags" itemList={tags} onNew={(newItems) => setTags(tags ? tags.concat(newItems) : newItems)} onRemove={() => {}} />
            </Expandable>

            <Expandable variant="horizontal" title="Recommended Tags">
                <BadgeList listType="ai-tags" itemList={aiTags} onNew={() => {}} onRemove={() => {}} />
            </Expandable>

            <Expandable variant="horizontal" title="Authors">
                <BadgeList listType="authors" itemList={authors} onNew={(newItems) => setAuthors(authors ? authors.concat(newItems) : newItems)} onRemove={() => {}} />
            </Expandable>
            
            <Expandable variant="horizontal" title="Organisations">
                <BadgeList listType="organisations" itemList={organisations} onNew={(newItems) => setOrganisations(organisations ? organisations.concat(newItems) : newItems)} onRemove={() => {}} />
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="related-persons" itemList={relatedPersons} onNew={(newItems) => setRelatedPersons(relatedPersons ? relatedPersons.concat(newItems) : newItems)} onRemove={() => {}} />
            </Expandable>
            
            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="related-organisations" itemList={relatedOrganisations} onNew={(newItems) => setRelatedOrganisations(relatedOrganisations ? relatedOrganisations.concat(newItems) : newItems)} onRemove={() => {}} />
            </Expandable>

            <ResourceList header="Related Resources" resources={relatedResources}/>

            <Expandable variant="horizontal" title="Sources">
                <BadgeList listType="sources" itemList={sourceList} onNew={(newItems) => setSourceList(sourceList ? sourceList.concat(newItems) : newItems)} onRemove={() => {}} />
            </Expandable>

            <Expandable variant="horizontal" title="Regions">
                <BadgeList listType="regions" itemList={regions} onNew={(newItems) => setRegions(regions ? regions.concat(newItems) : newItems)} onRemove={() => {}} />
            </Expandable>

            {/* {resourceType === "Scientific Article" && (
                <Expandable title="Abstract" collapsedHeight={100}>
                    {<>insert abstract</> || <Skeleton />}
                </Expandable> 
            )} */}
            
            <Expandable editButton={<Edit setNewText={setNote} currentText={note} property="note" />} title="Notes" collapsedHeight={100}>
                {note || <Skeleton />}
            </Expandable>

            <Expandable editButton={<Edit setNewText={setPubCode} currentText={pubCode} property="publicationCode" />} title="Publication Code" collapsedHeight={100}>
                {pubCode || <Skeleton />}
            </Expandable>

            <Expandable editButton={<Edit setNewText={setLicense} currentText={license} property="license" />} title="License Code" collapsedHeight={100}>
                {license || <Skeleton />}
            </Expandable>

            { userRole === 'admin' && !trashOpen &&
                <div className="w-full flex justify-center mt-10">
                    <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:bg-red-50 hover:border-red-600 hover:text-red-600">Delete Resource</Button>
                </div>
            }
            
            
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
