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
    const { currentId } = useSidebar();
    const [ title, setTitle ] = useState<string | null>(null);
    const [ description, setDescription ] = useState<string | null>(null);
    const [ authors, setAuthors ] = useState<ListItem[] | null>(null);

    useEffect(() => {
        loadInformation()
    }, [])

    const loadInformation = async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.RESOURCE);
        const authorsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "author");
    
        infoPromise.then(response => {
            setTitle(response.body.title);
            setDescription(response.body.description)
        }).catch(error => {
            console.error("Error loading information: ", error);
        });

        authorsPromise.then(response => {

            setAuthors(response.body);
        }).catch(error => {
            console.error("Error loading authors: ", error);
        });
    }
    

    return (
        <>
            <h1 className="pb-2 font-bold">{title || <Skeleton />}</h1>
        
            <Expandable title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Tags">
                <Badge variant="outline" className="p-2">Tag1</Badge>
                <Badge variant="outline" className="p-2">Tag2</Badge>
                <Badge variant="outline" className="p-2">Tag3</Badge>
                <Badge variant="outline" className="p-2">Tag4</Badge>
                <Badge variant="outline" className="p-2">Tag5</Badge>
                <Badge variant="outline" className="p-2">Tag6</Badge>
                <Badge variant="outline" className="p-2">Tag7</Badge>
                <Badge variant="outline" className="p-2">Tag8</Badge>
                <Badge variant="outline" className="p-2">Tag9</Badge>
                <Badge variant="outline" className="p-2">Tag10</Badge>
                <Badge variant="outline" className="p-2">Tag11</Badge>
                <Badge variant="outline" className="p-2">Tag12</Badge>
                <Badge variant="outline" className="p-2">Tag13</Badge>
                <Badge variant="outline" className="p-2">Tag14</Badge>
                <Badge variant="outline" className="p-2">Tag15</Badge>
                <Badge variant="outline" className="p-2">Tag16</Badge>
                <Badge variant="outline" className="p-2">Tag17</Badge>
                <Badge variant="outline" className="p-2">Tag18</Badge>
                <Badge variant="outline" className="p-2">Tag19</Badge>
                <Badge variant="outline" className="p-2">Tag20</Badge>
            </Expandable>

            <Expandable variant="horizontal" title="Authors">
                <BadgeList emptyMessage={"No Authors recorded"} itemList={[
                    {id: "9a224808-f595-4aaa-9c87-48c5db3235e0", name: "test", type: MetadataTypeEnum.RESOURCE},
                    {id: "i2", name: "author2", type: MetadataTypeEnum.PERSON},
                    {id: "i3", name: "author3", type: MetadataTypeEnum.PERSON},
                    {id: "i4", name: "author4", type: MetadataTypeEnum.PERSON},
                    {id: "i5", name: "author5", type: MetadataTypeEnum.PERSON},
                    {id: "i6", name: "author6", type: MetadataTypeEnum.PERSON},
                    {id: "i7", name: "author7", type: MetadataTypeEnum.PERSON},
                    {id: "i8", name: "author8", type: MetadataTypeEnum.PERSON},
                    {id: "i9", name: "author9", type: MetadataTypeEnum.PERSON},
                    {id: "i10", name: "author10", type: MetadataTypeEnum.PERSON},
                    {id: "i11", name: "author11", type: MetadataTypeEnum.PERSON},
                    {id: "i12", name: "author12", type: MetadataTypeEnum.PERSON},
                    {id: "i13", name: "author13", type: MetadataTypeEnum.PERSON},
                    {id: "i14", name: "author14", type: MetadataTypeEnum.PERSON},
                    {id: "i15", name: "author15", type: MetadataTypeEnum.PERSON},
                    {id: "i16", name: "author16", type: MetadataTypeEnum.PERSON},
                    {id: "i17", name: "author17", type: MetadataTypeEnum.PERSON},
                ]}/>
            </Expandable>

            <Expandable variant="horizontal" title="Authors">
                <BadgeList emptyMessage={"No Authors recorded"} itemList={[]}/>
            </Expandable>
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
