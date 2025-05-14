"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

type resourceProperty = "title" | "description" | 
                        "language-code" | "publication-code" | 
                        "publication-date" | "creation-date" |
                        "license" | "note";

type personProperty = "";

type organisationProperty = "";

type resourceRelation = "author" | "organisation" |
                        "region" | "relatedOrganisation" |
                        "relatedSource" | "source" |
                        "tag"; 

export const getProperty = async (
    id: string,
    type: string,
    property: resourceProperty | personProperty | organisationProperty,
) : Promise<ApiResponse> => {
    
    const endPoint = "";
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `/api/${endPoint}`,
        {
            method: "GET",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
            body: JSON.stringify({id: id, type: type, property: property}),
        },
    );

    if (!response.ok) {
        throw new Error(`Problem with finding property: ${property}`);
    }
    
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);
    return data;
}

export const getRelation = async (
    id: string,
    type: string,
    relation: resourceRelation,
) => {

    const endPoint = "";
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `/api/${endPoint}`,
            {
                method: "GET",
                credentials: "include",
                headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
                body: JSON.stringify({id: id, type: type, relation: relation}),
            },
    );
    
    if (!response.ok) {
        throw new Error(`Problem with finding relation: ${relation}`);
    }
        
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);
    return data;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

