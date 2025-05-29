"use client"

import Expandable from "./expandable"
import BadgeList from "./BadgeList"
import { ListItem } from "./BadgeList"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import { getProperties, getRelation } from "@/actions/right-sidebarActions"
import React from "react"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'
import ResourceList from "./ResourceList"
import { Button } from "@/components/ui/button"
import { useUserRole } from "@/context/user-role-context"
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog"
import { toast } from "sonner"
import { ApiResponse } from "@/types/apiResponse.type"
import { useArchive } from "@/context/archive-provider"


export function ResourceContent()
{
    const { currentId, rightSidebarOpen, setRightSidebarOpen } = useSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload } = useArchive();
    
    const [ fileType, setFileType] = React.useState<string | null>(null);
    const [ title, setTitle ] = React.useState<string | null>(null);
    const [ url, setUrl ] = React.useState<string | undefined>(undefined);
    const [ description, setDescription ] = React.useState<string | null>(null);
    const [ note, setNote ] = React.useState<string | null>(null);
    const [ authors, setAuthors ] = React.useState<ListItem[] | null>(null);
    const [ tags, setTags ] = React.useState<ListItem[] | null>(null);
    const [ organisations, setOrganisations ] = React.useState<ListItem[] | null>(null);
    const [ relatedOrganisations, setRelatedOrganisations ] = React.useState<ListItem[] | null>(null);
    const [ relatedPersons, setRelatedPersons ] = React.useState<ListItem[] | null>(null);
    const [ relatedResources, setRelatedResources ] = React.useState<ListItem[] | null>(null);
    const [ sourceList, setSourceList ] = React.useState<ListItem[] | null>(null);
    const [ regions, setRegions ] = React.useState<ListItem[] | null>(null);

    const [shouldRefresh, setUpdateTrigger] = React.useState(false);
    const triggerRefresh = () => setUpdateTrigger(prev => !prev);  

    const [confirmDialogOpen, setConfirmDialogOpen] = React.useState<boolean>(false);

    

    const loadInformation = React.useCallback(async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.RESOURCE);
        const authorsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "authors");
        const tagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "tags");
        const organisationsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "organisations");
        const relatedOrganisationsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "related-organisations");
        const relatedPersonsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "related-persons");
        const relatedResourcesPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "resource-related-resources");
        const sourceListPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "sources");
        const regionsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "regions");
        // const relatedSourceListPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "related-sources");

        infoPromise.then(response => {
            setFileType(response.body.fileType);
            if (response.body.fileType === "website") {
                const websitePromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "website");
                
                websitePromise.then(response => {
                    setUrl(response.body.url);
                }).catch(error => {console.error("Error loading url: ", error);})
            }
            setTitle(response.body.title);
            if (response.body.description) {
                setDescription(response.body.description);
            }
            else {setDescription("No description.")}
            
            if (response.body.note) {
                setNote(response.body.note);
            }
            else {setNote("No notes.")}

        }).catch(error => {
            console.error("Error loading information: ", error);
        });
        

        authorsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "person",
            }))
            setAuthors(list);
        }).catch(error => {
            console.error("Error loading authors: ", error);
        });

        tagsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "tag",
            }))
            setTags(list);
        }).catch(error => {
            console.error("Error loading tags: ", error);
        });

        organisationsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "organisation",
            }))
            setOrganisations(list);
        }).catch(error => {
            console.error("Error loading organisations: ", error);
        });

        relatedPersonsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "person",
            }))
            setRelatedPersons(list);
        }).catch(error => {
            console.error("Error loading related persons: ", error);
        });
        
        relatedOrganisationsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "organisation",
            }))
            setRelatedOrganisations(list);
        }).catch(error => {
            console.error("Error loading related organisations: ", error);
        });

        relatedResourcesPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: string; title: string; fileType: string }) => ({
                id: item.id,
                name: item.title,
                type: item.fileType,
            }))
            setRelatedResources(list);
        }).catch(error => {
            console.error("Error loading related resources: ", error);
        });

        sourceListPromise.then(response => {
            const list: ListItem[] = response.body.map((item: {id: string; name: string}) => ({
                id: decodeURIComponent(item.id),
                name: decodeURIComponent(item.name),
                type: "source",
            }))
            setSourceList(list);
        }).catch(error => {
            console.error("Error loading sources: ", error);
        });

        regionsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: {id: string; name: string}) => ({
                id: item.id,
                name: item.name,
                type: "region",
            }))
            setRegions(list);
        }).catch(error => {
            console.error("Error loading regions: ", error);
        });
        
        // relatedSourceListPromise.then(response => {
        //     const list: ListItem[] = response.body.map((item: {id: string; name: string}) => ({
        //         id: decodeURIComponent(item.id),
        //         name: decodeURIComponent(item.name),
        //         type: "source",
        //     }))
        //     setRelatedSourceList(list);
        // }).catch(error => {
        //     console.error("Error loading sources: ", error);
        // });

    }, [currentId])
    
    React.useEffect(() => {
        if (rightSidebarOpen) {
        setFileType(null);
        setUrl(undefined);
        setTitle(null);
        setDescription(null);
        setNote(null);
        setAuthors(null);
        setTags(null);
        setOrganisations(null);
        setRelatedOrganisations(null);
        setRelatedPersons(null);
        setRelatedResources(null);
        setSourceList(null);
        setRegions(null);
        loadInformation(); }
    }, [currentId, shouldRefresh, loadInformation, rightSidebarOpen])
    
    const confirmDelete = async () => 
    {
        const response = await fetch(`/api/resources/trash/${currentId}`, { method: 'PATCH', credentials: 'include' });
        
        if (!response.ok) 
        {
            toast.error("Error Deleting Resource");
            return;
        }
        
        const data: ApiResponse = await response.json();
        
        if (data.success) 
        {
            setRightSidebarOpen(false);
            triggerGridReload();
        }
        else 
        {
            toast.error("Error Deleting Resource");
            console.error(data.message);
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
        
            <Expandable title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Tags">
                <BadgeList listType="tags" itemList={tags} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Authors">
                <BadgeList listType="authors" itemList={authors} onUpdate={triggerRefresh}/>
            </Expandable>
            
            <Expandable variant="horizontal" title="Organisations">
                <BadgeList listType="organisations" itemList={organisations} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="related-persons" itemList={relatedPersons} onUpdate={triggerRefresh}/>
            </Expandable>
            
            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="related-organisations" itemList={relatedOrganisations} onUpdate={triggerRefresh}/>
            </Expandable>

            <ResourceList header="Related Resources" resources={relatedResources}/>

            <Expandable variant="horizontal" title="Sources">
                <BadgeList listType="sources" itemList={sourceList} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Regions">
                <BadgeList listType="regions" itemList={regions} onUpdate={triggerRefresh}/>
            </Expandable>

            {/* {resourceType === "Scientific Article" && (
                <Expandable title="Abstract" collapsedHeight={100}>
                    {<>insert abstract</> || <Skeleton />}
                </Expandable> 
            )} */}
            
            <Expandable title="Notes" collapsedHeight={100}>
                {note || <Skeleton />}
            </Expandable>

            {/* <Expandable variant="horizontal" title="Related Sources">
                <BadgeList listType="related-sources" emptyMessage={"No sources recorded"} itemList={relatedSourceList} onUpdate={triggerRefresh}/>
            </Expandable> */}
            
            { userRole === 'admin' &&
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
