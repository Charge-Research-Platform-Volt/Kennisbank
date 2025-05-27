'use client'

import React from 'react';

export type ArchiveContextType = {

    // Filters
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
    // Filters
    const [tagFilter, setTagFilter] = React.useState<string[]>([]);
    const [typeFilter, setTypeFilter] = React.useState<string[]>(['resource', 'person', 'organisation']);
    const [publicationDateRangeMin, setPublicationDateRangeMin] = React.useState<string>('');
    const [publicationDateRangeMax, setPublicationDateRangeMax] = React.useState<string>('');
    const [regionFilter, setRegionFilter] = React.useState<string[]>([]);
    
    // Search
    const [searchInput, setSearchInput] = React.useState<string>('');
    const [searchQuery, setSearchQuery] = React.useState<string>('');
    
    // Pagination
    const [currentPage, setCurrentPage] = React.useState<number>(1);
    const [pageSize, setPageSize] = React.useState<number>(20);
    const [totalItems, setTotalItems] = React.useState<number>(0);
    const [totalPages, setTotalPages] = React.useState<number>(1);
    
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
        if (currentPage !== 1)
            setCurrentPage(1);
    }, [searchQuery, typeFilter, tagFilter, publicationDateRangeMax, publicationDateRangeMin]);
    
    // Calculate total pages on total items or page size change
    React.useEffect(() => 
    {
        setTotalPages(Math.ceil(totalItems / pageSize));
    }, [totalItems, pageSize])
    
    // Page helper
    const goToPage = (pageIndex: number) => 
    {
        if (pageIndex >= 1 && pageIndex <= totalPages)
            setCurrentPage(pageIndex);
    };

    return (
        <ArchiveContext.Provider 
            value={{
                // Filters
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


