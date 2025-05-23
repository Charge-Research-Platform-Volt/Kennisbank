"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { TagFilterOptions } from "@/types/tag.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";


// declare relation types for resource
type resourceRelation = "authors" | "organisations" |
                        "regions" | "related-organisations" |
                        "related-sources" | "sources" |
                        "tags" | "related-persons" | "website" | "resource-related-resources"; 

// declare relation types for persons
type personRelation = "authored-resources" | "related-resources" | "person-related-persons" | "organisations";

// declare relation types for organisations
type organisationRelation = "direct-resources" | "related-resources"| "organisation-related-organisations" | "persons";

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
    if (type === "resource") {endPoint = `resources/${encodeURIComponent(id)}/relations`}
    else if (type === "person") {endPoint = `persons/${encodeURIComponent(id)}/relations`}
    else if (type === "organisation") {endPoint = `organisations/${encodeURIComponent(id)}/relations`}

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
        "resource-related-resources": "Id as id,Title as name,FileType as fileType",
        "related-sources": "Url as id,Url as name",
        "sources": "Url as id,Url as name",
        "tags": "TagId as id,Tag.Name as name",
        "website": "Url as id, Url as name"
    }[relation];
    
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `${process.env.API_URL}/${endPoint}/${encodeURIComponent(relation)}?properties=${encodeURIComponent(properties)}`, //Take only Id and Name from relation
            {
                method: "GET",
                credentials: "include",
                headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
            },
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
        includeUsageCount: false,
        includeCanEditAndDelete: false,
        sortDescending: true,
        weightedSort: "IsStandardized:2,IsApproved:1,UsageCount:0.5",
      }

    const cookieHeader : ReadonlyRequestCookies = await cookies();
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
    else if (type === "authors" || type === "related-persons" || type === "persons" ) {
        endPoint = `Persons/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Name")}`;
        path = '/list';
        fetchContents = {
            method: "Get",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        };
    }
    else if (type === "related-organisations" || type === "organisations") {
        endPoint = `Organisations/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Name")}`;
        path = '/list';
        fetchContents = {
            method: "Get",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        };
    }
    else if (type === "regions") {
        endPoint = `Regions/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Name")}`;
        path = '/list';
        fetchContents = {
            method: "Get",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
        };
    }
    else if (type === "authored-resources" || type === "related-resources" || type === "direct-resources") {
        endPoint = `Resources/list?searchQuery=${encodeURIComponent(searchQuery)}&pageIndex=${1}&pageSize=${K}&properties=${encodeURIComponent("Id,Name")}`;
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

