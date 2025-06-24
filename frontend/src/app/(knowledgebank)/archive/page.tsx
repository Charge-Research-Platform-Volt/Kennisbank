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
import { DynamicCombobox } from "@/components/ui/dynamic-combobox";
import { TagFilterOptions } from "@/types/tag.type";
import { SelectOption } from "@/components/ui/selection";
import { useUserRole } from "@/context/user-role-context";
import TrashIcon from "@/icons/trash-icon";
import { ReadonlyURLSearchParams, useSearchParams } from "next/navigation";
import ResourcesList from "@/components/archive/resources-list";
import LayoutSwitch from "@/components/archive/layout-switch";

export default function Page() {
  const searchParams = useSearchParams();
  const isListLayout: boolean = searchParams.get("layout") === "list" ? true : false;

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
        tagFilter,
        setTagFilter,
        regionFilter,
        setRegionFilter,
        resetFilters,
        trashOpen,
        setTrashOpen,
        router,
    } = useArchive();
    
    const { userRole } = useUserRole();
    
    // States
    const [filtersOpen, setFiltersOpen] = React.useState<boolean>(false);
    const [trashAnimationTrigger, setTrashAnimationTrigger] = React.useState(Date.now());
    
    // Handle URL parameters and apply filters on page load
    React.useEffect(() => {
        const listType = searchParams.get('listType');
        const id = searchParams.get('id');
        
        if (listType && id) {
            // Apply the appropriate filters based on listType
            if (listType === 'tags') {
                setTypeFilter(['resource']);
                setTagFilter([id]);
            } else if (listType === 'regions') {
                setTypeFilter(['resource']);
                setRegionFilter([id]);
            }
            
            // Clean up the URL parameters after applying filters
            const newParams = new URLSearchParams(searchParams.toString());
            newParams.delete('listType');
            newParams.delete('id');
            router.replace(`/archive?${newParams.toString()}`);
        }
    }, [searchParams, setTypeFilter, setTagFilter, setRegionFilter, router]);

    // Create tag body function for the dynamic combobox
    const createTagBody = (searchQuery: string): unknown => 
    {
        const body: TagFilterOptions = 
        {
            usePaging: true,
            pageIndex: 1,
            pageSize: 20,
            searchQuery: searchQuery,
            includeUsageCount: false,
            includeCanEditAndDelete: false,
        }
        
        return body;
    }
    
    // Parse the tag response for the dynamic combobox
    const parseTagResponse = (response: unknown): SelectOption[] => {
        const data = response as { tags: Array<{ id: string | number; name: string }> };
        
        if (!data?.tags || !Array.isArray(data.tags)) {
            return [];
        }
        
        return data.tags.map(tag => ({
            value: tag.id,
            label: tag.name
        })) as SelectOption[];
    };
    
    // Create region body for dynamic combobox
    const createRegionBody = (searchQuery: string): unknown => 
    {
        const params = new URLSearchParams(
        {
            pageIndex: '1',
            pageSize: '20',
            properties: 'Id as value,Name as label',
            searchQuery: searchQuery
        });
        
        return params;
    }

    return (
        <div className="@container flex flex-col h-full w-full p-2">
            {/* Search bar/Trash title */}
            <div className="w-full h-[4rem] flex items-center gap-2 flex-shrink-0 bg-white z-10 mb-2">
                {
                    !trashOpen &&
                    <div className="relative flex-grow">
                        {/* Search input */}
                        <Input className="peer h-10 ps-9" placeholder={"Search"} type="text" value={searchInput} onChange={(e) => setSearchInput(e.target.value)} />
                        
                        {/* Search icon */}
                        <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
                            <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
                        </div>
                        
                        {/* Filter and clear input button */}
                        <div className="absolute inset-y-0 right-0 flex items-center justify-center">
                            { searchInput !== '' && <X className="text-gray-600 cursor-pointer" onClick={() => setSearchInput('')} />}
                            <Button variant="ghost" onClick={() => setFiltersOpen(!filtersOpen)}>
                                <Filter className="h-6 w-4 text-gray-600" aria-hidden="true" fill="currentColor" />
                            </Button>
                        </div>
                    </div>
                }
                
                {
                    trashOpen &&
                    <div className="flex-grow flex justify-start items-center gap-3">
                        <TrashIcon className="ml-3 w-10 h-10 text-gray-600" />
                        <h1 className="text-3xl font-bold text-gray-900">Trash</h1>
                    </div>
                }
                
                <LayoutSwitch isListLayout={isListLayout} />
                
                {
                    userRole === 'admin' &&
                    <Button variant="outline" onClick={() => { setFiltersOpen(false); resetFilters(); setTrashOpen(!trashOpen); setTrashAnimationTrigger(Date.now()); }}>
                        {!trashOpen && <TrashIcon key={trashAnimationTrigger} className='text-gray-600' />}
                        {trashOpen && "Back"}
                    </Button>
                }
            </div>
            
            {/* Filter selection */}
            { filtersOpen &&
                <div className="w-full grid @[1200px]:grid-cols-4 @[900px]:grid-cols-3 @[600px]:grid-cols-2 grid-cols-1 gap-4 flex-shrink-0 my-3 px-5 pb-2 border-b border-gray-300">
                    {/* Type filter */}
                    <div className="w-full">
                        <Label htmlFor="typeFilter" className="pb-2">Show types:</Label>
                        <Combobox id="typeFilter" className="w-full" multiSelect={true} enabledByDefault={true} value={typeFilter} onValueChange={setTypeFilter} options={[{value: 'resource', label: 'Resource'}, {value: 'person', label: 'Person'}, {value: 'organisation', label: 'Organisation'}]} />
                        <ResetFilter onClick={() => setTypeFilter(['resource', 'person', 'organisation'])} />
                    </div>
                    
                    {/* Publication date range */}
                    <div className="w-full">
                        <Label htmlFor="publicationRange" className="pb-2">Publication date range:</Label>
                        <div id="publicationRange" className="w-full grid grid-cols-2 gap-x-2">
                            <Input type="date" max={publicationDateRangeMax} value={publicationDateRangeMin} onChange={(e) => setPublicationDateRangeMin(e.target.value)} className="cursor-pointer" />
                            <Input type="date" min={publicationDateRangeMin} value={publicationDateRangeMax} onChange={(e) => setPublicationDateRangeMax(e.target.value)} className="cursor-pointer" />
                            <ResetFilter onClick={() => setPublicationDateRangeMin('')} />
                            <ResetFilter onClick={() => setPublicationDateRangeMax('')} />
                        </div>
                    </div>
                    
                    {/* Tags filter */}
                    <div className="w-full">
                        <Label htmlFor="tagsFilter" className="pb-2">Tag filter:</Label>
                        <DynamicCombobox id="tagsFilter" className="w-full" multiSelect={true} value={tagFilter} onValueChange={setTagFilter} endpoint={"/api/tags/tags"} createPayload={createTagBody} parseResponse={parseTagResponse} usePost={true} />
                        <ResetFilter onClick={() => setTagFilter([])} />
                    </div>
                    
                    {/* Region filter */}
                    <div className="w-full">
                        <Label htmlFor="regionFilter" className="pb-2">Region filter:</Label>
                        <DynamicCombobox id="regionFilter" className="w-full" multiSelect={true} value={regionFilter} onValueChange={setRegionFilter} endpoint={"/api/regions/list"} createPayload={createRegionBody} parseResponse={(response) => response as SelectOption[]} />
                        <ResetFilter onClick={() => setRegionFilter([])} />
                    </div>
                    
                    {/* Reset all and close button */}
                    <div className="@[1200px]:col-span-4 @[900px]:col-span-3 @[600px]:col-span-2 col-span-1 flex justify-center gap-4 mt-2">
                        <Button variant="outline" className="border-red-500 text-red-500 hover:bg-red-50 hover:border-red-600 hover:text-red-600" onClick={resetFilters}>Reset All Filters</Button>
                        <Button variant="outline" onClick={() => setFiltersOpen(false)}>Close</Button>
                    </div>
                </div>
            }
            
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


