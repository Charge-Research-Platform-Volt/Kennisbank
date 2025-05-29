"use client"

import React from "react";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import { getProperties, getRelation } from "@/actions/right-sidebarActions";
import Skeleton from 'react-loading-skeleton'
import Expandable from "./expandable"
import BadgeList, { ListItem } from "./BadgeList";

export function PersonContent() 
{
    const { currentId, rightSidebarOpen } = useSidebar();
    const [ name, setName ] = React.useState<string | null>(null);  
    const [ description, setDescription ] = React.useState<string | null>(null);
    const [ occupation, setOccupation ] = React.useState<string | null>(null);
    const [ authored, setAuthored ] = React.useState<ListItem[] | null>(null); 
    const [ related, setRelated ] = React.useState<ListItem[] | null>(null); 
    const [ persons, setPersons ] = React.useState<ListItem[] | null>(null); 
    const [ organisations, setOrganisations ] = React.useState<ListItem[] | null>(null); 
    const [linkedin, setLinkedin] = React.useState<string>('');

    const [shouldRefresh, setUpdateTrigger] = React.useState(false);
    const triggerRefresh = () => setUpdateTrigger(prev => !prev);   


    const loadInformation = React.useCallback(async () =>
    {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.PERSON);
        const authoredPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "authored-resources");
        const relatedPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "related-resources");
        const personsPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "person-related-persons");
        const organisationsPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "organisations");
    
        infoPromise.then(response =>
        {
            setName(response.body.name);
            if (response.body.description)
            {
                setDescription(response.body.description);
            }
            else { setDescription("No Description") }

            if (response.body.occupation)
            {
                setOccupation(response.body.occupation);
            }
            else { setOccupation("No Occupation") }
            
            if (response.body.linkedin)
                setLinkedin(response.body.linkedin)
            else
                setLinkedin('');
        }).catch(error =>
        {
            console.error("Error loading person information: ", error);
        });

        authoredPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setAuthored(list);
        }).catch(error =>
        {
            console.error("Error loading authored-resources: ", error);
        });

        relatedPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setRelated(list);
        }).catch(error =>
        {
            console.error("Error loading related resource: ", error);
        });

        personsPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { targetid: string; targetname: string, sourceid: string, sourcename: string }) => ({
                id: item.targetid === currentId ? item.sourceid : item.targetid,
                name: item.targetid === currentId ? item.sourcename : item.targetname,
                type: "person",
            }))
            setPersons(list);
        }).catch(error =>
        {
            console.error("Error loading related persons: ", error);
        });

        organisationsPromise.then(response =>
        {
            const list: ListItem[] = response.body.map((item: { id: string; name: string }) => ({
                id: item.id,
                name: item.name,
                type: "organisation",
            }))
            setOrganisations(list);
        }).catch(error =>
        {
            console.error("Error loading organisations: ", error);
        });
    }, [currentId]);
    
    React.useEffect(() => {
        if (rightSidebarOpen) {
            setName(null);
            setDescription(null);
            setOccupation(null);
            setAuthored(null);
            setRelated(null);
            setPersons(null);
            setOrganisations(null);
            loadInformation();
        }
    }, [currentId, shouldRefresh, loadInformation, rightSidebarOpen])

    return (
        <>
            <h1 className="pb-2 font-bold select-none text-2xl">{name || <Skeleton />}</h1>
            <h2 className="mb-2 select-none">{occupation || <Skeleton />}</h2>
            <a href={linkedin} className="select-none" target="_blank" rel="noreferror">
                <h1 className="mb-8 select-none text-blue-500 underline">{linkedin}</h1>
            </a>
            
            <Expandable title="Description" collapsedHeight={100}>
                    {description || <Skeleton />}
            </Expandable>

            <Expandable variant="horizontal" title="Authored">
                <BadgeList listType="authored-resources" itemList={authored} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related">
                <BadgeList listType="related-resources" itemList={related} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="person-related-persons" itemList={persons} onUpdate={triggerRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="organisations" itemList={organisations} onUpdate={triggerRefresh}/>
            </Expandable>
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
