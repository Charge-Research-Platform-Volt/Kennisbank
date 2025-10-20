"use client"

import { MetadataTypeEnum, useArchiveSidebar } from "@/context/archive-sidebar-provider";
import React from "react";
import Skeleton from 'react-loading-skeleton';
import Expandable from "./expandable";
import { Item } from "./archive-sidebar";
import { useUserRole } from "@/context/user-role-context";
import { Button } from "@/components/ui/button";
import { useArchive } from "@/context/archive-provider";
import { TrashResource } from "@/actions/trashResourceActions";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { ApiResponse } from "@/types/apiResponse.type";
import { Badge } from "@/components/ui/badge";
import OrganisationIcon from "@/icons/organisation-icon";
import { createDebouncedUpdate } from "@/lib/debouncedUpdate";
import { Input } from "@/components/ui/input";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { AtSign, Link } from "lucide-react";
import { Textarea } from "@/components/ui/textarea";
import AddRelationBadge from "./AddRelationBadge";
import { RelationTrash } from "./relation-trash";
import { removeRelation } from "@/lib/relationManager";
import { RelationTooltip } from "./RelationTooltip";

class OrganisationContentItems
{
    name: string | null = null;
    website: string | null = null;
    description: string | null = null;
    email: string | null = null;
    authored: Item[] = [];
    resources: Item[] = [];
    organisations: Item[] = [];
    persons: Item[] = [];
    trashed: boolean = false;
}

