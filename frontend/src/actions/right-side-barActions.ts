import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";

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
    
    const response = await fetch(
        `${process.env.API_URL}/`,
        {
            method: "GET",
            credentials: "include",
            headers: { "Content-Type": "application/json"},
            body: JSON.stringify({id, type, property}),
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

    const response = await fetch(
        `${process.env.API_URL}/`,
            {
                method: "GET",
                credentials: "include",
                headers: { "Content-Type": "application/json"},
                body: JSON.stringify({id, type, relation}),
            },
    );
    
    if (!response.ok) {
        throw new Error(`Problem with finding relation: ${relation}`);
    }
        
    const rawData = await response.json();
    const data = ApiResponseSchema.parse(rawData);
    return data;
}