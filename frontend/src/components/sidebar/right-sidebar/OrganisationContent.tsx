"use client"

import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import { useEffect, useState } from "react";
import { getProperties } from "@/actions/right-sidebarActions";
import Skeleton from 'react-loading-skeleton';
import Expandable from "./expandable";

export function OrganisationContent() 
{
    const { currentId, rightSidebarOpen } = useSidebar();
    const [ name, setName ] = useState<string | null>(null);
    const [ url, setUrl ] = useState<string | undefined>(undefined);
    const [ description, setDescription ] = useState<string | null>(null);


    useEffect(() => {
        if (rightSidebarOpen) {
            setName(null);
            setUrl(undefined);
            setDescription(null);
            loadInformation();
        }
    }, [currentId])
    
    const loadInformation = async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.ORGANISTATION);

        infoPromise.then(response => {
            setName(response.body.name);
            if (response.body.website) {
                setUrl(response.body.website);
            }
            else( setUrl("No Website"))
            if (response.body.description) {
                setDescription(response.body.description);
            }
            else {setDescription("No Description")}
        }).catch(error => {
            console.error("Error loading organisation information:", error);
        })
    }

    return (
        <>
            <h1 className="pb-2 font-bold select-none">{name || <Skeleton />}</h1>
            <a href={url} className="select-none" target="_blank" rel="noreferror">
                <h1 className="p-1 pl-2 mb-2 select-none bg-gray-200 rounded-md">{url || <Skeleton />}</h1>
            </a>

            <Expandable title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
