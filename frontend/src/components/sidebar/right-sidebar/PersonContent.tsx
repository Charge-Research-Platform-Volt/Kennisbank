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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
