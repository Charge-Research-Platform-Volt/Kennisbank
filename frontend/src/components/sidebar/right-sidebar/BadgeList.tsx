"use client"

import React from "react";
import { Badge } from "@/components/ui/badge"
import { useSidebar, MetadataTypeEnum, FetchMetadataType } from "@/context/sidebar-provider"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'
import NewBadge from "./NewBadge"
import { useArchive } from "@/context/archive-provider"
import { organisationRelation, personRelation, resourceRelation, removeRelation, tryAddNewTag, addRelation } from "@/actions/right-sidebarActions";
import { ContextMenu, ContextMenuTrigger, ContextMenuContent, ContextMenuItem } from "@/components/ui/context-menu";
import { usePathname } from "next/navigation";

export interface ListItem
{
    id: string,
    name: string,
    type: string, //if "tag" it applies the tag as filter, if "source" it opens the url in a new tab, else it navigates to selected source,organisation,person
}

interface BadgeListProps
{
    variant?: "outline" | "default" | "secondary" | "destructive";
    className?: string;
    listType: resourceRelation | personRelation | organisationRelation;
    itemList: ListItem[] | null;
    onNew: (newItems: ListItem[]) => void;
    onRemove: (removedItem: ListItem) => void;
}

const skeletonList = [1,2,3,4,5,6,7,8]


export default function BadgeList({
    variant = "outline",
    className = "p-2 select-none",
    listType, //can be overridden, is used to identify what relation you can add with the plus button
    itemList,
    onNew,
    onRemove
} : BadgeListProps)
{
    const { navigate, currentId, currentType } = useSidebar();
    const { setTagFilter, setTypeFilter, setRegionFilter } = useArchive();
    const pathname: string = usePathname();
    
    async function navigateTo(id: string)
    {
        if (["authors"].includes(listType)) 
        {
            // Authors can be either persons or organisations, so fetch the type first
            navigate(id, await FetchMetadataType(id));
        }
        else if ( ["related-persons", "person-related-persons", "persons"].includes(listType))
        {
            navigate(id, MetadataTypeEnum.PERSON);
        }
        else if ( ["resource-related-resource", "related-resources", "authored-resources", "direct-resources", "related-resources"].includes(listType))
        {
            navigate(id, MetadataTypeEnum.RESOURCE);
        }
        else if ( ["organisations", "related-organisations", "organisation-related-organisations"].includes(listType))
        {
            navigate(id, MetadataTypeEnum.ORGANISATION);
        }
        else if (listType == "tags")
        {
            if (pathname != `/archive`) {
                const params = new URLSearchParams();
                params.set('listType', listType);
                params.set('id', id);
                
                window.location.href = `${window.location.origin}/archive?${params.toString()}`;
            }
            else {
                setTypeFilter(['resource']);
                setTagFilter([id]);
            }
        }
        else if (listType == "regions")
        {
            if (pathname != `/archive`) {
                const params = new URLSearchParams();
                params.set('listType', listType);
                params.set('id', id);
                
                window.location.href = `${window.location.origin}/archive?${params.toString()}`;
            }
            else {
                setTypeFilter(['resource']);
                setRegionFilter([id]);
            }
        }
        else if (["sources", "related-sources"].includes(listType))
        {
            window.open(id)?.focus();
        }
    }

    async function handleRemove(item: ListItem) {
        try {
            
            await removeRelation(listType, currentType, currentId, item.id) 
            onRemove(item);

        } catch (error) {
            console.error("Error adding relations:", error);
        }
    }

    async function handleAiAdd(item: ListItem) {
        try {
            const tagid = await tryAddNewTag(item.name);
            await addRelation("tags", currentType, currentId, tagid)
            onNew([item]);
        }
        catch (error) {
            console.error("Error adding recommended tag:", error)
        }
    }

    return (
        <>
            {itemList === null ? (
                <>
                    {skeletonList.map((item, index) =>(
                        <Badge key={index} variant={variant} className={className}><Skeleton width={50}/></Badge>
                    ))}
                </>
            ) : itemList.length > 0 ? (
                <>
                    {itemList.map((item, index) =>(
                        <ContextMenu key={index}>
                            <ContextMenuTrigger>
                                <Badge onClick={() => { navigateTo(item.id); }} variant={variant} className={`max-w-96 truncate inline-block justify-start ${className}`} title={item.name}>{item.name}</Badge>
                            </ContextMenuTrigger>
                            <ContextMenuContent className="select-none">
                                {listType != "ai-tags" ? (
                                    <ContextMenuItem className="select-none text-red-600" onClick={() => handleRemove(item)}>
                                        <div className="select-none cursor-pointer">Remove relation</div>
                                    </ContextMenuItem>
                                ) : (
                                    <ContextMenuItem className="select-none text-[#502379]" onClick={() => handleAiAdd(item)}>
                                        <div className="select-none cursor-pointer">Add tag</div>
                                    </ContextMenuItem>
                                )}
                                
                            </ContextMenuContent>
                        </ContextMenu>
                    ))}
                    {listType != "ai-tags" && (
                        <NewBadge relation={listType} onNew={onNew} alreadyRelated={itemList}/>
                    )}
                    
                </>
            ) : (
                <>
                {listType != "ai-tags" && (
                    <NewBadge relation={listType} onNew={onNew} alreadyRelated={itemList}/>
                )}
                </>
            )}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
