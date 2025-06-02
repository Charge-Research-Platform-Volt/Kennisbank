"use server";

import { MetadataTypeEnum } from "@/context/sidebar-provider";
import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { TagCreateDto, TagFilterOptions } from "@/types/tag.type";
import { RegionCreateDto } from "@/types/uploadTypes";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";


// declare relation types for resource
export type resourceRelation = "authors" | "organisations" |
                        "regions" | "related-organisations" |
                        "related-sources" | "sources" | "ai-tags" |
                        "tags" | "related-persons" | "website" | "resource-related-resources"; 

// declare relation types for persons
export type personRelation = "authored-resources" | "related-resources" | "person-related-persons" | "organisations";

// declare relation types for organisations
export type organisationRelation = "direct-resources" | "related-resources"| "organisation-related-organisations" | "persons";

export const getProperties = async (
    id: string,
    type: string,
) : Promise<ApiResponse> => {
    
    let endPoint;

    if (type === "resource") {endPoint = "resources/info"}
    else if (type === "person") {endPoint = "persons/info"}
    else if (type === "organisation") {endPoint = "organisations/info"}

    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `${process.env.API_URL}/${endPoint}/${id}`,
        {
            method: "GET",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        },
    );

    if (!response.ok) {
        throw new Error(`Problem with getting properties`);
    }
    
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);
    return data;
}

export const getRelation = async (
    id: string,
    type: string,
    relation: resourceRelation | personRelation | organisationRelation,
) => {

    // get endpoint based on the type (resource,person or organisation)
    let endPoint;

    const properties =
    {
        "authors": "PersonId as id, Person.Name as name",
        "authored-resources": "ResourceId as id,Resource.Title as name",
        "direct-resources": "ResourceId as id,Resource.Title as name",
        "organisations": "OrganisationId as id,Organisation.Name as name",
        "organisation-related-organisations": "TargetOrganisationId as targetid,TargetOrganisation.Name as targetname,SourceOrganisationId as sourceid,SourceOrganisation.Name as sourcename",
        "persons": "PersonId as id,Person.Name as name",
        "person-related-persons": "TargetPersonId as targetid,TargetPerson.Name as targetname,SourcePersonId as sourceid,SourcePerson.Name as sourcename",
        "regions": "RegionId as id,Region.Name as name",
        "related-organisations": "OrganisationId as id,Organisation.Name as name",
        "related-persons": "PersonId as id,Person.Name as name",
        "related-resources": "ResourceId as id,Resource.Title as name",
        "resource-related-resources": "Id as id,Title,FileType as fileType",
        "related-sources": "Url as id,Url as name",
        "sources": "Url as id,Url as name",
        "tags": "TagId as id,Tag.Name as name",
        "website": "Url as id,Url as name",
        "ai-tags": "",
    }[relation];

    const cookieHeader : ReadonlyRequestCookies = await cookies();
    let fetchContents: any = {
                method: "GET",
                credentials: "include",
                headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
            }

    
    if (relation === "ai-tags") {
        endPoint = `ai/generate-tags`;
        fetchContents = {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
            body: { id: id}
        }
    } 
    else if (type === "resource") {endPoint = `resources/${encodeURIComponent(id)}/relations/${encodeURIComponent(relation)}?properties=${encodeURIComponent(properties)}`}
    else if (type === "person") {endPoint = `persons/${encodeURIComponent(id)}/relations/${encodeURIComponent(relation)}?properties=${encodeURIComponent(properties)}`}
    else if (type === "organisation") {endPoint = `organisations/${encodeURIComponent(id)}/relations/${encodeURIComponent(relation)}?properties=${encodeURIComponent(properties)}`}
    
    
    const response = await fetch(
        `${process.env.API_URL}/${endPoint}`, //Take only Id and Name from relation
            fetchContents,
    );
    
    if (!response.ok) {
        console.log(response);
        throw new Error(`Problem with finding relation: ${relation}`);
    }
        
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);
    return data;
}

export const newRelationSearchResults = async (
    searchQuery: string,
    type: resourceRelation | personRelation | organisationRelation,
    K: number = 6,
) => {
    let endPoint;
    let path: string = "";

    //Set up tagfilter options to get tags when searching
    const tagFilterOptions : TagFilterOptions = {
        usePaging: true,
        pageIndex: 1,
        pageSize: K,
        searchQuery: searchQuery,
        onlyOwnedByCurrentUser: false,
        includeUsageCount: true,
        includeCanEditAndDelete: false,
        sortDescending: false,
      }

    const cookieHeader : ReadonlyRequestCookies = await cookies();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    let fetchContents: any;

    // switch fetchContents and endpoint depending on what relation is being sought
    if (type === "tags") {
    endPoint = 'Tags/tags';
    path = '/tags';
    fetchContents = {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        body: JSON.stringify(tagFilterOptions),
    };
    }
    else if (type === "authors" || type === "related-persons" || type === "persons" || type === "person-related-persons") {
        endPoint = `persons/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Name")}`;
        path = '/list';
        fetchContents = {
            method: "Get",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        };
    }
    else if (type === "related-organisations" || type === "organisations" || type === "organisation-related-organisations") {
        endPoint = `organisations/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Name")}`;
        path = '/list';
        fetchContents = {
            method: "Get",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        };
    }
    else if (type === "regions") {
        endPoint = `regions/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Name")}`;
        path = '/list';
        fetchContents = {
            method: "Get",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        };
    }
    else if (type === "authored-resources" || type === "related-resources" || type === "direct-resources") {
        endPoint = `resources/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Title as name")}`;
        path = '/list';
        fetchContents = {
            method: "Get",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        };
    }
    

    const response = await fetch(
      `${process.env.API_URL}/${endPoint}`,
      fetchContents,
    );

    if (!response.ok) {
      throw new Error("Problem getting results");
    }
  
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);
  
    revalidatePath(path);
  
    // Check if the request was succesful, if not, return an error
    return data;
}

