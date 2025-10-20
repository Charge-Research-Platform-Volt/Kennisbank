"use client";

import React, { useState, useEffect } from "react";
import { Badge } from "@/components/ui/badge";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Plus } from "lucide-react";
import { addRelation, EntityType } from "@/lib/relationManager";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Item } from "./archive-sidebar";

interface SearchResult
{
    id: string;
    name: string;
    type: "person" | "organisation";
}

interface AddAuthorBadgeProps
{
    resourceId: string;
    alreadyRelated: Item[];
    onAdd: (items: Item[]) => void;
}

export default function AddAuthorBadge({
    resourceId,
    alreadyRelated,
    onAdd,
}: AddAuthorBadgeProps)
{
    const [searchInput, setSearchInput] = useState<string>("");
    const [searchQuery, setSearchQuery] = useState<string>("");
    const [searchResults, setSearchResults] = useState<SearchResult[]>([]);
    const [selected, setSelected] = useState<SearchResult[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [isOpen, setIsOpen] = useState<boolean>(false);
    const [activeTab, setActiveTab] = useState<"persons" | "organisations">("persons");

    // Debounce search input
    useEffect(() => {
        const timer = setTimeout(() => setSearchQuery(searchInput), 300);
        return () => clearTimeout(timer);
    }, [searchInput]);

    // Perform search on query change
    useEffect(() => {
        async function performSearch() {
            if (!searchQuery.trim()) {
                setSearchResults([]);
                return;
            }

            setIsLoading(true);
            try {
                const endpoint = activeTab === "persons" ? "/api/persons/list" : "/api/organisations/list";
                const url = new URL(endpoint, window.location.origin);
                url.searchParams.append("searchQuery", searchQuery);
                url.searchParams.append("properties", "Id, Name");

                const response = await fetch(url.toString(), {
                    method: "GET",
                    credentials: "include",
                });

                if (response.ok) {
                    const data = await response.json();
                    const results = data.body || [];

                    // Normalize results
                    const normalized: SearchResult[] = results.map((item: any) => ({
                        id: item.id,
                        name: item.name || item.Name || "Unknown",
                        type: activeTab === "persons" ? "person" : "organisation"
                    }));

                    // Filter out already related items
                    const filtered = normalized.filter(
                        (item: SearchResult) => !alreadyRelated.some((r) => r.id === item.id)
                    );
                    setSearchResults(filtered);
                } else {
                    setSearchResults([]);
                }
            } catch (error) {
                console.error("Search error:", error);
                setSearchResults([]);
            } finally {
                setIsLoading(false);
            }
        }

        performSearch();
    }, [searchQuery, activeTab, alreadyRelated]);

    function handleResultSelect(result: SearchResult) {
        setSelected((prev) => {
            const isSelected = prev.some((item) => item.id === result.id);
            if (isSelected) {
                return prev.filter((item) => item.id !== result.id);
            } else {
                return [...prev, result];
            }
        });
    }

    function handleClose() {
        setIsOpen(false);
        setSearchQuery("");
        setSearchInput("");
        setSelected([]);
    }

    async function handleAdd() {
        try {
            const relations = selected.map((item) =>
                addRelation("resources", resourceId, "authors", item.id)
            );

            const results = await Promise.all(relations);

            // Only add items that were successfully added, convert to Item type
            const successfulItems: Item[] = selected
                .filter((_, index) => results[index])
                .map(item => ({
                    id: item.id,
                    name: item.name,
                    relation: undefined
                }));

            if (successfulItems.length > 0) {
                onAdd(successfulItems);
            }

            handleClose();
        } catch (error) {
            console.error("Error adding authors:", error);
        }
    }

    function isItemSelected(result: SearchResult): boolean {
        return selected.some((item) => item.id === result.id);
    }

    return (
        <Popover open={isOpen} onOpenChange={setIsOpen}>
            <PopoverTrigger asChild>
                <Badge
                    variant="outline"
                    className="h-8 w-8 flex items-center justify-center cursor-pointer hover:bg-accent transition-colors"
                >
                    <Plus className="h-4 w-4" />
                </Badge>
            </PopoverTrigger>
            <PopoverContent className="w-96 p-0" align="start" sideOffset={5}>
                <div className="flex flex-col">
                    {/* Header */}
                    <div className="px-4 py-3 border-b bg-muted/50">
                        <h3 className="font-semibold text-sm">Add Authors</h3>
                    </div>

                    {/* Tabs */}
                    <Tabs value={activeTab} onValueChange={(value) => {
                        setActiveTab(value as "persons" | "organisations");
                        setSearchQuery("");
                        setSearchInput("");
                        setSearchResults([]);
                    }} className="w-full">
                        <TabsList className="w-full grid grid-cols-2 h-10">
                            <TabsTrigger value="persons">People</TabsTrigger>
                            <TabsTrigger value="organisations">Organisations</TabsTrigger>
                        </TabsList>

                        <TabsContent value={activeTab} className="m-0">
                            {/* Search Input */}
                            <div className="p-3 border-b">
                                <Input
                                    type="text"
                                    placeholder={activeTab === "persons" ? "Search people..." : "Search organisations..."}
                                    value={searchInput}
                                    onChange={(e) => setSearchInput(e.target.value)}
                                    className="w-full h-9"
                                    autoFocus
                                />
                            </div>

                            {/* Results List */}
                            <div className="max-h-72 overflow-y-auto">
                                {isLoading ? (
                                    <div className="p-8 text-center text-muted-foreground text-sm">
                                        <div className="animate-pulse">Searching...</div>
                                    </div>
                                ) : searchResults.length > 0 ? (
                                    <div>
                                        {searchResults.map((result) => (
                                            <div
                                                key={result.id}
                                                className="px-4 py-2.5 hover:bg-accent cursor-pointer transition-colors border-b last:border-b-0"
                                                onClick={() => handleResultSelect(result)}
                                            >
                                                <div className="flex items-center justify-between gap-3">
                                                    <span className="text-sm truncate flex-1">{result.name}</span>
                                                    {isItemSelected(result) && (
                                                        <div className="flex-shrink-0 w-5 h-5 rounded-full bg-primary flex items-center justify-center">
                                                            <svg className="w-3 h-3 text-primary-foreground" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                                                            </svg>
                                                        </div>
                                                    )}
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                ) : searchQuery.trim() ? (
                                    <div className="p-8 text-center text-muted-foreground text-sm">
                                        No results found for "{searchQuery}"
                                    </div>
                                ) : (
                                    <div className="p-8 text-center text-muted-foreground text-sm">
                                        Start typing to search...
                                    </div>
                                )}
                            </div>
                        </TabsContent>
                    </Tabs>

                    {/* Action Buttons */}
                    {selected.length > 0 && (
                        <div className="p-3 border-t bg-muted/30 flex gap-2">
                            <Button variant="outline" onClick={handleClose} className="flex-1 h-9">
                                Cancel
                            </Button>
                            <Button onClick={handleAdd} className="flex-1 h-9">
                                Add {selected.length > 1 && `(${selected.length})`}
                            </Button>
                        </div>
                    )}
                </div>
            </PopoverContent>
        </Popover>
    );
}
