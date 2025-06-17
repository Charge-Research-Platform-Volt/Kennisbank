"use client"

import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import React from "react";
import Skeleton from 'react-loading-skeleton';
import Expandable from "./expandable";
import BadgeList, { ListItem } from "./BadgeList";
import { useUserRole } from "@/context/user-role-context";
import { Button } from "@/components/ui/button";
import Edit from "./Edit";
import { useArchive } from "@/context/archive-provider";
import { TrashResource } from "@/actions/trashResourceActions";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { ApiResponse } from "@/types/apiResponse.type";
import { Badge } from "@/components/ui/badge";

export function OrganisationContent() 
{
    const { currentId, rightSidebarOpen, setRightSidebarOpen, setCreationDate, setPublicationDate } = useSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload, trashOpen } = useArchive();
    const [confirmDialogOpen, setConfirmDialogOpen] = React.useState<boolean>(false);
    
    const [ name, setName ] = React.useState<string | null>(null);
    const [ website, setWebsite ] = React.useState<string | undefined>(undefined);
    const [ description, setDescription ] = React.useState<string | null>(null);
    const [ resources, setResources ] = React.useState<ListItem[] | null>(null);
    const [ relatedResources, setRelatedResources ] = React.useState<ListItem[] | null>(null);
    const [ organisations, setOrganisations ] = React.useState<ListItem[] | null>(null);
    const [ persons, setPersons ] = React.useState<ListItem[] | null>(null);
    const [ email, setEmail ] = React.useState<string | null>(null);
    const [trashed, setTrashed] = React.useState<boolean>(false);
    
    // Loads all the content at once
    const loadContent = React.useCallback(async () => 
    {
        const params: URLSearchParams = new URLSearchParams();
        
        // Define the properties to select
        params.append('properties', `
            Name,
            Website,
            Description,
            EmailAddress as Email,
            Trashed,
            ResourceOrganisationRelations.Select(new(Resource.Id, Resource.Title as Name)) as Resources,
            ResourceRelatedOrganisationRelations.Select(new(Resource.Id, Resource.Title as Name)) as RelatedResources,
            TargetRelationships.Select(new(TargetOrganisation.Id, TargetOrganisation.Name)) as TargetOrganisations,
            SourceRelationships.Select(new(SourceOrganisation.Id, SourceOrganisation.Name)) as SourceOrganisations,
            PersonOrganisationRelations.Select(new(Person.Id, Person.Name)) as Persons
        `);
        
        // Fetch
        const response = await fetch(`/api/organisations/info/${currentId}?${params.toString()}`,
        {
            method: 'GET',
            credentials: 'include'
        });
        
        // Set all the properties if success
        if (response.ok) 
        {
            const data: ApiResponse = await response.json();
            
            setName(data.body.name || "Name missing.");
            setWebsite(data.body.website || "Website missing.");
            setDescription(data.body.description || "No description.");
            setEmail(data.body.email || "Unknown.");
            setTrashed(data.body.trashed || false);
            setResources(data.body.resources || []);
            setRelatedResources(data.body.relatedResources || []);
            setOrganisations(data.body.targetOrganisations.concat(data.body.sourceOrganisations) || []);
            setPersons(data.body.persons || []);
        }
        
    }, [currentId]);
    
    // Load new content if sidebar is opened
    React.useEffect(() => {
        if (rightSidebarOpen) 
        {
            // Clear content
            setName(null);
            setWebsite(undefined);
            setDescription(null);
            setResources(null);
            setRelatedResources(null);
            setOrganisations(null);
            setPersons(null);
            setEmail(null);
            setCreationDate(null);
            setPublicationDate(null);

            // Load content
            loadContent();
        }
    }, [currentId, loadContent, rightSidebarOpen, setCreationDate, setPublicationDate])
    
    // Delete the organisation
    const confirmDelete = async () => 
    {
        if (await TrashResource(currentId, MetadataTypeEnum.ORGANISATION)) 
        {
            setRightSidebarOpen(false);
            triggerGridReload();
        }
    }

    return (
        <>
            <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />

            {/* Banner for when organisation is in trash */}
            { trashed &&
                <Badge variant="outline" className="w-full mb-5 flex flex-col border-red-500 text-red-500">
                    <h1 className="text-xl">This item is in the trash.</h1>
                    <span className="flex-1 mb-1">Contact an admin if you think this is a mistake.</span>
                </Badge>
            }
            
            <h1 className="pb-2 font-bold select-none text-2xl">{name || <Skeleton />}</h1>
            {name ? (
                <div className="flex justify-between flex-1">
                    {website ? (
                        <a href={website} className="select-none" target="_blank" rel="noreferror">
                            <h1 className="mb-2 select-none text-blue-500 underline">{website}</h1>
                        </a>
                    ) : (
                        <h1 className="mb-2 select-none">No Website</h1>
                    )}
                    <div className="flex justify-end"><Edit setNewText={setWebsite} currentText={website ? (website) : ""} property="website" /></div>
                </div>) : (<Skeleton />) }

            <Expandable editButton={<Edit setNewText={setDescription} currentText={description} property="description" />} title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Published">
                <BadgeList listType="direct-resources" itemList={resources} onNew={(newItems) => setResources(resources ? resources.concat(newItems) : newItems)} onRemove={(removedItem) => setResources(resources ? resources.filter((item) => item != removedItem) : [])} />
            </Expandable>

            <Expandable variant="horizontal" title="Related">
                <BadgeList listType="related-resources" itemList={relatedResources} onNew={(newItems) => setRelatedResources(relatedResources ? relatedResources.concat(newItems) : newItems)} onRemove={(removedItem) => setRelatedResources(relatedResources ? relatedResources.filter((item) => item != removedItem) : [])} />
            </Expandable>

            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="organisation-related-organisations" itemList={organisations} onNew={(newItems) => setOrganisations(organisations ? organisations.concat(newItems) : newItems)} onRemove={(removedItem) => setOrganisations(organisations ? organisations.filter((item) => item != removedItem) : [])} />
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="persons" itemList={persons} onNew={(newItems) => setPersons(persons ? persons.concat(newItems) : newItems)} onRemove={(removedItem) => setPersons(persons ? persons.filter((item) => item != removedItem) : [])} />
            </Expandable>
            
            <Expandable editButton={<Edit setNewText={setEmail} currentText={email} property="emailAddress" />} title="Email Address" collapsedHeight={100}>
                {email || <Skeleton />}
            </Expandable>

            { userRole === 'admin' && !trashOpen &&
                <div className="w-full flex justify-center mt-10">
                    <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:bg-red-50 hover:border-red-600 hover:text-red-600">Delete Organisation</Button>
                </div>
            }
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