export const addRelation = async (
    relation: resourceRelation | personRelation | organisationRelation,
    type: MetadataTypeEnum,
    id: string,
    targetId: string,
) => {
    let endPoint;
    const cookieHeader : ReadonlyRequestCookies = await cookies();

    if (type === "resource") {
        endPoint = `Resources/${encodeURIComponent(id)}/relations/add/${encodeURIComponent(relation)}/${encodeURIComponent(targetId)}`
    }

    else if (type === "person") {
        endPoint = `Persons/${encodeURIComponent(id)}/relations/add/${encodeURIComponent(relation)}/${encodeURIComponent(targetId)}`
    }

    else if (type === "organisation") {
        endPoint = `Organisations/${encodeURIComponent(id)}/relations/add/${encodeURIComponent(relation)}/${encodeURIComponent(targetId)}`
    }


    const fullUrl = `${process.env.API_URL}/${endPoint}`;
    
    
    const response = await fetch(fullUrl, {
        method: "GET",
        credentials: "include",
        headers: { 
            "Content-Type": "application/json", 
            Cookie: cookieHeader.toString() || "" 
        },
    });
    
    
    if (!response.ok) {
        const errorText = await response.text();
        console.error("API Error:", errorText);
        throw new Error(`Problem adding relation: ${response.status} - ${errorText}`);
    }
}

export const removeRelation = async (
    relation: resourceRelation | personRelation | organisationRelation,
    type: MetadataTypeEnum,
    id: string,
    targetId: string,
) => {
    let endPoint;
    const cookieHeader : ReadonlyRequestCookies = await cookies();

    if (type === "resource") {
        endPoint = `Resources/${encodeURIComponent(id)}/relations/remove/${encodeURIComponent(relation)}/${encodeURIComponent(targetId)}`
    }

    if (type === MetadataTypeEnum.RESOURCE) {
        endPoint = `Resources/${encodeURIComponent(id)}/relations/remove/${encodeURIComponent(relation)}/${encodeURIComponent(targetId)}`
    }

    else if (type === "person") {
        endPoint = `Persons/${encodeURIComponent(id)}/relations/remove/${encodeURIComponent(relation)}/${encodeURIComponent(targetId)}`
    }

    else if (type === "organisation") {
        endPoint = `Organisations/${encodeURIComponent(id)}/relations/remove/${encodeURIComponent(relation)}/${encodeURIComponent(targetId)}`
    }


    const fullUrl = `${process.env.API_URL}/${endPoint}`;
    
    
    const response = await fetch(fullUrl, {
        method: "GET",
        credentials: "include",
        headers: { 
            "Content-Type": "application/json", 
            Cookie: cookieHeader.toString() || "" 
        },
    });
    
    
    if (!response.ok) {
        const errorText = await response.text();
        console.error("API Error:", errorText);
        throw new Error(`Problem removing relation: ${response.status} - ${errorText}`);
    }
}

export const addNewRegion = async(
    region: string,
) => {

    const cookieHeader : ReadonlyRequestCookies = await cookies();

    const rawbody : RegionCreateDto = {
        Name: region,
    }

    const response = await fetch(
        `${process.env.API_URL}/Regions/new`,
        {
            method: "PUT",
            credentials: "include",
            headers: { "Content-Type": "application/json",
            Cookie: cookieHeader.toString() || ""  },
            body: JSON.stringify(rawbody),
        }
    )

    if (!response.ok) {
        const errorText = await response.text();
        console.error("API Error:", errorText);
        throw new Error(`Problem creating new region: ${response.status} - ${errorText}`);
    }

    
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);

    return (data.body);
}

export const tryAddNewTag = async (
    tag: string,
) => {
    
    const cookieHeader : ReadonlyRequestCookies = await cookies();

    const rawbody: TagCreateDto = {
        name: tag,
      };

    const response = await fetch(
        `${process.env.API_URL}/Regions/new`,
        {
            method: "PUT",
            credentials: "include",
            headers: { "Content-Type": "application/json",
            Cookie: cookieHeader.toString() || ""  },
            body: JSON.stringify(rawbody),
        }
    )

    if (!response.ok) {
        const errorText = await response.text();
        console.error("Tag already exists, returned id");
    }

    
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);

    return (data.body);

}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

