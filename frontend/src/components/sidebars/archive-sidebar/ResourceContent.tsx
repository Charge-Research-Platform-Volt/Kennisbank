"use client";

import Expandable from "./expandable";
import BadgeList from "./BadgeList";
import { Item } from "./archive-sidebar"
import { useArchiveSidebar, MetadataTypeEnum } from "@/context/archive-sidebar-provider";
import { getRelation } from "@/actions/archive-sidebarActions";
import React from "react";
import Skeleton from "react-loading-skeleton";
import ResourceList from "./ResourceList";
import { Button } from "@/components/ui/button";
import { useUserRole } from "@/context/user-role-context";
import ConfirmDeleteDialog from "@/components/ui/confirm-delete-dialog";
import { ApiResponse } from "@/types/apiResponse.type";
import { useArchive } from "@/context/archive-provider";
import { TrashResource } from "@/actions/trashResourceActions";
import { Badge } from "@/components/ui/badge";
import GetFileIcon from "@/components/getFileIcon";
import { Input } from "@/components/ui/input";
import { Textarea } from "@/components/ui/textarea";
import { createDebouncedUpdate } from "@/lib/debouncedUpdate";
import { Fingerprint, Languages, Link, Scale, Tag } from "lucide-react";
import { getLanguageLabel, LanguageCodes } from "@/lists/languageCodes";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { removeRelation } from "@/lib/relationManager";
import AddRelationBadge from "./AddRelationBadge";
import AddAuthorBadge from "./AddAuthorBadge";
import { RelationTrash } from "./relation-trash";
import { RelationTooltip } from "./RelationTooltip";

interface ResourceContentProps
{
    fileType: string | null;
    setFileType: (type: string | null) => void;
}

class ResourceContentItems 
{
    title: string | null = null;
    url: string | null = null;
    description: string | null = null;
    note: string | null = null;
    authors: Item[] = [];
    tags: Item[] = [];
    aiTags: Item[] = [];
    organisations: Item[] = [];
    relatedPersons: Item[] = [];
    similarResources: Item[] = [];
    source: string | null = null;
    regions: Item[] = [];
    langCode: string | null = null;
    pubCode: string | null = null;
    license: string | null = null;
    abstract: string | null = null;
    resourceTypeId: string | null = null;
    resourceTypeName: string | null = null;
    trashed: boolean = false;
    fileExt: string = "";
}

interface ResourceType
{
    id: string;
    name: string;
}

