"use client"

import LoremIpsum from "@/utils/lorem-ipsum"
import Expandable from "./expandable"
import BadgeList from "./BadgeList"
import { Badge } from "@/components/ui/badge"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import { getProperty, getRelation } from "@/actions/right-sidebarActions"
import { ApiResponse } from "@/types/apiResponse.type"
import { useState, use, useEffect } from "react"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'


export function ResourceContent()
{
    const { currentId } = useSidebar();
    const [ title, setTitle ] = useState<string | null>(null);
    const [ description, setDescription ] = useState<string | null>(null);

    useEffect(() => {
        loadProperties()
    }, [])

    const loadProperties = async () => {

    const titlePromise = getProperty(currentId, MetadataTypeEnum.RESOURCE, "title");
    const descriptionPromise = getProperty(currentId, MetadataTypeEnum.RESOURCE, "description");
    
    titlePromise.then(response => {
        setTitle(response.body);
    }).catch(error => {
        console.error("Error loading title:", error);
    });
    
    descriptionPromise.then(response => {
        setDescription(response.body);
    }).catch(error => {
        console.error("Error loading description:", error);
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
                    {id: "9a224808-f595-4aaa-9c87-48c5db3235e0", label: "test", type: MetadataTypeEnum.RESOURCE},
                    {id: "i2", label: "author2", type: MetadataTypeEnum.PERSON},
                    {id: "i3", label: "author3", type: MetadataTypeEnum.PERSON},
                    {id: "i4", label: "author4", type: MetadataTypeEnum.PERSON},
                    {id: "i5", label: "author5", type: MetadataTypeEnum.PERSON},
                    {id: "i6", label: "author6", type: MetadataTypeEnum.PERSON},
                    {id: "i7", label: "author7", type: MetadataTypeEnum.PERSON},
                    {id: "i8", label: "author8", type: MetadataTypeEnum.PERSON},
                    {id: "i9", label: "author9", type: MetadataTypeEnum.PERSON},
                    {id: "i10", label: "author10", type: MetadataTypeEnum.PERSON},
                    {id: "i11", label: "author11", type: MetadataTypeEnum.PERSON},
                    {id: "i12", label: "author12", type: MetadataTypeEnum.PERSON},
                    {id: "i13", label: "author13", type: MetadataTypeEnum.PERSON},
                    {id: "i14", label: "author14", type: MetadataTypeEnum.PERSON},
                    {id: "i15", label: "author15", type: MetadataTypeEnum.PERSON},
                    {id: "i16", label: "author16", type: MetadataTypeEnum.PERSON},
                    {id: "i17", label: "author17", type: MetadataTypeEnum.PERSON},
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
