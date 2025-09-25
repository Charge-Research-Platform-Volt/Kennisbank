
import React, { useState, useEffect, useRef } from "react";
import { Badge } from "@/components/ui/badge"
import { Input } from "@/components/ui/input"
import New from "@/icons/new"
import { useArchiveSidebar } from "@/context/archive-sidebar-provider"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { newRelationSearchResults, organisationRelation, personRelation, resourceRelation, addRelation, addNewRegion } from "@/actions/archive-sidebarActions";
import { Button } from "@/components/ui/button";
import { ListItem } from "./BadgeList";
import { toast } from "sonner";

interface NewBadgeProps
{
    variant?: "outline" | "default" | "secondary" | "destructive";
    relation: resourceRelation | personRelation | organisationRelation;
    onNew: (items: ListItem[]) => void;
    alreadyRelated: ListItem[];
}

interface SearchResult {
    id: string;
    name: string;
}

export default function NewBadge({
    variant = "outline",
    relation,
    onNew,
    alreadyRelated,
} : NewBadgeProps)
{
    const { currentId, currentType } = useArchiveSidebar();
    const [searchInput, setSearchInput] = useState<string>("");
    const [searchQuery, setSearchQuery] = useState<string>("");
    const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
    const [selected, setSelected] = useState<SearchResult[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [isOpen, setIsOpen] = useState<boolean>(false);
    const [source, setSource] = useState<string>("");

    // use refs to avoid rerenders
    const latestRelation = useRef(relation);
    const latestAlreadyRelated = useRef(alreadyRelated);

    //
    useEffect(() => {
        latestRelation.current = relation;
        latestAlreadyRelated.current = alreadyRelated;
    }, [relation, alreadyRelated]);
    
    // Debounce search input
    useEffect(() => 
    {
        const timer = setTimeout(() => setSearchQuery(searchInput), 300);
        
        return () => clearTimeout(timer);
    }, [searchInput]);
    
    // Perform search on query change
    useEffect(() => 
    {
        async function performSearch() 
        {
            if (!searchQuery.trim() || !latestAlreadyRelated.current)
            {
                setSearchResults([]);
                return;
            }

            setIsLoading(true);
            try {
                const response = await newRelationSearchResults(searchQuery, latestRelation.current, 10);
                if (latestRelation.current === "tags")
                {
                    setSearchResults(response.body.tags.filter((item: { id: string; name: string;}) => 
                        !(latestAlreadyRelated.current.some(i => i.id === item.id))) || [])
                }
                else setSearchResults(response.body.filter((item: { id: string; name: string;}) => 
                    !(latestAlreadyRelated.current.some(i => i.id === item.id))) || []);
            } catch (error) 
            {
                console.error("Search error:", error);
                setSearchResults([]);
            } finally 
            {
                setIsLoading(false);
            }
        }
        
        performSearch();
    }, [searchQuery]);


    function handleResultSelect(result: SearchResult) {
        setSelected( prev => {
            const isSelected = prev.some(item => item.id === result.id); // check if the clicked option is already selected 
            if (isSelected) {
                return prev.filter(item => item.id !== result.id); // if so remove it
            } else {
                return [...prev, result]; // else add it
            }
        })
    }

    function handleClose() {
        setIsOpen(false);
        setSearchQuery("");
        setSelected([]);
        setSource("");
    }

    async function handleAdd() {
        try {
            const relations = selected.map(item => 
                addRelation(relation, currentType, currentId, item.id)
            );
            
            await Promise.all(relations);
            onNew(selected as ListItem[])

            setIsOpen(false);
            setSearchQuery("");
            setSelected([]);
            setSource("");
        } catch (error) {
            console.error("Error adding relations:", error);
        }
    }

    async function handleAddSource() {
        try {
            await addRelation(relation, currentType, currentId, source);
            onNew([{name: source, id: source}] as ListItem[]);

            setIsOpen(false);
            setSearchQuery("");
            setSelected([]);
            setSource("");
        } catch (error) {
            console.error("Error adding source:", error);
        }
    }

    async function handleAddRegion() {
        try {
            const addedRegionId = await addNewRegion(searchQuery);
            toast.info(`Region created succesfully: ${searchQuery}`);
            await addRelation("regions", currentType, currentId, addedRegionId);

            setIsOpen(false);
            onNew([{id: addedRegionId, name: searchQuery}] as ListItem[]);
            setSearchQuery("");
            setSelected([]);
            setSource("");
        } catch (error) {
            console.error("Error adding region:", error);
        }
    }

    function isItemSelected(result: SearchResult): boolean 
    {
        return selected.some(item => item.id === result.id);
    }



    return(
        <Popover open={isOpen} onOpenChange={setIsOpen}>
            <PopoverTrigger className="cursor-pointer">
                <Badge key={-1} variant={variant} style={{ width: '2.1rem', height: '2.1rem', userSelect: 'none'}} ><New style={{ width: '1.7rem', height: '1.7rem' }} className=" text-black" /></Badge>
            </PopoverTrigger>
            <PopoverContent 
                className="w-80 p-3 rounded-md" 
                forceMount>

                {relation === "sources" || relation === "related-sources" ? (
                    <div className="w-full h-20 flex flex-col">
                        {/* Input */}
                        <div className="mb-3">
                            <Input
                                type="text" 
                                placeholder={`Add valid URL...`}
                                value={source}
                                onChange={(e) => setSource(e.target.value)}
                                className="w-full"
                                autoFocus
                            />
                        </div>
                        <Button 
                            onClick={handleAddSource}
                            className="flex-1"
                            disabled={!URL.canParse(source)}
                        >
                            Add
                        </Button>
                    </div>

                ) : (

                <div className="w-full h-90 flex flex-col">
                    {/* Search Input */}
                    <div className="mb-3">
                        <Input
                            type="text"
                            placeholder={`Search ${relation || 'items'}...`}
                            value={searchInput}
                            onChange={(e) => setSearchInput(e.target.value)}
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
                                        <div className="flex items-center justify-between">
                                            <div className="text-sm font-medium">
                                                {result.name}
                                            </div>
                                            {isItemSelected(result) && (
                                                <div className="text-xs mr-2 font-semibold">
                                                    ✓
                                                </div>
                                            )}
                                        </div>
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
                    <div className="flex gap-2 mt-1">
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
                    {relation === "regions" && (
                        <Button 
                            onClick={handleAddRegion}
                            className="flex mt-1 h-10"
                            disabled={searchQuery.length === 0}
                        >
                            Add New
                        </Button>
                    )}
                </div>) }
            </PopoverContent>
        </Popover>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
