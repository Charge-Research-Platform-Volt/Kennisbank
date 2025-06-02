"use client"

import React from "react";
import { Badge } from "@/components/ui/badge"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'
import NewBadge from "./NewBadge"
import { useArchive } from "@/context/archive-provider"
import { organisationRelation, personRelation, resourceRelation, removeRelation, tryAddNewTag, addRelation } from "@/actions/right-sidebarActions";
import { ContextMenu, ContextMenuTrigger, ContextMenuContent, ContextMenuItem } from "@/components/ui/context-menu";

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
    onUpdate: () => void;
}

const skeletonList = [1,2,3,4,5,6,7,8]


export default function BadgeList({
    variant = "outline",
    className = "p-2 select-none",
    listType, //can be overridden, is used to identify what relation you can add with the plus button
    itemList,
    onUpdate,
} : BadgeListProps)
{
    const { navigate, currentId, currentType } = useSidebar();
    const { setTagFilter, setTypeFilter } = useArchive();
    
    async function navigateTo(id: string, type: string)
    {
        if (type == "person" || type == "organisation" || type == "resource")
        {
            const nType: MetadataTypeEnum = type as MetadataTypeEnum;
            navigate(id, nType)
        }
        else if (type == "tag")
        {
            setTypeFilter(['resource']);
            setTagFilter([id]);
        }
        else if (type = "source")
        {
            window.open(id)?.focus();
        }
    }

    async function handleRemove(id: string, type: string) {
        try {
            
            await removeRelation(listType, currentType, currentId, id) 
            onUpdate();

        } catch (error) {
            console.error("Error adding relations:", error);
        }
    }

    async function handleAddTag(id: string, name: string) {
        try {
            let tagid = await tryAddNewTag(name);
            await addRelation("tags", currentType, currentId, tagid)
            onUpdate();
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
                                <Badge onClick={() => { navigateTo(item.id, item.type); }} variant={variant} className={className}>{item.name}</Badge>
                            </ContextMenuTrigger>
                            <ContextMenuContent className="select-none">
                                {listType != "ai-tags" ? (
                                    <ContextMenuItem className="select-none text-red-600" onClick={() => handleRemove(item.id, item.type)}>
                                        <div className="select-none cursor-pointer">Remove relation</div>
                                    </ContextMenuItem>
                                ) : (
                                    <ContextMenuItem className="select-none text-[#502379]" onClick={() => handleAddTag(item.id, item.name)}>
                                        <div className="select-none cursor-pointer">Add tag</div>
                                    </ContextMenuItem>
                                )}
                                
                            </ContextMenuContent>
                        </ContextMenu>
                    ))}
                    {listType != "ai-tags" && (
                        <NewBadge relation={listType} onUpdate={onUpdate} alreadyRelated={itemList}/>
                    )}
                    
                </>
            ) : (
                <>
                {listType != "ai-tags" && (
                    <NewBadge relation={listType} onUpdate={onUpdate} alreadyRelated={itemList}/>
                )}
                </>
            )}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
