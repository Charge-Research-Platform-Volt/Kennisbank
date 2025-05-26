
import React, { useState, useEffect, useCallback } from "react";
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import New from "@/icons/new"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { newRelationSearchResults, organisationRelation, personRelation, resourceRelation } from "@/actions/right-sidebarActions";
import { Button } from "@/components/ui/button";

interface NewBadgeProps
{
    variant?: "outline" | "default" | "secondary" | "destructive";
    type: resourceRelation | personRelation | organisationRelation;
}

interface SearchResult {
    id: string;
    name: string;
    // Add other properties based on your API response structure
}

export default function NewBadge({
    variant = "outline",
    type,
} : NewBadgeProps)
{
    const { currentId, navigate } = useSidebar();
    const [container, setContainer] = useState<any>(null);
    const [searchQuery, setSearchQuery] = useState<string>("");
    const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
    const [selected, setSelected] = useState<SearchResult[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [isOpen, setIsOpen] = useState<boolean>(false);

    // Debounced search function
    const debouncedSearch = useCallback(
        debounce(async (query: string) => {
            if (!query.trim() || !type) {
                setSearchResults([]);
                return;
            }

            setIsLoading(true);
            try {
                const response = await newRelationSearchResults(query, type as any, 10);
                if (type === "tags") {
                    setSearchResults(response.body.tags || [])
                }
                else setSearchResults(response.body || []);
            } catch (error) {
                console.error("Search error:", error);
                setSearchResults([]);
            } finally {
                setIsLoading(false);
            }
        }, 300),
        [type]
    );


    useEffect(() => {
        debouncedSearch(searchQuery);
    }, [searchQuery, debouncedSearch]);


    function handleResultSelect(result: SearchResult) {
        setSelected( prev => {
            const isSelected = prev.some(item => item.id === result.id); // check if the clicked option is already selected 
            if (isSelected) {
                return prev.filter(item => item.id === result.id); // if so remove it
            } else {
                return [...prev, result]; // else add it
            }
        })
    }

    function handleClose() {
        setIsOpen(false);
        setSearchQuery("");
        setSelected([]);
    }

    function handleAdd() {
        
        //add
        
        setIsOpen(false);
        setSearchQuery("");
        setSelected([]);
    }

    function isItemSelected(result: SearchResult): boolean {
        return selected.some(item => item.id === result.id);
    }



    return(
        <Popover open={isOpen} onOpenChange={setIsOpen}>
            <PopoverTrigger>
                <Badge key={-1} variant={variant} style={{ width: '2.1rem', height: '2.1rem', userSelect: 'none'}} ><New style={{ width: '1.7rem', height: '1.7rem' }} className=" text-black" /></Badge>
            </PopoverTrigger>
            <PopoverContent 
                className="w-80 p-3 rounded-md" 
                container={container} 
                forceMount>
                <div className="w-full h-80 flex flex-col">
                    {/* Search Input */}
                    <div className="mb-3">
                        <Input
                            type="text"
                            placeholder={`Search ${type || 'items'}...`}
                            value={searchQuery}
                            onChange={(e) => setSearchQuery(e.target.value)}
                            className="w-full"
                            autoFocus
                        />
                    </div>

                    {/* Results List */}
                    <div className="flex-1 overflow-y-auto border rounded-md">
                        {isLoading ? (
                            <div className="p-3 text-center text-gray-500">
                                Searching...
                            </div>
                        ) : searchResults.length > 0 ? (
                            <div className="divide-y">
                                {searchResults.map((result) => (
                                    <div
                                        key={result.id}
                                        className="p-2 hover:bg-gray-50 cursor-pointer transition-colors"
                                        onClick={() => handleResultSelect(result)}
                                    >
                                        <div className="text-sm font-medium">
                                            {result.name}
                                        </div>
                                        {/* Add additional result information here if needed */}
                                    </div>
                                ))}
                            </div>
                        ) : searchQuery.trim() ? (
                            <div className="p-3 text-center text-gray-500">
                                No results found
                            </div>
                        ) : (
                            <div className="p-3 text-center text-gray-400">
                                Start typing to search...
                            </div>
                        )}
                    </div>

                    {/* Action Buttons */}
                    <div className="flex gap-2">
                        <Button 
                            variant="outline" 
                            onClick={handleClose}
                            className="flex-1"
                        >
                            Close
                        </Button>
                        <Button 
                            onClick={handleAdd}
                            disabled={selected.length === 0}
                            className="flex-1"
                        >
                            Add
                        </Button>
                    </div>

                </div>
            </PopoverContent>
        </Popover>
    );
}


// Utility function for debounce
function debounce<T extends (...args: any[]) => any>(
    func: T,
    wait: number
): (...args: Parameters<T>) => void {
    let timeout: NodeJS.Timeout;
    return (...args: Parameters<T>) => {
        clearTimeout(timeout);
        timeout = setTimeout(() => func(...args), wait);
    };
}