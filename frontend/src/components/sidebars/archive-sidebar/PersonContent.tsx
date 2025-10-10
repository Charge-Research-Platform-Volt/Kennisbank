"use client"

import React from "react";
import { MetadataTypeEnum, useArchiveSidebar } from "@/context/archive-sidebar-provider";
import Skeleton from 'react-loading-skeleton'
import Expandable from "./expandable"
import { ListItem } from "./BadgeList";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { TrashResource } from "@/actions/trashResourceActions";
import { useUserRole } from "@/context/user-role-context";
import { useArchive } from "@/context/archive-provider";
import { ApiResponse } from "@/types/apiResponse.type";
import { Badge } from "@/components/ui/badge";
import PersonIcon from "@/icons/person-icon";
import { createDebouncedUpdate } from "@/lib/debouncedUpdate";
import { Input } from "@/components/ui/input";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { AtSign, BriefcaseBusiness, Linkedin } from "lucide-react";
import { Textarea } from "@/components/ui/textarea";
import { RelationTrash } from "./relation-trash";
import { removeRelation } from "@/lib/relationManager";
import AddRelationBadge from "./AddRelationBadge";
import { Button } from "@/components/ui/button";

class PersonContentItems
{
    name: string | null = null;
    description: string | null = null;
    occupation: string | null = null;
    email: string | null = null;
    linkedIn: string | null = null;
    authored: ListItem[] = [];
    relatedResources: ListItem[] = [];
    relatedPersons: ListItem[] = [];
    relatedOrganisations: ListItem[] = [];
    trashed: boolean = false;
}

