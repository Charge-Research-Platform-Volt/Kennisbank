"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { FilterProjectDto, ProjectCreateDto, ProjectPageResponse } from "@/types/project.type";
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


export const createNewProject = async (data: ProjectCreateDto): Promise<ApiResponse> => {
  const cookieHeader = await cookies();
  try {
    const response = await fetch(`${process.env.API_URL}/Project/create`, { 
      method: 'PUT', 
      credentials: 'include',
      headers: { 
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || "" 
      },
      body: JSON.stringify(data),
    });

    const result: ApiResponse = await response.json();

    return result;

  } catch (error) {
    console.error("Error creating new project in action:", error);
    if (error instanceof Error) {
        return { success: false, message: error.message };
    }
    return { success: false, message: "An unknown error occurred while creating the project." };
  }
};


/**
 * Creates a new folder within a parent project or folder
 * @param folderName Name of the new folder
 * @param parentId ID of the parent project or folder
 * @returns API response with the created folder ID
 */
export async function createFolder(folderName: string, parentId: string): Promise<ApiResponse> {
  const cookieHeader = await cookies();
  try {
    // Call the backend PUT endpoint to create a folder
    const response = await fetch(`${process.env.API_URL}/Project/add-folder/${encodeURIComponent(folderName)}/${encodeURIComponent(parentId)}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || "" 
      },
    });

    const data = await response.json();
    
    if (!response.ok) {
      // Return error response
      return {
        success: false,
        message: data.message || `Failed to create folder. Status: ${response.status}`,
      };
    }

    // Return success response with folder ID
    return {
      success: true,
      message: 'Folder created successfully',
      body: { folderId: data.body },
    };
  } catch (error) {
    console.error('Error creating folder:', error);
    return {
      success: false,
      message: error instanceof Error ? error.message : 'An unexpected error occurred',
    };
  }
}