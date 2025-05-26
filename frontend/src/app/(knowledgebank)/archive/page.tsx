"use client";

import { useArchive } from "@/context/archive-provider";
import ResourcesGrid from "@/components/archive/resources-grid";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import { Button } from "@/components/ui/button";
import React from "react";
import Filter from "@/icons/filter";
import { Combobox } from "@/components/ui/combobox";
import { Label } from "@/components/ui/label";
import ResetFilter from "@/components/archive/reset-filter";
import { X } from "lucide-react";

export default function Page() {
    // Context
    const {
        searchInput,
        setSearchInput,
        currentPage,
        totalPages,
        goToPage,
        typeFilter,
        setTypeFilter,
        publicationDateRangeMax,
        publicationDateRangeMin,
        setPublicationDateRangeMax,
        setPublicationDateRangeMin,
    } = useArchive();
    
    // States
    const [filtersOpen, setFiltersOpen] = React.useState<boolean>(false);
    
    const clearAllFilters = () =>
    {
        setTypeFilter('resource,person,organisation');
        setPublicationDateRangeMax('');
        setPublicationDateRangeMin('');
    };

    return (
        <div className="flex flex-col h-full w-full">
            {/* Search bar */}
            <div className="w-full h-[4rem] flex items-center gap-2 flex-shrink-0 bg-white z-10">
                <div className="relative flex-grow">
                    {/* Search input */}
                    <Input className="peer h-10 ps-9" placeholder={"Search"} type="text" value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
                    
                    {/* Search icon */}
                    <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
                        <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
                    </div>
                    
                    {/* Filter button */}
                    <div className="absolute inset-y-0 right-0 flex items-center justify-center">
                        { searchInput !== '' && <X className="text-gray-600 cursor-pointer" onClick={() => setSearchInput('')} />}
                        <Button variant="ghost" onClick={() => setFiltersOpen(!filtersOpen)}>
                            <Filter className="h-6 w-4 text-gray-600" aria-hidden="true" fill="currentColor" />
                        </Button>
                    </div>
                </div>
            </div>
            
            {/* Filter selection */}
            <div hidden={!filtersOpen} className="w-full flex grid grid-cols-4 gap-2 flex-shrink-0 my-3 px-5">
                {/* Type filter */}
                <div className="w-full">
                    <Label htmlFor="typeFilter" className="pl-1 pb-2">Show types:</Label>
                    <Combobox id="typeFilter" className="w-full" multiSelect={true} enabledByDefault={true} value={typeFilter.split(',')} onValueChange={(values) => setTypeFilter(values.join(','))} options={[{value: 'resource', label: 'Resource'}, {value: 'person', label: 'Person'}, {value: 'organisation', label: 'Organisation'}]} />
                    <ResetFilter onClick={() => setTypeFilter('resource,person,organisation')} />
                </div>
                
                {/* Publication date range */}
                <div className="w-full">
                    <Label htmlFor="publicationRange" className="pl-1 pb-2">Publication date range:</Label>
                    <div id="publicationRange" className="w-full flex grid grid-cols-2 gap-x-2">
                        <Input type="date" max={publicationDateRangeMax} value={publicationDateRangeMin} onChange={(e) => setPublicationDateRangeMin(e.target.value)} className="cursor-pointer" />
                        <Input type="date" min={publicationDateRangeMin} value={publicationDateRangeMax} onChange={(e) => setPublicationDateRangeMax(e.target.value)} className="cursor-pointer" />
                        <ResetFilter onClick={() => setPublicationDateRangeMin('')} />
                        <ResetFilter onClick={() => setPublicationDateRangeMax('')} />
                    </div>
                </div>
                
                {/* Close button */}
                <div className="col-span-4 flex justify-center gap-4 mt-2">
                    <Button variant="outline" className="border-red-500 text-red-500 hover:bg-red-50 hover:border-red-600 hover:text-red-600" onClick={clearAllFilters}>Clear All Filters</Button>
                    <Button variant="outline" onClick={() => setFiltersOpen(false)}>Close</Button>
                </div>
            </div>
            
            {/* Scrollable content area */}
            <div className="flex-1 overflow-y-auto min-h-0">
                <div className="w-full">
                    <ResourcesGrid />
                    
                    {/* Navigation buttons */}
                    <div className="w-full p-4 flex justify-center gap-5">
                        <Button className="w-20" disabled={currentPage == 1} onClick={() => goToPage(currentPage - 1)}>Previous</Button>
                        <Button variant="ghost" className="hover:bg-transparent hover:text-current pointer-events-none">{currentPage} / {totalPages}</Button>
                        <Button className="w-20" disabled={currentPage == totalPages} onClick={() => goToPage(currentPage + 1)}>Next</Button>
                    </div>
                </div>
            </div>
        </div>
    )
}