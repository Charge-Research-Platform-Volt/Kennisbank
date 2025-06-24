"use client";

import React from 'react';
import { usePathname, useSearchParams, useRouter, ReadonlyURLSearchParams } from 'next/navigation';
import { AppRouterInstance } from 'next/dist/shared/lib/app-router-context.shared-runtime';
import { MetadataTypeEnum } from './sidebar-provider';
import { ApiResponse } from '@/types/apiResponse.type';
import { GridRequest, GridRequestSchema } from '@/types/gridRequest.type';

export type ResourceGridItem =
{
    id: string;
    name: string;
    publicationDate: string;
    type: MetadataTypeEnum;
    fileType: string;
    creationDate: string;
    chunks?: string[];
    description: string;
    items: any;
}

export type ArchiveContextType = {

    // Filters
    resetFilters: () => void;
    tagFilter: string[];
    setTagFilter: (filter: string[]) => void;
    typeFilter: string[];
    setTypeFilter: (typeFilter: string[]) => void;
    publicationDateRangeMin: string;
    setPublicationDateRangeMin: (publicationDateRangeMin: string) => void;
    publicationDateRangeMax: string;
    setPublicationDateRangeMax: (publicationDateRangeMax: string) => void;
    regionFilter: string[];
    setRegionFilter: (filter: string[]) => void;
    
    // Search
    searchQuery: string;
    searchInput: string;
    setSearchInput: (searchInput: string) => void;
    
    // Pagination
    currentPage: number;
    goToPage: (pageIndex: number) => void;
    pageSize: number;
    setPageSize: (pageSize: number) => void;
    totalItems: number;
    setTotalItems: (totalItems: number) => void;
    totalPages: number;
    
    // States
    trashOpen: boolean;
    setTrashOpen: (open: boolean) => void;
    
    // Triggers
    gridReloadTrigger: boolean;
    triggerGridReload: () => void;

    // Router and SearchParams
    router: AppRouterInstance;
    searchParams: ReadonlyURLSearchParams;
    pathname: string;
    
    // Data
    rowData: ResourceGridItem[];
    setRowData: (data: ResourceGridItem[]) => void;
    loading: boolean;
    setLoading: (isLoading: boolean) => void;
    sortBy: string;
    setSortBy: (sortBy: string) => void;
    sortDirection: 'asc' | 'desc';
    setSortDirection: (sortDir: 'asc' | 'desc') => void;
    mode: "search-results" | "general-results";
    setMode: (mode: "search-results" | "general-results") => void;
};

const ArchiveContext = React.createContext<ArchiveContextType | undefined>(undefined);

export const useArchive = (): ArchiveContextType => {
  const context = React.useContext(ArchiveContext);

  if (!context) throw new Error("useArchive must be used within a ArchiveProvider");

  return context;
};

