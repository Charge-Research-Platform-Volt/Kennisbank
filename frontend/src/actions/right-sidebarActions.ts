"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

type resourceRelation = "author" | "organisation" |
                        "region" | "relatedOrganisation" |
                        "relatedSource" | "source" |
                        "tag"; 

export const getProperties = async (
    id: string,
    type: string,
) : Promise<ApiResponse> => {
    
    let endPoint;

    if (type === "resource") {endPoint = "resources/info"}
    else if (type === "person") {endPoint = "persons/info"}
    else if (type === "organisation") {endPoint = "organisation/info"}

    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `/api/${endPoint}/${id}`,
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
    relation: resourceRelation,
) => {

    let endPoint;
    if (type === "resource") {endPoint = "resources/relation"}
    else if (type === "person") {endPoint = "persons/..."}
    else if (type === "organisation") {endPoint = "organisation/..."}

    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `/api/${endPoint}`,
            {
                method: "GET",
                credentials: "include",
                headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
                body: JSON.stringify({id: id, relation: relation}),
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

