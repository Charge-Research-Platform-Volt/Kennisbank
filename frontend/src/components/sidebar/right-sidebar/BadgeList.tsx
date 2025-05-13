"use client"

import React from "react";
import { Badge } from "@/components/ui/badge"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"

interface ListItem
{
    id: string,
    label: string,
    type: MetadataTypeEnum,
}

interface BadgeListProps
{
    variant?: "outline" | "default" | "secondary" | "destructive";
    className?: string;
    emptyMessage: string;
    itemList: ListItem[];
}

export default function BadgeList({
    variant = "outline",
    className = "p-2",
    emptyMessage = "None found",
    itemList,
} : BadgeListProps)
{
    const { navigate } = useSidebar();

    return (
        <>
            {itemList.length > 0 ? (
                <>
                    {itemList.map((item, index) =>(
                        <Badge onClick={() => navigate(item.id, item.type)} variant={variant} className={className}>{item.label}</Badge>
                    ))}
                </>
            ) : (
                <div className="text-xs font-medium">{emptyMessage}</div>
            )}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