export function PersonContent() 
{
    const { currentId, archiveSidebarOpen, setArchiveSidebarOpen, setCreationDate, setPublicationDate, editMode, navigate } = useArchiveSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload, trashOpen } = useArchive();
    const [confirmDialogOpen, setConfirmDialogOpen] = React.useState<boolean>(false);
    
    const [content, setContent] = React.useState<PersonContentItems>(new PersonContentItems());
    const updateTimerRef = React.useRef<NodeJS.Timeout | null>(null);
    const updateField = React.useMemo(() => createDebouncedUpdate(updateTimerRef, `/api/persons/update/${currentId}`), [currentId]);

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
            ResourceRelatedPersonRelations.Select(new(Resource.Id, Resource.Title as Name)) as RelatedResources,
            TargetRelationships.Select(new(TargetPerson.Id, TargetPerson.Name)) as TargetPersons,
            SourceRelationships.Select(new(SourcePerson.Id, SourcePerson.Name)) as SourcePersons,
            PersonOrganisationRelations.Select(new(Organisation.Id, Organisation.Name)) as RelatedOrganisations
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
            
            setCreationDate(data.body.creationDate || "Unknown.");
            
            const newContent: PersonContentItems =
            {
                name: data.body.name || null,
                description: data.body.description || null,
                occupation: data.body.occupation || null,
                email: data.body.email || null,
                linkedIn: data.body.linkedin || null,
                authored: data.body.authored || [],
                relatedResources: data.body.relatedResources || [],
                relatedPersons: (data.body.targetPersons || []).concat(data.body.sourcePersons || []),
                relatedOrganisations: data.body.relatedOrganisations || [],
                trashed: data.body.trashed || false
            }

            setContent(newContent);
        }
    }, [currentId])

    // Reload content on sidebar open or when currentId changes
    React.useEffect(() =>
    {
        if (archiveSidebarOpen)
        {
            // Clear content
            setContent(new PersonContentItems());
            setCreationDate(null);
            setPublicationDate(null);

            // Load content
            loadContent();
        }
    }, [archiveSidebarOpen, currentId, loadContent]);
    
    // Delete the person
    const confirmDelete = async () => 
    {
        if (await TrashResource(currentId, MetadataTypeEnum.PERSON)) 
        {
            setArchiveSidebarOpen(false);
            triggerGridReload();
        }
    }

    return (
        <>
            <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />

            {/* Banner for when person is in trash */}
            { content.trashed &&
                <Badge variant="outline" className="w-full mb-5 flex flex-col border-red-500 text-red-500">
                    <h1 className="text-xl">This item is in the trash.</h1>
                    <span className="flex-1 mb-1">Contact an admin if you think this is a mistake.</span>
                </Badge>
            }
            
            {/* Title */}
            <div className="flex items-center justify-start gap-2 pb-2">
                <PersonIcon className="w-5 h-5" />
                
                {!editMode && (
                    <h1 className="text-2xl font-bold select-none">{content.name || <Skeleton />}</h1>
                )}
                
                {editMode && (
                    <Input
                        type="text"
                        className="flex-1"
                        value={content.name || ""}
                        onChange={(e) => {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, name: newValue }));
                            updateField("name", newValue);
                        }}
                    />
                )}
            </div>
            
            {/* Characteristics */}
            <div className="mb-4">
                {/* Occupation */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.occupation)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <BriefcaseBusiness width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Occupation</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (<span className="ml-2 pt-0.25 text-sm flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.occupation}</span>)}
                    {editMode && (
                        <Input type="text" className="ml-3 flex-1" value={content.occupation || ""} onChange={(e) => 
                        {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, occupation: newValue }));
                            updateField("Occupation", newValue);
                        }} />
                    )}
                </div>
                
                {/* Email */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.email)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <AtSign width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Email Address</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (
                        <a href={"mailto:" + content.email} className="select-none pb-1" target="_blank" rel="noreferror">
                            <span className="ml-2 pt-0.25 text-sm text-blue-500 underline flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.email}</span>
                        </a>
                    )}
                    
                    {editMode && (
                        <Input type="email" className="ml-3 flex-1" value={content.email || ""} onChange={(e) => 
                        {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, email: newValue }));
                            updateField("EmailAddress", newValue);
                        }} />
                    )}
                </div>
                
                {/* LinkedIn */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.linkedIn)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <Linkedin width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>LinkedIn</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (
                        <a href={content.linkedIn || ""} className="select-none pb-1" target="_blank" rel="noreferror">
                            <span className="ml-2 pt-0.25 text-sm text-blue-500 underline flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.linkedIn}</span>
                        </a>
                    )}
                    
                    {editMode && (
                        <Input type="url" className="ml-3 flex-1" value={content.linkedIn || ""} onChange={(e) => 
                        {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, linkedIn: newValue }));
                            updateField("Linkedin", newValue); 
                        }} />
                    )}
                </div>
            </div>
            
            {/* Description */}
            <Expandable title="Description" collapsedHeight={100} hidden={!(editMode || content.description)} defaultOpen={editMode}>
                {!editMode && (<span className="text-xs">{content.description || <Skeleton />}</span>)}
            
                {editMode && (
                    <Textarea rows={10} className="w-full text-xs" value={content.description || ""} onChange={(e) => 
                    {
                        const newValue = e.target.value;
                        setContent(prevContent => ({ ...prevContent, description: newValue }));
                        updateField("description", newValue);
                    }} />
                )}
            </Expandable>
            
            {/* Authored Resources */}
            <Expandable variant="horizontal" title="Authored Resources" hidden={!(editMode || content.authored.length > 0)} defaultOpen={editMode}>
                { content.authored.map((item) => (
                    <Badge key={item.id} variant="outline" className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer" onClick={async () => 
                    {
                        if (editMode) return;
                        
                        navigate(item.id, MetadataTypeEnum.RESOURCE);
                    }}>
                        <span className="truncate">{item.name}</span>
                        
                        {editMode && (
                            <RelationTrash
                                removeAction={() => removeRelation("persons", currentId, "authored-resources", item.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, authored: prevContent.authored.filter(i => i.id !== item.id) }))}
                            />
                        )}
                    </Badge>
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityType="persons"
                        entityId={currentId}
                        relationType="authored-resources"
                        searchEndpoint="/api/resources/list"
                        searchMethod="GET"
                        alreadyRelated={content.authored}
                        onAdd={(newAuthored) =>
                        {
                            setContent(prevContent => ({
                                ...prevContent,
                                authored: [...prevContent.authored, ...newAuthored]
                            }));
                        }}
                        placeholder="Search resources..."
                        allowMultiple={true}
                        title="Add Authored Resources"
                    />
                )}
            </Expandable>
            
            {/* Related Resources */}
            <Expandable variant="horizontal" title="Related Resources" hidden={!(editMode || content.relatedResources.length > 0)} defaultOpen={editMode}>
                { content.relatedResources.map((item) => (
                    <Badge key={item.id} variant="outline" className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer" onClick={async () =>
                    {
                        if (editMode) return;

                        navigate(item.id, MetadataTypeEnum.RESOURCE);
                    }}>
                        <span className="truncate">{item.name}</span>

                        {editMode && (
                            <RelationTrash
                                removeAction={() => removeRelation("persons", currentId, "related-resources", item.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, relatedResources: prevContent.relatedResources.filter(i => i.id !== item.id) }))}
                            />
                        )}
                    </Badge>
                ))}

                { editMode && (
                    <AddRelationBadge
                        entityType="persons"
                        entityId={currentId}
                        relationType="related-resources"
                        searchEndpoint="/api/resources/list"
                        searchMethod="GET"
                        alreadyRelated={content.relatedResources}
                        onAdd={(newRelated) =>
                        {
                            setContent(prevContent => ({
                                ...prevContent,
                                relatedResources: [...prevContent.relatedResources, ...newRelated]
                            }));
                        }}
                        placeholder="Search resources..."
                        allowMultiple={true}
                        title="Add Related Resources"
                    />
                )}
            </Expandable>
            
            {/* Related People */}
            <Expandable variant="horizontal" title="Related People" hidden={!(editMode || content.relatedPersons.length > 0)} defaultOpen={editMode}>
                { content.relatedPersons.map((person) => (
                    <Badge key={person.id} variant="outline" className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer" onClick={() => 
                    {
                        if (editMode) return;
                        
                        navigate(person.id, MetadataTypeEnum.PERSON);
                    }}>
                        <span className="truncate">{person.name}</span>
                        
                        { editMode && (
                            <RelationTrash
                                removeAction={() => removeRelation("persons", currentId, "related-persons", person.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, relatedPersons: prevContent.relatedPersons.filter(p => p.id !== person.id) }))}
                            />
                        )}
                    </Badge>
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityType="persons"
                        entityId={currentId}
                        relationType="related-persons"
                        searchEndpoint="/api/persons/list"
                        searchMethod="GET"
                        alreadyRelated={content.relatedPersons}
                        onAdd={(newRelated) =>
                        {
                            setContent(prevContent => (
                            {
                                ...prevContent,
                                relatedPersons: [...prevContent.relatedPersons, ...newRelated]
                            }));
                        }}
                        placeholder="Search people..."
                        allowMultiple={true}
                        title="Add Related People"
                    />
                )}
            </Expandable>
            
            <Expandable variant="horizontal" title="Related Organisations" hidden={!(editMode || content.relatedOrganisations.length > 0)} defaultOpen={editMode}>
                { content.relatedOrganisations.map((organisation) => (
                    <Badge key={organisation.id} variant="outline" className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer" onClick={() => 
                    {
                        if (editMode) return;
                        
                        navigate(organisation.id, MetadataTypeEnum.ORGANISATION);
                    }}>
                        <span className="truncate">{organisation.name}</span>
                        
                        { editMode && (
                            <RelationTrash
                                removeAction={() => removeRelation("persons", currentId, "organisations", organisation.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, relatedOrganisations: prevContent.relatedOrganisations.filter(o => o.id !== organisation.id)}))}
                            />
                        )}
                    </Badge>
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityType="persons"
                        entityId={currentId}
                        relationType="organisations"
                        searchEndpoint="/api/organisations/list"
                        searchMethod="GET"
                        alreadyRelated={content.relatedOrganisations}
                        onAdd={(newRelated) =>
                        {
                            setContent(prevContent => (
                            {
                                ...prevContent,
                                relatedOrganisations: [...prevContent.relatedOrganisations, ...newRelated]
                            }));
                        }}
                        placeholder="Search organisations..."
                        allowMultiple={true}
                        title="Add Related Organisations"
                    />
                )}
            </Expandable>
            
            {/* Delete Button */}
            {userRole === "admin" && !trashOpen && editMode &&
            (
                <div className="mt-10 flex w-full justify-center">
                    <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:border-red-600 hover:bg-red-50 hover:text-red-600">
                        Delete Person
                    </Button>
                </div>
            )}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
