"use client"

import { useSidebar } from "@/context/sidebar-provider"

export function OrganisationContent() 
{
    const { currentId } = useSidebar();

    return (
        <>
            <h1>Organisation</h1>
            <p>{currentId}</p>
        </>
    )
}