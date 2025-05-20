"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { FilterProjectDto, ProjectPageResponse } from "@/types/project.type";
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
    console.log("Getting tags paged:");
    
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