"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { FilterProjectDto, ProjectPageResponse } from "@/types/project.type";
import { Resource } from "@/types/resource.type";
import { Project } from "@/types/project.type";
import type { FormResponse } from "@/types/return.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

/**
 * 
 * @param pageIndex - The page to fetch
 * @param query - Query to filter the projects with
 * @returns - A promise with the projects and the page information
 */
export const ListProjectsPaged = async (pageIndex: number, searchQuery: string): Promise<ApiResponse> => {
   
    const projectFilterOptions : FilterProjectDto = {
        usePaging: true,
        pageIndex: pageIndex,
        pageSize: 50,
        searchQuery: searchQuery
    }

    // Send the data to the backend
    const cookieHeader : ReadonlyRequestCookies = await cookies();
    const response = await fetch(
        `${process.env.API_URL}/Project/list`,
        {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
            body: JSON.stringify(projectFilterOptions),
        },
    );

    const result: ApiResponse = await response.json();
    
    return result;
}

/**
 * Fetches the content (resources and sub-projects/folders) of a specific project.
 * @param projectId - The ID of the project to fetch content for.
 * @returns An object containing resources and projects (folders).
 */
export const getProjectContentById = async (projectId: string): Promise<{ resources: Resource[], projects: Project[] }> => {
    try {
        const cookieHeader = await cookies();
        const response = await fetch(`${process.env.API_URL}/Project/info/${projectId}`, {
            method: 'GET',
            credentials: 'include', // Assuming cookies are needed for this endpoint
            headers: {
                'Content-Type': 'application/json',
                Cookie: cookieHeader.toString() || "",
            },
        });

        if (!response.ok) {
            const errorText = await response.text();
            console.error(`Failed to fetch project content: ${response.status} ${response.statusText}`, errorText);
            throw new Error(`Failed to fetch project content: ${response.status}`);
        }

        const data: ApiResponse = await response.json();
      
        if (data.success && data.body) {
            return {
                resources: data.body.resources || [],
                projects: data.body.folders || []
            };
        } else {
            console.error("Failed to retrieve project content from API response:", data.message);
            throw new Error(data.message || "Failed to retrieve project content due to API error");
        }

    } catch (error) {
        console.error("Error in getProjectContentById action:", error);
        throw error; 
    }
};