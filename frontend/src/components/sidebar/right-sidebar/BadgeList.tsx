"use client"

import React from "react";
import { Badge } from "@/components/ui/badge"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'
import New from "@/icons/new"

export interface ListItem
{
    id: string,
    name: string,
    type: string,
}

interface BadgeListProps
{
    variant?: "outline" | "default" | "secondary" | "destructive";
    className?: string;
    listType: string | null;
    emptyMessage: string;
    itemList: ListItem[] | null;
}

const skeletonList = [1,2,3,4,5,6,7,8]


export default function BadgeList({
    variant = "outline",
    className = "p-2",
    listType = null, //can be overridden, is used to identify what relation you can add with the plus button
    emptyMessage = "None found",
    itemList,
} : BadgeListProps)
{
    const { navigate } = useSidebar();
    
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
                    <Badge key={-1} onClick={() => addBadge()} variant={variant} className={className}><New className="h-5 w-5 text-black" /></Badge>
                </>
            ) : (
                <Badge key={-1} onClick={() => addBadge()} variant={variant} className={className}><New className="h-5 w-5 text-black" /></Badge>
                
            )}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
