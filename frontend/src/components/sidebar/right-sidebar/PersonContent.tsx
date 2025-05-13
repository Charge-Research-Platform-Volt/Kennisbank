"use client"

import { useSidebar } from "@/context/sidebar-provider";

export function PersonContent() 
{
    const { currentId } = useSidebar();

    return (
        <>
            <h1>Person</h1>
            <p>{currentId}</p>
        </>
    )
}