'use client'

import React from 'react';
import { usePathname, useSearchParams, useRouter } from 'next/navigation';

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
};

const ArchiveContext = React.createContext<ArchiveContextType | undefined>(undefined);

export const useArchive = (): ArchiveContextType =>
{
    const context = React.useContext(ArchiveContext);

    if (!context) throw new Error("useArchive must be used within a ArchiveProvider");

    return context;
}

export const ArchiveProvider = ({children}: {children: React.ReactNode}) =>
{
    const searchParams = useSearchParams();
    const router = useRouter();
    const pathname = usePathname();

    // Filters
    const [tagFilter, setTagFilter] = React.useState<string[]>(searchParams.getAll('tagFilter'));
    const [typeFilter, setTypeFilter] = React.useState<string[]>(searchParams.getAll('typeFilter').length > 0 ? searchParams.getAll('typeFilter') : ['resource', 'person', 'organisation']);
    const [publicationDateRangeMin, setPublicationDateRangeMin] = React.useState<string>(searchParams.get('pubdateMin') || '');
    const [publicationDateRangeMax, setPublicationDateRangeMax] = React.useState<string>(searchParams.get('pubdateMax') || '');
    const [regionFilter, setRegionFilter] = React.useState<string[]>(searchParams.getAll('regionFilter'));
    
    const isResettingRef = React.useRef<boolean>(false);
    
    // Search
    const [searchInput, setSearchInput] = React.useState<string>(searchParams.get('query') || '');
    const [searchQuery, setSearchQuery] = React.useState<string>(searchParams.get('query') || '');
    
    // Pagination
    const [currentPage, setCurrentPage] = React.useState<number>(Number(searchParams.get('page')) || 1);
    const [pageSize, setPageSize] = React.useState<number>(20);
    const [totalItems, setTotalItems] = React.useState<number>(0);
    const [totalPages, setTotalPages] = React.useState<number>(1);
    
    // Update search parameters helper
    const updateParam = React.useCallback((key: string, value?: string | string[] | undefined) => 
    {
        if (isResettingRef.current) return;
    
        const params = new URLSearchParams(searchParams.toString());
        
        // Delete existing value
        params.delete(key);
        
        // Only add when value is provided
        if (value) 
        {
            // Handle arrays
            if (Array.isArray(value))
                value.forEach((item) => params.append(key, item));
                
            // Single item
            else
                params.set(key, String(value));
        }
        
        // Update address bar
        router.replace(`${pathname}?${params.toString()}`);
    }, [pathname, router, searchParams]);
    
    // Debounce search query
    React.useEffect(() => 
    {
        const timer = setTimeout(() => 
        {
            setSearchQuery(searchInput);
        }, 500); // 500 ms delay
        
        return () => clearTimeout(timer);
    }, [searchInput]);
    
    // Reset to first page when filters/search change
    React.useEffect(() => 
    {
        setCurrentPage(prev => prev !== 1 ? 1 : prev);
    }, [searchQuery, typeFilter, tagFilter, publicationDateRangeMax, publicationDateRangeMin]);
    
    // Calculate total pages on total items or page size change
    React.useEffect(() => 
    {
        setTotalPages(Math.ceil(totalItems / pageSize));
    }, [totalItems, pageSize])
    
    // Page helper
    const goToPage = (pageIndex: number) => 
    {
        if (pageIndex >= 1)
            setCurrentPage(pageIndex <= totalPages ? pageIndex : totalPages);
    };
    
    // Filter reset function
    const resetFilters = () =>
    {
        isResettingRef.current = true
    
        setTypeFilter(['resource', 'person', 'organisation']);
        setPublicationDateRangeMax('');
        setPublicationDateRangeMin('');
        setTagFilter([]);
        setRegionFilter([]);
        
        const params = new URLSearchParams();
        
        // Only keep non-filter params
        if (searchQuery) params.set('query', searchQuery);
        if (currentPage > 1) params.set('page', String(currentPage));
        
        // Update address bar
        router.replace(`${pathname}?${params.toString()}`);
        
        // Set isResetting to false in the next tick to prevent race conditions
        setTimeout(() => isResettingRef.current = false, 0);
    };
    
    // Search param updaters for each filter, search and pagination:
    React.useEffect(() => updateParam('tagFilter', tagFilter), [updateParam, tagFilter]);
    React.useEffect(() => updateParam('typeFilter', typeFilter.length === 3 ? undefined : typeFilter), [updateParam, typeFilter]);
    React.useEffect(() => updateParam('pubdateMin', publicationDateRangeMin), [updateParam, publicationDateRangeMin]);
    React.useEffect(() => updateParam('pubdateMax', publicationDateRangeMax), [updateParam, publicationDateRangeMax]);
    React.useEffect(() => updateParam('regionFilter', regionFilter), [updateParam, regionFilter]);
    React.useEffect(() => updateParam('query', searchQuery), [updateParam, searchQuery]);
    React.useEffect(() => updateParam('page', currentPage > 1 ? String(currentPage) : undefined), [updateParam, currentPage]);

    return (
        <ArchiveContext.Provider 
            value={{
                // Filters
                resetFilters,
                tagFilter,
                setTagFilter: setTagFilter,
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
            }}>
        {children}
        </ArchiveContext.Provider>
    );
}


