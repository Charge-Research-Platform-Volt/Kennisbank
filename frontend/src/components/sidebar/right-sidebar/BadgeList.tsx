"use client"

import React from "react";
import { Badge } from "@/components/ui/badge"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'
import New from "@/icons/new"
import NewBadge from "./NewBadge"
import { useArchive } from "@/context/archive-provider"
import { organisationRelation, personRelation, resourceRelation } from "@/actions/right-sidebarActions";

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
    emptyMessage: string;
    itemList: ListItem[] | null;
    onUpdate: () => void;
}

const skeletonList = [1,2,3,4,5,6,7,8]


export default function BadgeList({
    variant = "outline",
    className = "p-2 select-none",
    listType, //can be overridden, is used to identify what relation you can add with the plus button
    emptyMessage = "None found",
    itemList,
    onUpdate,
} : BadgeListProps)
{
    const { navigate } = useSidebar();
    const { setTagFilters } = useArchive();
    
    async function navigateTo(id: string, type: string)
    {
        if (type == "person" || type == "organisation" || type == "resource")
        {
            const nType: MetadataTypeEnum = type as MetadataTypeEnum;
            navigate(id, nType)
        }
        else if (type == "tag")
        {
            // Add here: apply tag filter on archive
            setTagFilters([id]);
        }
        else if (type = "source")
        {
            window.open(id)?.focus();
        }
    }

    function addBadge()
    {

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
                        <Badge key={index} onClick={() => navigateTo(item.id, item.type)} variant={variant} className={className}>{item.name}</Badge>
                    ))}
                    <NewBadge relation={listType} onUpdate={onUpdate} alreadyRelated={itemList}/>
                </>
            ) : (
                <NewBadge relation={listType} onUpdate={onUpdate}  alreadyRelated={[]}/>
                
            )}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