export function OrganisationContent() 
{
    const { currentId, archiveSidebarOpen, setArchiveSidebarOpen, setCreationDate, setPublicationDate, navigate, editMode  } = useArchiveSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload, trashOpen } = useArchive();
    const [confirmDialogOpen, setConfirmDialogOpen] = React.useState<boolean>(false);
    
    const [content, setContent] = React.useState<OrganisationContentItems>(new OrganisationContentItems());
    const updateTimerRef = React.useRef<NodeJS.Timeout | null>(null);
    const updateField = React.useMemo(() => createDebouncedUpdate(updateTimerRef, `/api/organisations/update/${currentId}`), [currentId]);
    
    // Loads all the content at once
    const loadContent = React.useCallback(async () => 
    {
        const params: URLSearchParams = new URLSearchParams();
        
        // Define the properties to select
        params.append('properties', `
            Name,
            Website,
            Description,
            CreationDate,
            EmailAddress as Email,
            Trashed,

            ResourceOrganisationRelations.Select(new(Resource.Id, Resource.Title as Name, Role)) as Resources,
            TargetRelationships.Select(new(TargetOrganisation.Id, TargetOrganisation.Name, Relation)) as TargetOrganisations,
            SourceRelationships.Select(new(SourceOrganisation.Id, SourceOrganisation.Name, Relation)) as SourceOrganisations,
            PersonOrganisationRelations.Select(new(Person.Id, Person.Name, Role)) as Persons
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
            
            setCreationDate(data.body.creationDate || "Unknown.");
            
            const newContent: OrganisationContentItems =
            {
                name: data.body.name || null,
                website: data.body.website || null,
                description: data.body.description || null,
                email: data.body.email || null,
                authored: (data.body.authored || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: undefined
                })),
                resources: (data.body.resources || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: item.role
                })),
                organisations: (data.body.targetOrganisations || []).concat(data.body.sourceOrganisations || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: item.relation
                })),
                persons: (data.body.persons || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: item.role
                })),
                trashed: data.body.trashed || false
            }
            
            setContent(newContent);
        }
        
    }, [currentId]);
    
    // Load new content if sidebar is opened
    React.useEffect(() => {
        if (archiveSidebarOpen) 
        {
            // Clear content
            setContent(new OrganisationContentItems());
            setCreationDate(null);
            setPublicationDate(null);

            // Load content
            loadContent();
        }
    }, [currentId, loadContent, archiveSidebarOpen, setCreationDate, setPublicationDate])
    
    // Delete the organisation
    const confirmDelete = async () => 
    {
        if (await TrashResource(currentId, MetadataTypeEnum.ORGANISATION)) 
        {
            setArchiveSidebarOpen(false);
            triggerGridReload();
        }
    }

    return (
        <>
            <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />

            {/* Banner for when organisation is in trash */}
            { content.trashed &&
                <Badge variant="outline" className="w-full mb-5 flex flex-col border-red-500 text-red-500">
                    <h1 className="text-xl">This item is in the trash.</h1>
                    <span className="flex-1 mb-1">Contact an admin if you think this is a mistake.</span>
                </Badge>
            }
            
            {/* Title */}
            <div className="flex items-center justify-start gap-2 pb-2">
                <OrganisationIcon className="w-5 h-5" />
                
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
                {/* Website */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.website)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <Link width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Website</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (
                        <a href={content.website || ""} className="select-none pb-1" target="_blank" rel="norefferor">
                            <span className="ml-2 pt-0.25 text-sm text-blue-500 underline flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.website}</span>
                        </a>
                    )}
                    
                    { editMode && (
                        <Input type="url" className="ml-3 flex-1" value={content.website || ""} onChange={(e) => 
                        {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, website: newValue }));
                            updateField("Website", newValue);
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
                                removeAction={() => removeRelation("organisations", currentId, "authored-resources", item.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, authored: prevContent.authored.filter(i => i.id !== item.id) }))}
                            />
                        )}
                    </Badge>
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityType="organisations"
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
            
            <Expandable variant="horizontal" title="Related Resources" hidden={!(editMode || content.resources.length > 0)} defaultOpen={editMode}>
                { content.resources.map((item) => (
                    <RelationTooltip
                        key={item.id}
                        item={item}
                        entityType="organisations"
                        currentId={currentId}
                        relationType="related-resources"
                        editMode={editMode}
                        onClick={() => {
                            if (!editMode) {
                                navigate(item.id, MetadataTypeEnum.RESOURCE);
                            }
                        }}
                        onRelationUpdate={(itemId, newRelation) => {
                            setContent(prevContent => ({
                                ...prevContent,
                                resources: prevContent.resources.map(i =>
                                    i.id === itemId ? { ...i, relation: newRelation } : i
                                )
                            }));
                        }}
                        trashComponent={
                            <RelationTrash
                                removeAction={() => removeRelation("organisations", currentId, "related-resources", item.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, resources: prevContent.resources.filter(i => i.id !== item.id)}))}
                            />
                        }
                    />
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityType="organisations"
                        entityId={currentId}
                        relationType="related-resources"
                        searchEndpoint="/api/resources/list"
                        searchMethod="GET"
                        alreadyRelated={content.resources}
                        onAdd={(newResources) => 
                        {
                            setContent(prevContent => ({ ...prevContent, resources: [...prevContent.resources, ...newResources]}))
                        }}
                        placeholder="Search resources..."
                        allowMultiple={true}
                        title="Add Related Resources"
                    />
                )}
            </Expandable>
            
            <Expandable variant="horizontal" title="Related Organisations" hidden={!(editMode || content.organisations.length > 0)} defaultOpen={editMode}>
                { content.organisations.map((organisation) => (
                    <RelationTooltip
                        key={organisation.id}
                        item={organisation}
                        entityType="organisations"
                        currentId={currentId}
                        relationType="related-organisations"
                        editMode={editMode}
                        onClick={() => {
                            if (!editMode) {
                                navigate(organisation.id, MetadataTypeEnum.ORGANISATION);
                            }
                        }}
                        onRelationUpdate={(itemId, newRelation) => {
                            setContent(prevContent => ({
                                ...prevContent,
                                organisations: prevContent.organisations.map(o =>
                                    o.id === itemId ? { ...o, relation: newRelation } : o
                                )
                            }));
                        }}
                        trashComponent={
                            <RelationTrash
                                removeAction={() => removeRelation("organisations", currentId, "related-organisations", organisation.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, organisations: prevContent.organisations.filter(o => o.id !== organisation.id) }))}
                            />
                        }
                    />
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityId={currentId}
                        entityType="organisations"
                        relationType="related-organisations"
                        searchEndpoint="/api/organisations/list"
                        searchMethod="GET"
                        alreadyRelated={content.organisations}
                        onAdd={(newRelated) => 
                        {
                            setContent(prevContent => ({ ...prevContent, organisations: [...prevContent.organisations, ...newRelated]}))
                        }}
                        placeholder="Search organisations..."
                        allowMultiple={true}
                        title="Add Related Organisations"
                    />
                )}
            </Expandable>
            
            <Expandable variant="horizontal" title="Related People" hidden={!(editMode || content.persons.length > 0)} defaultOpen={editMode}>
                { content.persons.map((person) => (
                    <RelationTooltip
                        key={person.id}
                        item={person}
                        entityType="organisations"
                        currentId={currentId}
                        relationType="related-persons"
                        editMode={editMode}
                        onClick={() => {
                            if (!editMode) {
                                navigate(person.id, MetadataTypeEnum.PERSON);
                            }
                        }}
                        onRelationUpdate={(itemId, newRelation) => {
                            setContent(prevContent => ({
                                ...prevContent,
                                persons: prevContent.persons.map(p =>
                                    p.id === itemId ? { ...p, relation: newRelation } : p
                                )
                            }));
                        }}
                        trashComponent={
                            <RelationTrash
                                removeAction={() => removeRelation("organisations", currentId, "related-persons", person.id)}
                                successAction={() => setContent(prevContent => ({ ...prevContent, persons: prevContent.persons.filter(p => p.id !== person.id) }))}
                            />
                        }
                    />
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityId={currentId}
                        entityType="organisations"
                        relationType="related-persons"
                        searchEndpoint="/api/persons/list"
                        searchMethod="GET"
                        alreadyRelated={content.persons}
                        onAdd={(newRelated) => 
                        {
                            setContent(prevContent => (
                            {
                                ...prevContent,
                                persons: [...prevContent.persons, ...newRelated]
                            }));
                        }}
                        placeholder="Search people..."
                        allowMultiple={true}
                        title="Add Related People"
                    />
                )}
            </Expandable>
            
            {/* Delete Button */}
            {userRole === "admin" && !trashOpen && editMode &&
            (
                <div className="mt-10 flex w-full justify-center">
                    <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:border-red-600 hover:bg-red-50 hover:text-red-600">
                        Delete Organisation
                    </Button>
                </div>
            )}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