export function ResourceContent({ fileType, setFileType }: ResourceContentProps)
{
    const { currentId, archiveSidebarOpen, setArchiveSidebarOpen, setCreationDate, setPublicationDate, setPublicationDatePrecision, editMode, navigate } = useArchiveSidebar();
    const { userRole } = useUserRole();
    const { triggerGridReload, trashOpen } = useArchive();
    const [confirmDialogOpen, setConfirmDialogOpen] = React.useState<boolean>(false);

    const [content, setContent] = React.useState<ResourceContentItems>(new ResourceContentItems());
    const [resourceTypes, setResourceTypes] = React.useState<ResourceType[]>([]);
    const updateTimerRef = React.useRef<NodeJS.Timeout | null>(null);
    const updateField = React.useMemo(() => createDebouncedUpdate(updateTimerRef, `/api/resources/update/${currentId}`), [currentId]);

    // Loads all content at once
    const loadContent = React.useCallback(async () =>
    {
        const params: URLSearchParams = new URLSearchParams();

        // Define properties to get
        params.append(
            "properties",
            `
            Title,
            Description,
            LanguageCode,
            PublicationCode,
            PublicationDate,
            PublicationDatePrecision,
            License,
            CreationDate,
            Note,
            FileType,
            FileExt,
            Trashed,
            SourceUrl,
            WebsiteMetadata.Url as Url,
            DocumentMetadata.Abstract as Abstract,
            ResourceAuthorRelations.Select(new(Author.Id, Author.Name)) as Authors,
            ResourceOrganisationRelations.Select(new(Organisation.Id, Organisation.Name, Role)) as Organisations,
            ResourceRegionRelations.Select(new(Region.Id, Region.Name)) as Regions,
            ResourceRelatedPersonRelations.Select(new(Person.Id, Person.Name, Role)) as RelatedPersons,
            ResourceTagRelations.Select(new(Tag.Id, Tag.Name)) as Tags,
            ResourceType.Id as ResourceTypeId,
            ResourceType.Name as ResourceTypeName
        `,
        );

        // Fetch
        const response = await fetch(`/api/resources/info/${currentId}?${params.toString()}`,
        {
            method: "GET",
            credentials: "include",
        });

        // Set all states on success
        if (response.ok)
        {
            const data: ApiResponse = await response.json();

            setPublicationDate(data.body.publicationDate || null);
            setPublicationDatePrecision(data.body.publicationDatePrecision ?? null);
            setCreationDate(data.body.creationDate || "Unknown.");
            setFileType(data.body.fileType || "Unknown.");
            
            const newContent: ResourceContentItems =
            {
                title: data.body.title || null,
                url: data.body.url || null,
                description: data.body.description || null,
                note: data.body.note || null,
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                authors: (data.body.authors || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: undefined
                })),
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                tags: (data.body.tags || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: undefined
                })),
                aiTags: data.body.aiTags || [],
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                organisations: (data.body.organisations || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: item.role
                })),
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                relatedPersons: (data.body.relatedPersons || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: item.role
                })),
                similarResources: data.body.similarResources || [],
                source: data.body.sourceUrl || null,
                // eslint-disable-next-line @typescript-eslint/no-explicit-any
                regions: (data.body.regions || []).map((item: any) => ({
                    id: item.id,
                    name: item.name,
                    relation: undefined
                })),
                langCode: data.body.languageCode || null,
                pubCode: data.body.publicationCode || null,
                license: data.body.license || null,
                abstract: data.body.abstract || null,
                resourceTypeId: data.body.resourceTypeId || null,
                resourceTypeName: data.body.resourceTypeName || null,
                trashed: data.body.trashed || false,
                fileExt: data.body.fileExt || ""
            }
            
            setContent(newContent);
        }
        
        // Retrieve the resource types
        const response2 = await fetch(`/api/resources/types/list`, 
        {
            method: 'GET',
            credentials: 'include',
        });
        
        // Store list on success
        if (response2.ok) 
        {
            const data: ApiResponse = await response2.json();
            
            setResourceTypes(data.body);
        }
    }, [currentId, setCreationDate, setFileType, setPublicationDate, setPublicationDatePrecision]);
    
    // Loads the similar resources
    const loadSimilarResources = React.useCallback(async () =>
    {
        const similarResourcesPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "resource-similar-resources");

        similarResourcesPromise
            .then((response) =>
            {
                const list: Item[] = response.body.map((item: { id: string; title: string }) => ({
                    id: item.id,
                    name: item.title,
                }));

                setContent(prevContent => ({
                    ...prevContent,
                    similarResources: list
                }));
            })
            .catch((error) =>
            {
                console.log("Error loading similar resources: ", error);

                setContent(prevContent => ({
                    ...prevContent,
                    similarResources: []
                }));
            });
    }, [currentId]);

    // Loads the AI tags
    const loadAiTags = React.useCallback(async () =>
    {
        const aiTagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "ai-tags");

        aiTagsPromise
            .then((response) =>
            {
                // Check if we got an array back, if not dont continue
                if (!Array.isArray(response.body.tags))
                {
                    console.log("This document is not processed. No AI tags available.");
                    return;
                }

                //Add translation logic from ai tags to Item[] here
                const list: Item[] = response.body.tags.map((item: string) => ({
                    id: item,
                    name: item,
                }));

                const finalList = list.length > 10 ? list.sort(() => 0.5 - Math.random()).slice(0, 10) : list;

                setContent(prevContent => ({
                    ...prevContent,
                    aiTags: finalList
                }));
            })
            .catch((error) =>
            {
                console.error("Error loading tags: ", error);
            });
    }, [currentId]);

    // Reload content on sidebar open
    React.useEffect(() =>
    {
        if (archiveSidebarOpen)
        {
            // Clear everything
            setFileType(null);
            setPublicationDate(null);
            setCreationDate(null);
            setContent(new ResourceContentItems());

            // Load content
            loadContent();
            loadSimilarResources();
            loadAiTags();
        }
    }, [currentId, loadContent, loadSimilarResources, loadAiTags, archiveSidebarOpen, setPublicationDate, setCreationDate, setFileType]);

    // Deletes resource
    const confirmDelete = async () =>
    {
        if (await TrashResource(currentId, MetadataTypeEnum.RESOURCE))
        {
            setArchiveSidebarOpen(false);
            triggerGridReload();
        }
    };
    
    return (
        <>
            <ConfirmDeleteDialog open={confirmDialogOpen} onOpenChange={setConfirmDialogOpen} onConfirmation={confirmDelete} />

            {/* Banner for when resource is in trash */}
            {content.trashed && (
                <Badge variant="outline" className="mb-5 flex w-full flex-col border-red-500 text-red-500">
                    <h1 className="text-xl">This item is in the trash.</h1>
                    <span className="mb-1 flex-1">Contact an admin if you think this is a mistake.</span>
                </Badge>
            )}

            {/* Title */}
            <div className="flex items-center justify-start gap-2 pb-2">
                <GetFileIcon fileType={content.fileExt || fileType || ""} className="h-5 w-5" />
                
                {!editMode && (
                    <h1 className="text-2xl font-bold select-none">{content.title || <Skeleton />}</h1>
                )}
                
                {editMode && (
                    <Input
                        type="text"
                        className="flex-1"
                        value={content.title || ""}
                        onChange={(e) => {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, title: newValue }));
                            updateField("title", newValue);
                        }}
                    />
                )}
            </div>
            
            {/* Characteristics */}
            <div className="mb-4">
                {/* Language */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.langCode)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <Languages width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Language of the resource</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (<span className="ml-2 pt-0.25 text-sm flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{getLanguageLabel(content.langCode)}</span>)}
                    {editMode && (
                        <Select value={content.langCode || ""} onValueChange={(value) => {
                            setContent(prevContent => ({ ...prevContent, langCode: value }));
                            updateField("languageCode", value);
                        }}>
                            <SelectTrigger className="ml-3 flex-1">
                                <SelectValue placeholder="Select Language..." />
                            </SelectTrigger>
                            <SelectContent>
                                {LanguageCodes.map((lang) => (
                                    <SelectItem key={lang.value} value={lang.value}>
                                        {lang.label}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    )}
                </div>
                
                {/* Resource Type */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.resourceTypeName)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <Tag width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Type of the resource</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (<span className="ml-2 pt-0.25 text-sm flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.resourceTypeName}</span>)}
                    {editMode && (
                        <Select value={content.resourceTypeId || ""} onValueChange={(value) => {
                            const selectedType = resourceTypes.find(t => t.id === value);
                            setContent(prevContent => ({
                                ...prevContent,
                                resourceTypeId: value,
                                resourceTypeName: selectedType?.name || null
                            }));
                            updateField("typeId", value);
                        }}>
                            <SelectTrigger className="ml-3 flex-1">
                                <SelectValue placeholder="Select Resource Type..." />
                            </SelectTrigger>
                            <SelectContent>
                                {resourceTypes.map((type) => (
                                    <SelectItem key={type.id} value={type.id}>
                                        {type.name}
                                    </SelectItem>
                                ))}
                            </SelectContent>
                        </Select>
                    )}
                </div>
                
                {/* Source URL */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.url || content.source)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <Link width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            { content.url && (<p>URL of the website</p>)}
                            { !content.url && (<p>Source of the resource</p>)}
                        </TooltipContent>
                    </Tooltip>
                    {!editMode && (
                        <a href={content.url || content.source || ""} className="select-none pb-1" target="_blank" rel="noreferror">
                            <span className="ml-2 pt-0.25 text-sm text-blue-500 underline flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.url || content.source || "-"}</span>
                        </a>
                    )}
                    
                    {editMode && (
                        <Input type="url" className="ml-3 flex-1" value={content.url || content.source || ""} onChange={(e) =>
                        {
                            const newValue = e.target.value;
                            if (content.url) 
                            {
                                setContent(prevContent => ({ ...prevContent, url: newValue }));
                                updateField("Url", newValue);
                            }
                            else 
                            {
                                setContent(prevContent => ({ ...prevContent, source: newValue }));
                                updateField("SourceUrl", newValue);
                            }
                        }} />
                    )}
                </div>
                
                {/* Publication Code */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.pubCode)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <Fingerprint width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>Publication code</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (<span className="ml-2 pt-0.25 text-sm flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.pubCode || "-"}</span>)}
                    {editMode && (
                        <Input type="text" className="ml-3 flex-1" value={content.pubCode || ""} onChange={(e) => 
                        {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, pubCode: newValue }));
                            updateField("PublicationCode", newValue);
                        }} />
                    )}
                </div>
                
                {/* License */}
                <div className="w-full flex justify-start" hidden={!(editMode || content.license)}>
                    <Tooltip delayDuration={500}>
                        <TooltipTrigger>
                            <Scale width={15} />
                        </TooltipTrigger>
                        <TooltipContent>
                            <p>License</p>
                        </TooltipContent>
                    </Tooltip>
                    
                    {!editMode && (<span className="ml-2 pt-0.25 text-sm flex-1 h-full whitespace-nowrap overflow-hidden text-ellipsis">{content.license || "-"}</span>)}
                    {editMode && (
                        <Input type="text" className="ml-3 flex-1" value={content.license || ""} onChange={(e) => 
                        {
                            const newValue = e.target.value;
                            setContent(prevContent => ({ ...prevContent, license: newValue }));
                            updateField("License", newValue);
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
            
            {/* Tags */}
            <Expandable variant="horizontal" title="Tags" hidden={!(editMode || content.tags.length > 0)} defaultOpen={editMode}>
                { content.tags.map((tag) => (
                    <Badge key={tag.id} variant="outline" className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer" onClick={async () =>
                    {
                        if (editMode) return;
                        
                        // TODO: APPLY EDIT FILTER
                    }}>
                        <span className="truncate">{tag.name}</span>
                        {editMode && (
                            <RelationTrash
                                removeAction={() => removeRelation("resources", currentId, "tags", tag.id)}
                                successAction={() => setContent(prevContent => ({
                                    ...prevContent,
                                    tags: prevContent.tags.filter(t => t.id !== tag.id)
                                }))}
                            />
                        )}
                    </Badge>
                )) }
                {editMode && (
                    <AddRelationBadge
                        entityType="resources"
                        entityId={currentId}
                        relationType="tags"
                        searchEndpoint="/api/tags/tags"
                        alreadyRelated={content.tags}
                        onAdd={(newTags) => {
                            setContent(prevContent => ({
                                ...prevContent,
                                tags: [...prevContent.tags, ...newTags]
                            }));
                        }}
                        placeholder="Search tags..."
                        allowMultiple={true}
                        title="Add Tags"
                    />
                )}
            </Expandable>
            
            {/* Recommended Tags */}
            <Expandable variant="horizontal" title="Recommended Tags" hidden={!(editMode || content.aiTags.length > 0)} defaultOpen={editMode}>
                <BadgeList listType="ai-tags" itemList={content.aiTags} onNew={() => {}} onRemove={() => {}} />
            </Expandable>

            {/* Authors */}
            <Expandable variant="horizontal" title="Authors" hidden={!(editMode || content.authors.length > 0)} defaultOpen={editMode}>
                { content.authors.map((author) => (
                    <Badge key={author.id} variant="outline" className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer" onClick={async () => 
                    {
                        if (editMode) return
                        
                        navigate(author.id, MetadataTypeEnum.PERSON);
                    }}>
                        <span className="truncate">{author.name}</span>
                        {editMode && (
                            <RelationTrash
                                removeAction={() => removeRelation("resources", currentId, "authors", author.id)}
                                successAction={() => setContent(prevContent => ({
                                    ...prevContent,
                                    authors: prevContent.authors.filter(a => a.id !== author.id)
                                }))}
                            />
                        )}
                    </Badge>
                )) }
                
                { editMode && (
                    <AddAuthorBadge
                        resourceId={currentId}
                        alreadyRelated={content.authors}
                        onAdd={(newAuthors) =>
                        {
                            setContent(prevContent => ({
                                ...prevContent,
                                authors: [...prevContent.authors, ...newAuthors]
                            }));
                        }}
                    />
                )}
            </Expandable>
            
            {/* Organisations */}
            <Expandable variant="horizontal" title="Related Organisations" hidden={!(editMode || content.organisations.length > 0)} defaultOpen={editMode}>
                { content.organisations.map((organisation) => (
                    <RelationTooltip
                        key={organisation.id}
                        item={organisation}
                        entityType="resources"
                        currentId={currentId}
                        relationType="organisations"
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
                                removeAction={() => removeRelation("resources", currentId, "organisations", organisation.id)}
                                successAction={() => setContent(prevContent => ({
                                    ...prevContent,
                                    organisations: prevContent.organisations.filter(o => o.id !== organisation.id)
                                }))}
                            />
                        }
                    />
                ))}
                
                {editMode && (
                    <AddRelationBadge
                        entityType="resources"
                        entityId={currentId}
                        relationType="organisations"
                        searchEndpoint="/api/organisations/list"
                        searchMethod="GET"
                        alreadyRelated={content.organisations}
                        onAdd={(newOrganisations) =>
                        {
                            setContent(prevContent => ({
                                ...prevContent,
                                organisations: [...prevContent.organisations, ...newOrganisations]
                            }));
                        }}
                        placeholder="Search organisations..."
                        allowMultiple={true}
                        title="Add Organisations"
                    />
                )}
            </Expandable>

            {/* Related People */}
            <Expandable variant="horizontal" title="Related People" hidden={!(editMode || content.relatedPersons.length > 0)} defaultOpen={editMode}>
                { content.relatedPersons.map((person) => (
                    <RelationTooltip
                        key={person.id}
                        item={person}
                        entityType="resources"
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
                                relatedPersons: prevContent.relatedPersons.map(p =>
                                    p.id === itemId ? { ...p, relation: newRelation } : p
                                )
                            }));
                        }}
                        trashComponent={
                            <RelationTrash
                                removeAction={() => removeRelation("resources", currentId, "related-persons", person.id)}
                                successAction={() => setContent(prevContent => ({
                                    ...prevContent,
                                    relatedPersons: prevContent.relatedPersons.filter(p => p.id !== person.id)
                                }))}
                            />
                        }
                    />
                ))}
                
                {editMode && (
                    <AddRelationBadge
                        entityType="resources"
                        entityId={currentId}
                        relationType="related-persons"
                        searchEndpoint="/api/persons/list"
                        searchMethod="GET"
                        alreadyRelated={content.relatedPersons}
                        onAdd={(newRelatedPersons) =>
                        {
                            setContent(prevContent => ({
                                ...prevContent,
                                relatedPersons: [...prevContent.relatedPersons, ...newRelatedPersons]
                            }));
                        }}
                        placeholder="Search people..."
                        allowMultiple={true}
                        title="Add Related People"
                    />
                )}
            </Expandable>

            {/* Similar Resources */}
            <ResourceList header="Similar Resources" resources={content.similarResources} />
            
            {/* Regions */}
            <Expandable variant="horizontal" title="Regions" hidden={!(editMode || content.regions.length > 0)} defaultOpen={editMode}>
                { content.regions.map((region) => (
                    <Badge key={region.id} variant="outline" className="h-8 max-w-50 flex items-center overflow-hidden cursor-pointer" onClick={async () => 
                    {
                        if (editMode) return;
                        
                        // TODO: REGION FILTER ON
                    }}>
                        <span className="truncate">{region.name}</span>
                        {editMode && (
                            <RelationTrash
                                removeAction={() => removeRelation("resources", currentId, "regions", region.id)}
                                successAction={() => setContent(prevContent => ({
                                    ...prevContent,
                                    regions: prevContent.regions.filter(o => o.id !== region.id)
                                }))}
                            />
                        )}
                    </Badge>
                ))}
                
                { editMode && (
                    <AddRelationBadge
                        entityType="resources"
                        entityId={currentId}
                        relationType="regions"
                        searchEndpoint="/api/regions/list"
                        searchMethod="GET"
                        alreadyRelated={content.regions}
                        onAdd={(newRegions) =>
                        {
                            setContent(prevContent => ({
                                ...prevContent,
                                regions: [...prevContent.regions, ...newRegions]
                            }));
                        }}
                        placeholder="Search regions..."
                        allowMultiple={true}
                        title="Add Regions"
                    />
                )}
            </Expandable>

            {/* Abstract */}
            <Expandable title="Abstract" collapsedHeight={editMode ? 1000 : 100} hidden={!(editMode || content.abstract)} defaultOpen={editMode}>
                {!editMode && (<span className="text-xs">{content.abstract}</span>)}
            
                {editMode && (
                    <Textarea rows={10} className="w-full text-xs" value={content.abstract || ""} onChange={(e) => 
                    {
                        const newValue = e.target.value;
                        setContent(prevContent => ({ ...prevContent, abstract: newValue }));
                        updateField("abstract", newValue);
                    }} />
                )}
            </Expandable>

            {/* Notes */}
            <Expandable title="Notes" collapsedHeight={editMode ? 1000 : 100} hidden={!(editMode || content.note)} defaultOpen={editMode}>
                {!editMode && (<span className="text-xs">{ content.note || <Skeleton /> }</span>)}
            
                {editMode && (
                    <Textarea rows={10} className="w-full text-xs" value={content.note || ""} onChange={(e) => 
                    {
                        const newValue = e.target.value;
                        setContent(prevContent => ({ ...prevContent, note: newValue }));
                        updateField("note", newValue);
                    }} />
                )}
                
            </Expandable>
            
            {/* Delete Button */}
            {userRole === "admin" && !trashOpen && editMode &&
            (
                <div className="mt-10 flex w-full justify-center">
                    <Button onClick={() => setConfirmDialogOpen(true)} variant="outline" className="border-red-500 text-red-500 hover:border-red-600 hover:bg-red-50 hover:text-red-600">
                        Delete Resource
                    </Button>
                </div>
            )}
        </>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)