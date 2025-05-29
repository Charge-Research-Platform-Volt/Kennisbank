"use client"

import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import React from "react";
import { getProperties, getRelation } from "@/actions/right-sidebarActions";
import Skeleton from 'react-loading-skeleton';
import Expandable from "./expandable";
import BadgeList, { ListItem } from "./BadgeList";
import { useUserRole } from "@/context/user-role-context";
import { Button } from "@/components/ui/button";

export function OrganisationContent() 
{
    const { currentId, rightSidebarOpen } = useSidebar();
    const { userRole } = useUserRole();
    
    const [ name, setName ] = React.useState<string | null>(null);
    const [ url, setUrl ] = React.useState<string | undefined>(undefined);
    const [ description, setDescription ] = React.useState<string | null>(null);
    const [ resources, setResources ] = React.useState<ListItem[] | null>(null);
    const [ relatedResources, setRelatedResources ] = React.useState<ListItem[] | null>(null);
    const [ organisations, setOrganisations ] = React.useState<ListItem[] | null>(null);
    const [ persons, setPersons ] = React.useState<ListItem[] | null>(null);

    
    const [shouldRefresh, setUpdateTrigger] = React.useState(false);
    const triggerRefresh = () => setUpdateTrigger(prev => !prev);   

    
    
    const loadInformation = React.useCallback(async () =>
    {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.ORGANISATION);
        const resourcesPromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "direct-resources");
        const relatedResourcsePromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "related-resources");
        const organisationsPromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "organisation-related-organisations");
        const personsPromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "persons");

        infoPromise.then(response =>
        {
            setName(response.body.name);
            if (response.body.website)
            {
                setUrl(response.body.website);
            }
            else (setUrl("No Website"))
            if (response.body.description)
            {
                setDescription(response.body.description);
            }
            else { setDescription("No Description") }
        }).catch(error =>
        {
            console.error("Error loading organisation information:", error);
        })

        resourcesPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setResources(list);
        }).catch(error =>
        {
            console.error("Error loading resources: ", error);
        });

        relatedResourcsePromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setRelatedResources(list);
        }).catch(error =>
        {
            console.error("Error loading related resources: ", error);
        });


        organisationsPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { targetid: string; targetname: string, sourceid: string, sourcename: string }) => ({
                id: item.targetid === currentId ? item.sourceid : item.targetid,
                name: item.targetid === currentId ? item.sourcename : item.targetname,
                type: "organisation",
            }))
            setOrganisations(list);
        }).catch(error =>
        {
            console.error("Error loading related organisations: ", error);
        });

        personsPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "person",
            }))
            setPersons(list);
        }).catch(error =>
        {
            console.error("Error loading related persons: ", error);
        });

    }, [currentId]);
    
    React.useEffect(() => {
        if (rightSidebarOpen) {
            setName(null);
            setUrl(undefined);
            setDescription(null);
            setResources(null);
            setRelatedResources(null);
            setOrganisations(null);
            setPersons(null);
            loadInformation();
        }
    }, [currentId, shouldRefresh, loadInformation, rightSidebarOpen])

    return (
        <>
            <h1 className="pb-2 font-bold select-none text-2xl">{name || <Skeleton />}</h1>
            <a href={url} className="select-none" target="_blank" rel="noreferror">
                <h1 className="mb-8 select-none text-blue-500 underline">{url || <Skeleton />}</h1>
            </a>

            <Expandable title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Published">
                <BadgeList listType="direct-resources" itemList={resources} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related">
                <BadgeList listType="related-resources" itemList={relatedResources} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="organisation-related-organisations" itemList={organisations} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="persons" itemList={persons} onUpdate={triggerRefresh}/>
            </Expandable>
            
            { userRole === 'admin' &&
                <div className="w-full flex justify-center mt-10">
                    <Button variant="outline">Delete Person</Button>
                </div>
            }
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
