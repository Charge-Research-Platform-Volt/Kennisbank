"use client"

import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import React, {useState, useEffect} from "react";
import { getProperties, getRelation } from "@/actions/right-sidebarActions";
import Skeleton from 'react-loading-skeleton';
import Expandable from "./expandable";
import BadgeList, { ListItem } from "./BadgeList";
import { useUserRole } from "@/context/user-role-context";
import { Button } from "@/components/ui/button";
import Edit from "./Edit";

export function OrganisationContent() 
{
    const { currentId, rightSidebarOpen } = useSidebar();
    const { userRole } = useUserRole();
    const [ name, setName ] = useState<string | null>(null);
    const [ url, setUrl ] = useState<string | undefined>(undefined);
    const [ description, setDescription ] = useState<string | null>(null);
    const [ resources, setResources ] = useState<ListItem[] | null>(null);
    const [ relatedResources, setRelatedResources ] = useState<ListItem[] | null>(null);
    const [ organisations, setOrganisations ] = useState<ListItem[] | null>(null);
    const [ persons, setPersons ] = useState<ListItem[] | null>(null);
    const [ email, setEmail ] = useState<string | null>(null); //ToDo
    const [ creationDate, setCreationDate ] = useState<Date | null>(null); //ToDo

    
    const [resourcesRefresh, setResourcesTrigger] = useState(false);
    const triggerResourcesRefresh = () => setResourcesTrigger(prev => !prev);   
    const [relatedResourcesRefresh, setRelatedResourcesTrigger] = useState(false);
    const triggerRelatedResourcesRefresh = () => setRelatedResourcesTrigger(prev => !prev);   
    const [organistationsRefresh, setOrganisationsTrigger] = useState(false);
    const triggerOrganisationsRefresh = () => setOrganisationsTrigger(prev => !prev);
    const [personsRefresh, setPersonsTrigger] = useState(false);
    const triggerPersonsRefresh = () => setPersonsTrigger(prev => !prev); 

    
    React.useEffect(() => {
        if (rightSidebarOpen) {
            setName(null);
            setUrl(undefined);
            setDescription(null);
            setResources(null);
            setRelatedResources(null);
            setOrganisations(null);
            setPersons(null);
            setEmail(null);
            setCreationDate(null);

            loadProperties();
            loadResources();
            loadRelatedResources();
            loadOrganisations();
            loadPersons();
        }
    }, [currentId])

    useEffect(() => { if (rightSidebarOpen) { setResources(null); loadResources();  } }, [resourcesRefresh]);
    useEffect(() => { if (rightSidebarOpen) { setRelatedResources(null); loadRelatedResources(); } }, [relatedResourcesRefresh]);
    useEffect(() => { if (rightSidebarOpen) { setOrganisations(null); loadOrganisations(); } }, [organistationsRefresh]);
    useEffect(() => { if (rightSidebarOpen) { setPersons(null); loadPersons(); } }, [personsRefresh]);
    
    const loadResources = async () => {
        const resourcesPromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "direct-resources");

        resourcesPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setResources(list);
        }).catch(error => {
            console.error("Error loading resources: ", error);
        });
    }

    const loadRelatedResources = async () => {
        const relatedResourcsePromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "related-resources");
        
        relatedResourcsePromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setRelatedResources(list);
        }).catch(error => {
            console.error("Error loading related resources: ", error);
        });
    }

    const loadOrganisations = async () => {
        const organisationsPromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "organisation-related-organisations");
        
        organisationsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { targetid: any; targetname: any, sourceid: any, sourcename: any }) => ({
                id: item.targetid === currentId ? item.sourceid : item.targetid,
                name: item.targetid === currentId ? item.sourcename : item.targetname,
                type: "organisation",
            }))
            setOrganisations(list);
        }).catch(error => {
            console.error("Error loading related organisations: ", error);
        });
    }

    const loadPersons = async () => {
        const personsPromise = getRelation(currentId, MetadataTypeEnum.ORGANISATION, "persons");
     
        personsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "person",
            }))
            setPersons(list);
        }).catch(error => {
            console.error("Error loading related persons: ", error);
        });
    }

    const loadProperties = async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.ORGANISATION);

        infoPromise.then(response => {
            setName(response.body.name);
            if (response.body.website) {
                setUrl(response.body.website);
            }
            else( setUrl("No Website"))
            if (response.body.description) {
                setDescription(response.body.description);
            }
            else {setDescription("No Description")}
            if (response.body.emailAddress) {
                setEmail(response.body.emailAddress);
            }
            else { setEmail("No Email")}

            setCreationDate(response.body.creationDate);

        }).catch(error => {
            console.error("Error loading organisation information:", error);
        })
    }

    return (
        <>
            <h1 className="pb-2 font-bold select-none text-2xl">{name || <Skeleton />}</h1>
            <a href={url} className="select-none" target="_blank" rel="noreferror">
                <h1 className="mb-8 select-none text-blue-500 underline">{url || <Skeleton />}</h1>
            </a>

            {url ? (
                <div className="flex justify-between flex-1">
                    <h2 className="mb-2 select-none">{url}</h2>
                    <div className="flex justify-end"><Edit setNewText={setUrl} currentText={url} property="url" /></div>
                </div>) : (<Skeleton />) }

            <Expandable editButton={<Edit setNewText={setDescription} currentText={description} property="description" />} title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Published">
                <BadgeList listType="direct-resources" itemList={resources} onUpdate={triggerResourcesRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related">
                <BadgeList listType="related-resources" itemList={relatedResources} onUpdate={triggerRelatedResourcesRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="organisation-related-organisations" itemList={organisations} onUpdate={triggerOrganisationsRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="persons" itemList={persons} onUpdate={triggerPersonsRefresh}/>
            </Expandable>
            
            { userRole === 'admin' &&
                <div className="w-full flex justify-center mt-10">
                    <Button variant="outline">Delete Organisation</Button>
                </div>
            }
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
