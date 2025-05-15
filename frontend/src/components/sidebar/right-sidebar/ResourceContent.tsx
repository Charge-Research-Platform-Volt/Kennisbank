"use client"

import LoremIpsum from "@/utils/lorem-ipsum"
import Expandable from "./expandable"
import BadgeList from "./BadgeList"
import { ListItem } from "./BadgeList"
import { Badge } from "@/components/ui/badge"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import { getProperties, getRelation } from "@/actions/right-sidebarActions"
import { ApiResponse } from "@/types/apiResponse.type"
import { useState, use, useEffect } from "react"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'


export function ResourceContent()
{
    const { currentId, navigate } = useSidebar();
    const [ title, setTitle ] = useState<string | null>(null);
    const [ description, setDescription ] = useState<string | null>(null);
    const [ authors, setAuthors ] = useState<ListItem[] | null>(null);
    const [ tags, setTags ] = useState<ListItem[] | null>(null);



    useEffect(() => {
        setTitle(null);
        setDescription(null);
        setAuthors(null);
        setTags(null);
        loadInformation();
    }, [currentId])

    const loadInformation = async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.RESOURCE);
        const authorsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "author");
        const tagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "tag");
    
        infoPromise.then(response => {
            setTitle(response.body.title);
            if (response.body.description) {
                setDescription(response.body.description)
            }

            else {setDescription("No description.")}
        }).catch(error => {
            console.error("Error loading information: ", error);
        });

        authorsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "person",
            }))
            setAuthors(list);
        }).catch(error => {
            console.error("Error loading authors: ", error);
        });

        tagsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "tag",
            }))
            setTags(list);
        }).catch(error => {
            console.error("Error loading tags: ", error);
        });
    }
    

    return (
        <>
            <h1 className="pb-2 font-bold">{title || <Skeleton />}</h1>
        
            <Expandable title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Tags">
                <BadgeList emptyMessage={"No Tags recorded"} itemList={tags}/>
            </Expandable>

            <Expandable variant="horizontal" title="Authors">
                <BadgeList emptyMessage={"No Authors recorded"} itemList={authors}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related Resources">
                <Badge onClick={() => navigate("b5c443ad-88f3-4065-836b-bb61595ba55b", MetadataTypeEnum.RESOURCE)} variant={"outline"} className={"p-2"}>Burgers gelijkwaardig aan de ontwerptafel</Badge>
            </Expandable>
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
