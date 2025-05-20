"use client"

import { useState, useEffect } from "react";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import { getProperties } from "@/actions/right-sidebarActions";
import Skeleton from 'react-loading-skeleton'
import Expandable from "./expandable"

export function PersonContent() 
{
    const { currentId, rightSidebarOpen } = useSidebar();
    const [ name, setName ] = useState<string | null>(null);  
    const [ description, setDescription ] = useState<string | null>(null);
    const [ occupation, setOccupation ] = useState<string | null>(null);

    useEffect(() => {
        if (rightSidebarOpen) {
            setName(null);
            setDescription(null);
            setOccupation(null);
        }
        loadInformation();
    }, [currentId])

    const loadInformation = async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.PERSON);
    
        infoPromise.then(response => {
            setName(response.body.name);
            if (response.body.description) {
                setDescription(response.body.description);
            }
            setOccupation(response.body.occupation)
        }).catch(error => {
            console.error("Error loading information: ", error);
        });
    }

    return (
        <>
            <h1 className="pb-2 font-bold select-none">{name || <Skeleton />}</h1>
            <h1 className="p-1 pl-2 select-none bg-gray-300 rounded-md">{occupation || <Skeleton />}</h1>
             <Expandable title="Description" collapsedHeight={100}>
                    {description || <Skeleton />}
            </Expandable>
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
