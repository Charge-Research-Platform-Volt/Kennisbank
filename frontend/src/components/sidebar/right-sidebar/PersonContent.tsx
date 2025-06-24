"use client"

import React from "react";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import Skeleton from 'react-loading-skeleton'
import Expandable from "./expandable"
import BadgeList, { ListItem } from "./BadgeList";
import Edit from "./Edit";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { TrashResource } from "@/actions/trashResourceActions";
import { useUserRole } from "@/context/user-role-context";
import { useArchive } from "@/context/archive-provider";
import { Button } from "@/components/ui/button";
import { ApiResponse } from "@/types/apiResponse.type";
import { Badge } from "@/components/ui/badge";
import PersonIcon from "@/icons/person-icon";

export function PersonContent() 
{
    const { currentId, rightSidebarOpen, setRightSidebarOpen, setCreationDate, setPublicationDate, setEditMode, editMode  } = useSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload, trashOpen } = useArchive();
    const [confirmDialogOpen, setConfirmDialogOpen] = React.useState<boolean>(false);
    
    const [ name, setName ] = React.useState<string | null>(null);  
    const [ description, setDescription ] = React.useState<string | null>(null);
    const [ occupation, setOccupation ] = React.useState<string | null>(null);
    const [ email, setEmail ] = React.useState<string | null>(null);
    const [ linkedIn, setLinkedIn ] = React.useState<string | undefined>(undefined);
    const [ authored, setAuthored ] = React.useState<ListItem[] | null>(null); 
    const [ related, setRelated ] = React.useState<ListItem[] | null>(null); 
    const [ persons, setPersons ] = React.useState<ListItem[] | null>(null); 
    const [ organisations, setOrganisations ] = React.useState<ListItem[] | null>(null);
    const [trashed, setTrashed] = React.useState<boolean>(false);

    // Loads all content at once
    const loadContent = React.useCallback(async () => 
    {
        const params: URLSearchParams = new URLSearchParams();
        
        // Define properties to retrieve
        params.append('properties', `
            Name,
            Description,
            Occupation,
            CreationDate,
            EmailAddress as Email,
            Linkedin,
            Trashed,
            ResourceAuthorRelations.Select(new(Resource.Id, Resource.Title as Name)) as Authored,
            ResourceRelatedPersonRelations.Select(new(Resource.Id, Resource.Title as Name)) as Related,
            TargetRelationships.Select(new(TargetPerson.Id, TargetPerson.Name)) as TargetPersons,
            SourceRelationships.Select(new(SourcePerson.Id, SourcePerson.Name)) as SourcePersons,
            PersonOrganisationRelations.Select(new(Organisation.Id, Organisation.Name)) as Organisations
        `);
        
        // Fetch
        const response = await fetch(`/api/persons/info/${currentId}?${params.toString()}`,
        {
            method: 'GET',
            credentials: 'include'
        });
        
        // Set states on success
        if (response.ok) 
        {
            const data: ApiResponse = await response.json();
            
            setName(data.body.name || "Name missing.");
            setDescription(data.body.description || "No description.");
            setOccupation(data.body.occupation || "Occupation unknown.");
            setCreationDate(data.body.creationDate || "Unknown.");
            setEmail(data.body.email || "Email unknown.");
            setLinkedIn(data.body.linkedin || "LinkedIn unknown.");
            setTrashed(data.body.trashed || false);
            setAuthored(data.body.authored || []);
            setRelated(data.body.related || []);
            setPersons(data.body.targetPersons.concat(data.body.sourcePersons) || []);
            setOrganisations(data.body.organisations || []);
        }
    }, [currentId])

    // Reload content on sidebar open
    React.useEffect(() =>
    {
        if (rightSidebarOpen)
        {
            // Clear content
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
            setPublicationDate(null);

            // Load content
            loadContent();
        }
    }, [loadContent, rightSidebarOpen, setCreationDate, setPublicationDate]);
    
    // Delete the person
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

            {/* Banner for when person is in trash */}
            { trashed &&
                <Badge variant="outline" className="w-full mb-5 flex flex-col border-red-500 text-red-500">
                    <h1 className="text-xl">This item is in the trash.</h1>
                    <span className="flex-1 mb-1">Contact an admin if you think this is a mistake.</span>
                </Badge>
            }
            
            <div className="flex justify-start gap-2 items-center pb-2">
                <PersonIcon className="w-5 h-5" />
                {editMode && (  <div className="mt-1 text-sm flex justify-center select-none">
                                                <Edit setNewText={setName} currentText={name} property="name" />
                                            </div>)}
            <h1 className="font-bold select-none text-2xl">{name || <Skeleton />}</h1>
            </div>
            
            {occupation ? (
                <div className="flex justify-between flex-1">
                    <h2 className="mb-2 select-none">{occupation}</h2>
                    <div className="flex justify-end"><Edit setNewText={setOccupation} currentText={occupation} property="occupation" /></div>
                </div>
            ) : (<Skeleton />) }

            {linkedIn ? (
                <div className="flex justify-between flex-1">
                    <h2 className="mb-2 select-none">{linkedIn}</h2>
                    <div className="flex justify-end"><Edit setNewText={setLinkedIn} currentText={linkedIn} property="linkedIn" /></div>
                </div>
            ) : (<Skeleton />) }

            
            <Expandable title="Description" collapsedHeight={100}>
                    {editMode && (  <div className="mt-1 text-sm flex justify-center select-none">
                                        <Edit setNewText={setDescription} currentText={description} property="description" />
                                    </div>)}
                    {description || <Skeleton />}
            </Expandable>

            <Expandable variant="horizontal" title="Authored">
                <BadgeList listType="authored-resources" itemList={authored} onNew={(newItems) => setAuthored(authored ? authored.concat(newItems) : newItems)} onRemove={(removedItem) => setAuthored(authored ? authored.filter((item) => item != removedItem) : [])} />
            </Expandable>

            <Expandable variant="horizontal" title="Related">
                <BadgeList listType="related-resources" itemList={related} onNew={(newItems) => setRelated(related ? related.concat(newItems) : newItems)} onRemove={(removedItem) => setRelated(related ? related.filter((item) => item != removedItem) : [])} />
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="person-related-persons" itemList={persons} onNew={(newItems) => setPersons(persons ? persons.concat(newItems) : newItems)} onRemove={(removedItem) => setPersons(persons ? persons.filter((item) => item != removedItem) : [])} />
            </Expandable>

            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="organisations" itemList={organisations} onNew={(newItems) => setOrganisations(organisations ? organisations.concat(newItems) : newItems)} onRemove={(removedItem) => setOrganisations(organisations ? organisations.filter((item) => item != removedItem) : [])} />
            </Expandable>

            <Expandable title="Email Address" collapsedHeight={100}>
                {editMode && (  <div className="mt-1 text-sm flex justify-center select-none">
                                    <Edit setNewText={setEmail} currentText={email} property="emailAddress" />
                                </div>)}
                {email || <Skeleton />}
            </Expandable>
            
            <div className="w-full flex justify-center mt-10">
                <div className="flex gap-4">
                    <Button
                        onClick={() => setEditMode(!editMode)}
                        variant={editMode ? "default" : "outline"}
                        className={editMode ? "bg-green-600 hover:bg-green-700 text-white" : "text-gray-700 border-gray-300 hover:bg-gray-100"}
                    >
                        {editMode ? "Disable Edit Mode" : "Enable Edit Mode"}
                    </Button>

                    {userRole === 'admin' && !trashOpen && (
                        <Button
                            onClick={() => setConfirmDialogOpen(true)}
                            variant="outline"
                            className="border-red-500 text-red-500 hover:bg-red-50 hover:border-red-600 hover:text-red-600"
                        >
                            Delete Resource
                        </Button>
                    )}
                </div>
            </div>
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