export const ArchiveProvider = ({ children }: { children: React.ReactNode }) => {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

    // Filters
    const [tagFilter, setTagFilter] = React.useState<string[]>(() => searchParams.getAll('tagFilter'));
    const [typeFilter, setTypeFilter] = React.useState<string[]>(() => 
    {
        const urlTypes = searchParams.getAll('typeFilter');
        return urlTypes.length > 0 ? urlTypes : ['resource', 'person', 'organisation'];
    });
    const [publicationDateRangeMin, setPublicationDateRangeMin] = React.useState<string>(() => searchParams.get('pubdateMin') || '');
    const [publicationDateRangeMax, setPublicationDateRangeMax] = React.useState<string>(() => searchParams.get('pubdateMax') || '');
    const [regionFilter, setRegionFilter] = React.useState<string[]>(() => searchParams.getAll('regionFilter'));
    
    // Search
    const [searchInput, setSearchInput] = React.useState<string>(() => searchParams.get('query') || '');
    const [searchQuery, setSearchQuery] = React.useState<string>(() => searchParams.get('query') || '');
    
    // Pagination
    const [currentPage, setCurrentPage] = React.useState<number>(() => Number(searchParams.get('page')) || 1);
    const [pageSize, setPageSize] = React.useState<number>(20);
    const [totalItems, setTotalItems] = React.useState<number>(0);
    const [totalPages, setTotalPages] = React.useState<number>(1);
    
    // States
    const [trashOpen, setTrashOpen] = React.useState<boolean>(false);
    
    // Triggers
    const [gridReloadTrigger, setGridReloadTrigger] = React.useState<boolean>(false);
    
    // Track if initial load to prevent resets
    const [isInitialLoad, setIsInitialLoad] = React.useState(true);
    
    // Data for grid
    const [rowData, setRowData] = React.useState<ResourceGridItem[]>([]);
    const [loading, setLoading] = React.useState(false);
    
    // Sort states -- Controlled by AgGrid
    const [sortBy, setSortBy] = React.useState<string>('');
    const [sortDirection, setSortDirection] = React.useState<'asc' | 'desc'>('asc');
    
    const [mode, setMode] = React.useState<"search-results" | "general-results">("general-results");
    
    // URL Management
    const syncToUrl = React.useCallback((updates: Record<string, string | string[] | undefined>) => 
    {
        const params = new URLSearchParams(searchParams.toString());
        
        Object.entries(updates).forEach(([key, value]) => 
        {
            params.delete(key);
            
            if (value !== undefined && value !== '' && !(Array.isArray(value) && value.length === 0)) 
            {
                if (Array.isArray(value))
                    value.forEach(item => params.append(key, item));
                else
                    params.set(key, String(value));
            }
        });
        
        router.replace(`${pathname}?${params.toString()}`);
    }, [searchParams, router, pathname])
    
    // Filter management
    const resetFilters = React.useCallback(() => 
    {
        const defaultTypes = ['resource', 'person', 'organisation'];
        
        setTypeFilter(defaultTypes);
        setPublicationDateRangeMax('');
        setPublicationDateRangeMin('');
        setTagFilter([]);
        setRegionFilter([]);
        
        // Sync to URL
        syncToUrl(
        {
            typeFilter: undefined,
            pubdateMin: undefined,
            pubdateMax: undefined,
            tagFilter: undefined,
            regionFilter: undefined,
            query: searchQuery || undefined,
            page: currentPage > 1 ? String(currentPage) : undefined
        });
    }, [searchQuery, currentPage, syncToUrl]);
    
    // Pagination
    const goToPage = React.useCallback((pageIndex: number) => 
    {
        if (pageIndex >= 1) 
        {
            const newPage = pageIndex <= totalPages ? pageIndex : totalPages;
            setCurrentPage(newPage);
        }
    }, [totalPages]);

    // Reload trigger
    const triggerGridReload = React.useCallback(() => 
    {
        setGridReloadTrigger(prev => !prev);
    }, []);
    
    // Debounced search query update
    React.useEffect(() => 
    {
        const timer = setTimeout(() => 
        {
            setSearchQuery(searchInput);
        }, 500);
        
        return () => clearTimeout(timer);
    }, [searchInput]);
    
    // Reset to first page when filters change (after initial load)
    React.useEffect(() => 
    {
        if (isInitialLoad) 
        {
            setIsInitialLoad(false);
            return;
        }
        
        if (currentPage !== 1)
            setCurrentPage(1);
        }, [searchQuery, typeFilter, tagFilter, publicationDateRangeMax, publicationDateRangeMin, regionFilter]); // eslint-disable-line react-hooks/exhaustive-deps
    
    // Calculate total pages on total items change
    React.useEffect(() => 
    {
        setTotalPages(Math.ceil(totalItems / pageSize));
    }, [totalItems, pageSize]);
    
    // Sync filters to URL
    React.useEffect(() => 
    {
        if (isInitialLoad) return;
        
        syncToUrl(
        {
            tagFilter: tagFilter.length > 0 ? tagFilter : undefined,
            typeFilter: typeFilter.length === 3 ? undefined : typeFilter,
            pubdateMin: publicationDateRangeMin || undefined,
            pubdateMax: publicationDateRangeMax || undefined,
            regionFilter: regionFilter.length > 0 ? regionFilter : undefined,
        });
    }, [tagFilter, typeFilter, publicationDateRangeMin, publicationDateRangeMax, regionFilter, isInitialLoad, syncToUrl]);
    
    // Sync search and pagination to URL
    React.useEffect(() => 
    {
        if (isInitialLoad) return;
        
        syncToUrl(
        {
            query: searchQuery || undefined,
            page: currentPage > 1 ? String(currentPage) : undefined
        });
    }, [searchQuery, currentPage, isInitialLoad, syncToUrl]);
    
    // Clear state when navigating away from /archive
    React.useEffect(() => 
    {
        if (pathname !== '/archive') 
        {
            // Reset all filters to defaults
            setTagFilter([]);
            setTypeFilter(['resource', 'person', 'organisation']);
            setPublicationDateRangeMin('');
            setPublicationDateRangeMax('');
            setRegionFilter([]);
            
            // Reset search
            setSearchInput('');
            setSearchQuery('');
            
            // Reset pagination
            setCurrentPage(1);
            setTotalItems(0);
            setTotalPages(1);
            
            // Reset other state
            setTrashOpen(false);
            
            // Mark as initial load for when user returns to /archive
            setIsInitialLoad(true);
        }
    }, [pathname]);
    
    // Fetch data function
    const fetchData = React.useCallback(async () => 
    {
        if (pathname !== '/archive') return;
    
        setLoading(true);
        
        try 
        {   
            // Create the request
            const request: GridRequest = GridRequestSchema.parse(
            {
                pageIndex: currentPage,
                pageSize: pageSize,
                searchQuery: searchQuery || undefined,
                sortBy: sortBy || undefined,
                sortDirection: sortDirection || undefined,
                filterOptions:
                {
                    typeFilter: typeFilter || undefined,
                    pubdateMin: publicationDateRangeMin || undefined,
                    pubdateMax: publicationDateRangeMax || undefined,
                    tagFilter: tagFilter || undefined,
                    regionFilter: regionFilter || undefined,
                },
            });
            
            // Fetch the data from the backend
            const response = trashOpen ?
                await fetch(`/api/resources/trash-grid`, 
                {
                    credentials: 'include',
                })
                :
                await fetch(`/api/resources/grid`,
                {
                    method: 'POST',
                    credentials: 'include',
                    headers: 
                    {
                        'Content-Type': 'application/json',
                    },
                    body: JSON.stringify(request),
                });
            
            if (!response.ok)
                throw new Error(`HTTP error! Status: ${response.status}`);
                
            const data: ApiResponse = await response.json();
            
            if (data.success) 
            {
                setRowData(trashOpen ? data.body : data.body.items);
                setTotalItems(trashOpen ? data.body.length : data.body.totalCount);
            }
            else 
            {
                console.error('API returned error:', data.message);
                setRowData([]);
                setTotalItems(0);
            }
        }
        catch (error) 
        {
            console.error('Failed to fetch data:', error);
            setRowData([]);
            setTotalItems(0);
        }
        finally 
        {
            setLoading(false);
        }
    }, [
        currentPage,
        pageSize,
        searchQuery,
        typeFilter,
        publicationDateRangeMax,
        publicationDateRangeMin,
        sortBy,
        sortDirection,
        tagFilter,
        regionFilter,
        setTotalItems,
        trashOpen,
        pathname
    ]);
    
    // Fetch data when dependencies change or when trigger is activated
    React.useEffect(() => 
    {
        fetchData();
    }, [fetchData, gridReloadTrigger]);
    
    return (
        <ArchiveContext.Provider 
            value={{
                // Filters
                resetFilters,
                tagFilter,
                setTagFilter,
                typeFilter,
                setTypeFilter,
                publicationDateRangeMin,
                setPublicationDateRangeMin,
                publicationDateRangeMax,
                setPublicationDateRangeMax,
                regionFilter,
                setRegionFilter,
                
                // Search
                searchQuery,
                searchInput,
                setSearchInput,
                
                // Pagination
                currentPage,
                goToPage,
                pageSize,
                setPageSize,
                totalItems,
                setTotalItems,
                totalPages,
                
                // States
                trashOpen,
                setTrashOpen,
                
                // Triggers
                gridReloadTrigger,
                triggerGridReload,
                
                // Router and SearchParams
                router,
                searchParams,
                pathname,
                
                // Data
                rowData,
                setRowData,
                loading,
                setLoading,
                sortBy,
                setSortBy,
                sortDirection,
                setSortDirection,
                mode,
                setMode
            }}>
        {children}
        </ArchiveContext.Provider>
    );
}




// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


