
import React from "react";
import { Badge } from "@/components/ui/badge"
import New from "@/icons/new"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"


interface NewBadgeProps
{
    variant?: "outline" | "default" | "secondary" | "destructive";
    type: string | null;
}


export default function NewBadge({
    variant = "outline",
    type,
} : NewBadgeProps)
{
    const { currentId, navigate } = useSidebar();

    function addBadge () {}



    return(
                <Badge key={-1} onClick={() => addBadge()} variant={variant} style={{ width: '2.1rem', height: '2.1rem', userSelect: 'none'}} ><New style={{ width: '1.7rem', height: '1.7rem' }} className=" text-black" /></Badge>
    );
}