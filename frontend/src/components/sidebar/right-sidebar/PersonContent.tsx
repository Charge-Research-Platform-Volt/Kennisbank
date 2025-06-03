"use client"

import React, {useState, useEffect} from "react";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import { getProperties, getRelation } from "@/actions/right-sidebarActions";
import Skeleton from 'react-loading-skeleton'
import Expandable from "./expandable"
import BadgeList, { ListItem } from "./BadgeList";
import Edit from "./Edit";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { TrashResource } from "@/actions/trashResourceActions";
import { useUserRole } from "@/context/user-role-context";
import { useArchive } from "@/context/archive-provider";
import { Button } from "@/components/ui/button";

export function PersonContent() 
{
    const { currentId, rightSidebarOpen, setRightSidebarOpen } = useSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload, trashOpen } = useArchive();
    const [confirmDialogOpen, setConfirmDialogOpen] = useState<boolean>(false);
    
    const [ name, setName ] = useState<string | null>(null);  
    const [ description, setDescription ] = useState<string | null>(null);
    const [ occupation, setOccupation ] = useState<string | null>(null);
    const [ authored, setAuthored ] = useState<ListItem[] | null>(null); 
    const [ related, setRelated ] = useState<ListItem[] | null>(null); 
    const [ persons, setPersons ] = useState<ListItem[] | null>(null); 
    const [ organisations, setOrganisations ] = useState<ListItem[] | null>(null); 
    const [ email, setEmail ] = useState<string | null>(null); //ToDo
    const [ linkedIn, setLinkedIn ] = useState<string | undefined>(undefined); //ToDo
    const [ creationDate, setCreationDate ] = useState<Date | null>(null); //ToDo

    const [authoredRefresh, setAuthoredTrigger] = useState(false);
    const triggerAuthoredRefresh = () => setAuthoredTrigger(prev => !prev);   
    const [relatedRefresh, setRelatedTrigger] = useState(false);
    const triggerRelatedRefresh = () => setRelatedTrigger(prev => !prev);   
    const [personsRefresh, setPersonsTrigger] = useState(false);
    const triggerPersonsRefresh = () => setPersonsTrigger(prev => !prev);   
    const [organistationsRefresh, setOrganisationsTrigger] = useState(false);
    const triggerOrganisationsRefresh = () => setOrganisationsTrigger(prev => !prev);

    useEffect(() => { if (rightSidebarOpen) { setAuthored(null); loadAuthored();  } }, [authoredRefresh]);
    useEffect(() => { if (rightSidebarOpen) { setRelated(null); loadRelated(); } }, [relatedRefresh]);
    useEffect(() => { if (rightSidebarOpen) { setPersons(null); loadPersons(); } }, [personsRefresh]);
    useEffect(() => { if (rightSidebarOpen) { setOrganisations(null); loadOrganisations(); } }, [organistationsRefresh]);

    
    React.useEffect(() => {
        if (rightSidebarOpen) {
            setName(null);
            setDescription(null);
            setOccupation(null);
            setAuthored(null);
            setRelated(null);
            setPersons(null);
            setOrganisations(null);
            setEmail(null);
            setLinkedIn(undefined);
            setCreationDate(null);

            loadProperties();
            loadAuthored();
            loadRelated();
            loadPersons();
            loadOrganisations();
        }
    }, [currentId])

    const loadAuthored = async () => {
        const authoredPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "authored-resources");

        authoredPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setAuthored(list);
        }).catch(error => {
            console.error("Error loading authored-resources: ", error);
        });
    }

    const loadRelated = async () => {
        const relatedPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "related-resources");

        relatedPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "resource",
            }))
            setRelated(list);
        }).catch(error => {
            console.error("Error loading related resource: ", error);
        });
    }

    const loadPersons = async () => {
        const personsPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "person-related-persons");

        personsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { targetid: any; targetname: any, sourceid: any, sourcename: any }) => ({
                id: item.targetid === currentId ? item.sourceid : item.targetid,
                name: item.targetid === currentId ? item.sourcename : item.targetname,
                type: "person",
            }))
            setPersons(list);
        }).catch(error => {
            console.error("Error loading related persons: ", error);
        });
    }

    const loadOrganisations = async () => {
        const organisationsPromise = getRelation(currentId, MetadataTypeEnum.PERSON, "organisations");

        organisationsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "organisation",
            }))
            setOrganisations(list);
        }).catch(error => {
            console.error("Error loading organisations: ", error);
        });
    }

    const loadProperties = async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.PERSON);
    
        infoPromise.then(response => {
            setName(response.body.name);
            if (response.body.description) {
                setDescription(response.body.description);
            }
            else {setDescription("No Description")}

            if (response.body.occupation) {
                setOccupation(response.body.occupation);
            }
            else {setOccupation("No Occupation")}

            setCreationDate(response.body.creationDate)

            if (response.body.emailAddress) {
                setEmail(response.body.emailAddress);
            }
            else {setEmail("No Email")}

            if (response.body.linkedin) {
                setLinkedIn(response.body.linkedin);
            }
            else {setLinkedIn("No LinkedIn")}

        }).catch(error => {
            console.error("Error loading person information: ", error);
        });  
    }
    
    const confirmDelete = async () => 
    {
        if (await TrashResource(currentId, MetadataTypeEnum.PERSON)) 
        {
            setRightSidebarOpen(false);
            triggerGridReload();
        }
    }

    return (
        <>
            <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />
        
            <h1 className="pb-2 font-bold select-none text-2xl">{name || <Skeleton />}</h1>
            {occupation ? (
            <div className="flex justify-between flex-1">
                <h2 className="mb-2 select-none">{occupation}</h2>
                <div className="flex justify-end"><Edit setNewText={setOccupation} currentText={occupation} property="occupation" /></div>
            </div>) : (<Skeleton />) }

            {linkedIn ? (
            <div className="flex justify-between flex-1">
                <h2 className="mb-2 select-none">{linkedIn}</h2>
                <div className="flex justify-end"><Edit setNewText={setLinkedIn} currentText={linkedIn} property="linkedIn" /></div>
            </div>) : (<Skeleton />) }

            
            <Expandable editButton={<Edit setNewText={setDescription} currentText={description} property="description" />} title="Description" collapsedHeight={100}>
                    {description || <Skeleton />}
            </Expandable>

            <Expandable variant="horizontal" title="Authored">
                <BadgeList listType="authored-resources" itemList={authored} onUpdate={triggerAuthoredRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related">
                <BadgeList listType="related-resources" itemList={related} onUpdate={triggerRelatedRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="person-related-persons" itemList={persons} onUpdate={triggerPersonsRefresh}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="organisations" itemList={organisations} onUpdate={triggerOrganisationsRefresh}/>
            </Expandable>

            <Expandable editButton={<Edit setNewText={setEmail} currentText={email} property="emailAddress" />} title="Email Address" collapsedHeight={100}>
                {email || <Skeleton />}
            </Expandable>
            
            { userRole === 'admin' && !trashOpen &&
                <div className="w-full flex justify-center mt-10">
                    <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:bg-red-50 hover:border-red-600 hover:text-red-600">Delete Person</Button>
                </div>
            }
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
