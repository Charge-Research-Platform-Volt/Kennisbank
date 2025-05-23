"use client";

import { useArchive } from "@/context/archive-provider";
import ResourcesGrid, { RowItem } from "@/components/archive/resources-grid";
import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { useState, useEffect } from "react";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import FilterButton from "@/components/archive/filter-button";

type filterDto = 
{
    tagFilters: string[];
    startDate?: Date;
    endDate?: Date;
    archived?: boolean;
}

function ResourcesLoader() {
    const [items, setItems] = useState<RowItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);

    async function fetchResources(pageIndex: number = 1, pageSize: number = 500): Promise<RowItem[]> 
    {
        try {
            const response = await fetch(`/api/resources/grid?pageIndex=${pageIndex}&pageSize=${pageSize}`);
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const data = await response.json();
            
            return data.body;
        } catch (error) {
            console.error('Failed to fetch resources:', error);
            throw error;
        }
    }

    useEffect(() => {
        async function loadResources() {
            try {
                setLoading(true);
                setError(null);
                const fetchedItems = await fetchResources();
                setItems(fetchedItems);
            } catch (err) {
                setError(err instanceof Error ? err.message : 'Failed to load resources');
            } finally {
                setLoading(false);
            }
        }
        
        loadResources();
    }, []);
    
    if (loading) return <div className="w-full flex items-center justify-center">Loading resources...</div>;
    if (error) return <div className="w-full flex items-center justify-center">Error: {error}</div>;
    
    return <ResourcesGrid items={items} />;
}

export default function Page() {
    return (
        <>
            <div className="w-full h-[4rem] flex items-center gap-2">
                <div className="relative flex-grow">
                    {/* Search input */}
                    <Input className="peer h-10 ps-9" placeholder={"Search"} type="text" />
                    
                    {/* Search icon */}
                    <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
                        <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
                    </div>
                    
                    {/* Filter button */}
                    <FilterButton onApplyAction={(tagFilters, startDate, endDate) => { }} />
                </div>
            </div>
            <div className="h-[calc(100vh-5rem)] w-full">
                <ResourcesLoader />  
            </div>
        </>
    )
}