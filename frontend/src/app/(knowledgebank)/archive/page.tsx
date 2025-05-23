"use server";

import { useArchive } from "@/context/archive-provider";
import ResourcesGrid, { RowItem } from "@/components/archive/resources-grid";
import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";

type filterDto = 
{
    tagFilters: string[];
    startDate?: Date;
    endDate?: Date;
    archived?: boolean;
}

export default async function Page() {

    return (
        <ResourcesGrid items={await fetchResources()} />
    );
}

async function fetchResources(pageIndex: number = 1, pageSize: number = 50): Promise<RowItem[]> 
{
    const fetch = await FetchWithValidation(ApiResponseSchema, `${process.env.API_URL}/resources/grid?pageIndex=${pageIndex}&pageSize=${pageSize}`);
    
    if (fetch.data?.success)
        return fetch.data?.body as RowItem[];
        
    return [];
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)