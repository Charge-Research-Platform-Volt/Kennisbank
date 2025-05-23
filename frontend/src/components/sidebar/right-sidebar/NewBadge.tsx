
import React, { useState, useEffect, useCallback } from "react";
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import New from "@/icons/new"
import { useSidebar, MetadataTypeEnum } from "@/context/sidebar-provider"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { newRelationSearchResults } from "@/actions/right-sidebarActions";

interface NewBadgeProps
{
    variant?: "outline" | "default" | "secondary" | "destructive";
    type: string | null;
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
                const response = await newRelationSearchResults(query, type as any, 6);
                setSearchResults(response.body || []);
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
        // Handle selection logic here
        console.log("Selected:", result);
        setIsOpen(false);
        setSearchQuery("");
    }



    return(
        <Popover>
            <PopoverTrigger>
                <Badge key={-1} variant={variant} style={{ width: '2.1rem', height: '2.1rem', userSelect: 'none'}} ><New style={{ width: '1.7rem', height: '1.7rem' }} className=" text-black" /></Badge>
            </PopoverTrigger>
            <PopoverContent 
                className="w-80 p-3 rounded-md" 
                container={container} 
                forceMount>
                <div className="w-full h-60 flex flex-col">
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