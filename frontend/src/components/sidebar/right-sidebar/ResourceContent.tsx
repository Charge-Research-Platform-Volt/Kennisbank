"use client"

import LoremIpsum from "@/utils/lorem-ipsum"
import Expandable from "./expandable"
import BadgeList from "./BadgeList"
import { ListItem } from "./BadgeList"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import { getProperties, getRelatedDocuments, getRelation } from "@/actions/right-sidebarActions"
import { useState, use, useEffect } from "react"
import Skeleton from 'react-loading-skeleton'
import 'react-loading-skeleton/dist/skeleton.css'
import ResourceList from "./ResourceList"


export function ResourceContent()
{
    const { currentId, rightSidebarOpen } = useSidebar();
    const [ fileType, setFileType] = useState<string | null>(null);
    const [ title, setTitle ] = useState<string | null>(null);
    const [ url, setUrl ] = useState<string | undefined>(undefined);
    const [ description, setDescription ] = useState<string | null>(null);
    const [ note, setNote ] = useState<string | null>(null);
    const [ authors, setAuthors ] = useState<ListItem[] | null>(null);
    const [ tags, setTags ] = useState<ListItem[] | null>(null);
    const [ organisations, setOrganisations ] = useState<ListItem[] | null>(null);
    const [ relatedOrganisations, setRelatedOrganisations ] = useState<ListItem[] | null>(null);
    const [ relatedPersons, setRelatedPersons ] = useState<ListItem[] | null>(null);
    const [ relatedResources, setRelatedResources ] = useState<ListItem[] | null>(null);
    const [ sourceList, setSourceList ] = useState<ListItem[] | null>(null);
    const [ regions, setRegions ] = useState<ListItem[] | null>(null);



    useEffect(() => {
        if (rightSidebarOpen) {
        setFileType(null);
        setUrl(undefined);
        setTitle(null);
        setDescription(null);
        setNote(null);
        setAuthors(null);
        setTags(null);
        setOrganisations(null);
        setRelatedOrganisations(null);
        setRelatedPersons(null);
        setRelatedResources(null);
        setSourceList(null);
        setRegions(null);
        loadInformation(); }
    }, [currentId])

    const loadInformation = async () => {
        const infoPromise = getProperties(currentId, MetadataTypeEnum.RESOURCE);
        const authorsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "author");
        const tagsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "tag");
        const organisationsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "organisation");
        const relatedOrganisationsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "relatedOrganisation");
        const relatedPersonsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "relatedPerson");
        const relatedResourcesPromise = getRelatedDocuments(currentId, 10);
        const sourceListPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "source");
        const regionsPromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "region");
    
        infoPromise.then(response => {
            setFileType(response.body.fileType);
            if (response.body.fileType === "website") {
                const websitePromise = getRelation(currentId, MetadataTypeEnum.RESOURCE, "website");
                
                websitePromise.then(response => {
                    setUrl(response.body[0].url);
                }).catch(error => {console.error("Error loading url: ", error);})
            }
            setTitle(response.body.title);
            if (response.body.description) {
                setDescription(response.body.description);
            }
            else {setDescription("No description.")}
            if (response.body.note) {
                setNote(response.body.note);
            }

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

        organisationsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "organisation",
            }))
            setOrganisations(list);
        }).catch(error => {
            console.error("Error loading organisations: ", error);
        });

        relatedPersonsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "person",
            }))
            setRelatedPersons(list);
        }).catch(error => {
            console.error("Error loading related persons: ", error);
        });
        
        relatedOrganisationsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; name: any }) => ({
                id: item.id,
                name: item.name,
                type: "organisation",
            }))
            setRelatedOrganisations(list);
        }).catch(error => {
            console.error("Error loading related organisations: ", error);
        });

        relatedResourcesPromise.then(response => {
            const list: ListItem[] = response.body.map((item: { id: any; title: any;  }) => ({
                id: item.id,
                name: item.title,
                type: "pdf",
            }))
            setRelatedResources(list);
        }).catch(error => {
            console.error("Error loading related resources: ", error);
        });

        sourceListPromise.then(response => {
            const list: ListItem[] = response.body.map((item: {resourceid: any; url: any}) => ({
                id: item.url,
                name: item.url,
                type: "source",
            }))
            setSourceList(list);
        }).catch(error => {
            console.error("Error loading sources: ", error);
        });

        regionsPromise.then(response => {
            const list: ListItem[] = response.body.map((item: {id: any; name: any}) => ({
                id: item.id,
                name: item.name,
                type: "region",
            }))
            setRegions(list);
        }).catch(error => {
            console.error("Error loading regions: ", error);
        });
    }
    

    return (
        <>
            {fileType === "website" ? (
                <a href={url} className="select-none" target="_blank" rel="noreferror">
                    <h1 className="pb-2 font-bold select-none">{title || <Skeleton />}</h1>
                    <h1 className="p-1 pl-2 mb-2 bg-gray-200 rounded-md select-none">{url || <Skeleton />}</h1>
                </a>

            ) : ( <h1 className="pb-2 font-bold select-none">{title || <Skeleton />}</h1> )}
        
            <Expandable title="Description" collapsedHeight={100}>
                {description || <Skeleton />}
            </Expandable>
            
            <Expandable variant="horizontal" title="Tags">
                <BadgeList listType="tag" emptyMessage={"No Tags recorded"} itemList={tags}/>
            </Expandable>

            <Expandable variant="horizontal" title="Authors">
                <BadgeList listType="author" emptyMessage={"No Authors recorded"} itemList={authors}/>
            </Expandable>
            
            <Expandable variant="horizontal" title="Organisations">
                <BadgeList listType="organisation" emptyMessage={"No Organisations recorded"} itemList={organisations}/>
            </Expandable>

            <Expandable variant="horizontal" title="Related People">
                <BadgeList listType="relatedPerson" emptyMessage={"No related persons recorded"} itemList={relatedPersons}/>
            </Expandable>
            
            <Expandable variant="horizontal" title="Related Organisations">
                <BadgeList listType="relatedOrganisation" emptyMessage={"No related organisations recorded"} itemList={relatedOrganisations}/>
            </Expandable>

            <ResourceList header="Related" resources={relatedResources}/>

            <Expandable variant="horizontal" title="Sources">
                <BadgeList listType="source" emptyMessage={"No sources recorded"} itemList={sourceList}/>
            </Expandable>

            <Expandable variant="horizontal" title="Regions">
                <BadgeList listType="regiom" emptyMessage={"No regions recorded"} itemList={regions}/>
            </Expandable>

            {/* {resourceType === "Scientific Article" && (
                <Expandable title="Abstract" collapsedHeight={100}>
                    {<>insert abstract</> || <Skeleton />}
                </Expandable> 
            )} */}

            <Expandable title="Notes" collapsedHeight={100}>
                {note || <Skeleton />}
            </Expandable>            
            
            {fileType}
        </>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
