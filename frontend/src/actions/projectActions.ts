"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { FilterProjectDto, FolderProject, ProjectCreateDto, ProjectPageResponse } from "@/types/project.type";
import { Resource, ResourceProject } from "@/types/resource.type";
import { Project } from "@/types/project.type";
import type { FormResponse } from "@/types/return.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";
import { User } from "@/types/user.type";
import { Tag } from "@/types/tag.type";

/**
 * Lists all projects with pagination and search functionality.
 * @param {number} pageIndex - The page to fetch
 * @param {string} query - Query to filter the projects with
 * 
 * @author Jelle v.h. Schut
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
 * Fetches the content (resources and sub-folders) of a specific project/folder.
 * @param {string} projectId - The ID of the project/folder to fetch content for.
 * 
 * @author Jelle v.h. Schut
 * @returns An object containing resources and projects (folders).
 */
export const getProjectContentById = async (projectId: string): Promise<{ resources: ResourceProject[], projects: FolderProject[], creators: User[], tags: Tag[] }> => {
    try {
        const cookieHeader = await cookies();
        const response = await fetch(`${process.env.API_URL}/Project/info/${projectId}`, {
            method: 'GET',
            credentials: 'include',
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
            // Map ResourceGridItemWithAddedBy[] back to ResourceProject[] for compatibility
            const resources: ResourceProject[] = (data.body.items || []).map((entry: any) => ({
                resource: {
                    id: entry.item.id,
                    title: entry.item.name,
                    description: entry.item.description ?? null,
                    fileType: entry.item.fileType,
                    typeId: entry.item.type,
                    languageCode: "",
                    publicationDate: entry.item.publicationDate ?? "",
                    creationDate: entry.item.creationDate,
                },
                addedBy: entry.addedBy ?? "",
            }));

            return {
                resources,
                projects: data.body.folders || [],
                creators: data.body.creators || [],
                tags: data.body.tags || []
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

/**
 * Creates a new project at the root level
 * @param {ProjectCreateDto} data - The data to create a new project
 * 
 * @author Jelle v.h. Schut
 * @returns API response with the created project ID
 */
export const createNewProject = async (data: ProjectCreateDto): Promise<ApiResponse> => {
  console.log(data);
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
 * @param {string} folderName Name of the new folder
 * @param {string} parentId ID of the parent project or folder
 * 
 * @author Jelle v.h. Schut
 * @returns API response with the created folder ID
 */
export async function createFolder(folderName: string, parentId: string): Promise<ApiResponse> {
  const cookieHeader = await cookies();
  try {
    // Call the backend PUT endpoint to create a folder
    const response = await fetch(`${process.env.API_URL}/Project/add-folder/${encodeURIComponent(parentId)}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || ""
      },
      body: JSON.stringify({ name: folderName }),
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

/**
 * Gets all resources
 * 
 * @author Jelle v.h. Schut
 * @returns All resources
 */
export async function fetchAllResources(): Promise<Resource[]> {
  const cookieHeader = await cookies();
  try {
    const response = await fetch(`${process.env.API_URL}/Resources/list`, {
      method: 'GET',
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || "" 
      },
    });

    const data: ApiResponse = await response.json();

    if (!response.ok) {
      const errorText = await response.text();
      console.error(`Failed to fetch project content: ${response.status} ${response.statusText}`, errorText);
      throw new Error(`Failed to fetch project content: ${response.status}`);
    }

    console.log("Fetched resources:", data.body);

    if (data.success && data.body) {
      return data.body || [];
    } else {
        console.error("Failed to retrieve project content from API response:", data.message);
        throw new Error(data.message || "Failed to retrieve project content due to API error");
    }

  } catch (error) {
        console.error("Error in getProjectContentById action:", error);
        throw error; 
    }
}

/**
 * Adds a resource to a project
 * 
 * @param {string} projectId - Id of the project to add a resource to
 * @param {string} resourceId - Id of the resource to add to the project
 * 
 * @author Jelle v.h. Schut
 * @returns api response indicating whether or not the action was successful
 */
export async function addResourceToProject(projectId: string, resourceId: string): Promise<ApiResponse> {
  const cookieHeader = await cookies();
  try {
    const response = await fetch(`${process.env.API_URL}/Project/add-item/${projectId}/${resourceId}`, {
      method: 'PUT',
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || "" 
      },
    });

    const data: ApiResponse = await response.json();

    if (!response.ok) {
      return { 
        success: false, 
        message: data.message || "Failed to add resource to project" };
    }

    return { 
      success: true,
      message: data.message || "Resource added successfully", 
      body: data.body };
  } catch (error) {
    console.error("Error adding resource to project:", error);
    return { success: false, message: error instanceof Error ? error.message : "An unexpected error occurred" };
  }
}

/**
 * Removes a resource to a project
 * 
 * @param {string} projectId - Id of the project to remove a resource from
 * @param {string} resourceId - Id of the resource to remove from the project
 * 
 * @author Jelle v.h. Schut
 * @returns api response indicating whether or not the action was successful
 */
export async function removeResourceFromProject(projectId: string, resourceId: string): Promise<ApiResponse> {
  const cookieHeader = await cookies();
  try {
    const response = await fetch(`${process.env.API_URL}/Project/remove-item/${projectId}/${resourceId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || "" 
      },
    });

    const data: ApiResponse = await response.json();

    if (!response.ok) {
      return { 
        success: false, 
        message: data.message || "Failed to remove resource from project" };
    }

    return { 
      success: true,
      message: data.message || "Resource removed successfully", 
      body: data.body };
  } catch (error) {
    console.error("Error removing resource from project:", error);
    return { success: false, message: error instanceof Error ? error.message : "An unexpected error occurred" };
  }
}

/**
 * Deletes a project
 * 
 * @param {string} projectId - Id of the project to delete
 * 
 * @author Jelle v.h. Schut
 * @returns api response indicating whether or not the action was successful
 */
export async function deleteProject(projectId: string): Promise<ApiResponse> {
  const cookieHeader = await cookies();
  try {
    const response = await fetch(`${process.env.API_URL}/Project/delete/${projectId}`, {
      method: 'DELETE',
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || "" 
      },
    });

    const data: ApiResponse = await response.json();

    if (!response.ok) {
      return { 
        success: false, 
        message: data.message || "Failed to delete project" };
    }

    return { 
      success: true,
      message: data.message || "Project deleted successfully", 
      body: data.body };
  } catch (error) {
    console.error("Error deleting project:", error);
    return { success: false, message: error instanceof Error ? error.message : "An unexpected error occurred" };
  }
}

/**
 * Updates a project
 * 
 * @param {string} projectId - Id of the project to update
 * @param {Record<string, unknown>} data - Updates to execute on the project
 * @returns api response indicating whether or not the action was successful
 */
export async function updateProject(projectId: string, data: Record<string, unknown>): Promise<ApiResponse> {
  const cookieHeader = await cookies();

  try {
    console.log("Data to update project:", data);
    const response = await fetch(`${process.env.API_URL}/Project/update/${projectId}`, {
      method: 'PATCH',
      headers: {
        'Content-Type': 'application/json',
        Cookie: cookieHeader.toString() || "" 
      },
      body: JSON.stringify(data),
    });

    const result: ApiResponse = await response.json();

    if (!response.ok) {
      return { 
        success: false, 
        message: result.message || "Failed to update project" };
    }

    return { 
      success: true,
      message: result.message || "Project updated successfully", 
      body: result.body };
  } catch (error) {
    console.error("Error updating project:", error);
    return { success: false, message: error instanceof Error ? error.message : "An unexpected error occurred" };
  }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)