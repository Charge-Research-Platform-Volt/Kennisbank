"use client";

import React from "react";
import { Dialog, DialogClose, DialogContent, DialogDescription, DialogHeader, DialogOverlay, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import Search from "@/icons/search-icon";
import { VisuallyHidden } from "radix-ui";
import { Input } from "./ui/input";
import { useHotkeys } from "react-hotkeys-hook";
import { useDebouncedCallback } from "use-debounce";
import { toast } from "sonner";
import { OctagonAlert, X } from "lucide-react";
import GetFileIcon from "./getFileIcon";
import { useQuickSearch } from "../context/quick-search-provider";
import Kbd from "./kbd";
import { Button } from "./ui/button";
import { GridRequest, GridRequestSchema } from "@/types/gridRequest.type";
import { ApiResponse } from "@/types/apiResponse.type";
import Link from "next/link";
import { ResourceGridItem } from "@/context/archive-provider";

/**
 *
 * @returns QuickSearch bar in the top left corner of the screen. Users can then quickly search through the archive and open files / visit websites.
 */
export default function QuickSearch({ minimize = false }: { minimize?: boolean })
{
    const { isOpen, setIsOpen } = useQuickSearch();
    const [shortcut, setShortcut] = React.useState("");
    const [searchResults, setSearchResults] = React.useState<ResourceGridItem[]>([]);
    const [isLoading, setIsLoading] = React.useState<boolean>(false);
    const [totalItems, setTotalItems] = React.useState<number | null>(null);
    const [searchDuration, setSearchDuration] = React.useState<number | null>(null);
    const [searchQuery, setSearchQuery] = React.useState<string>("");
    const inputRef = React.useRef<HTMLInputElement>(null);

    // Keyboard shortcut
    useHotkeys("mod+k", () => setIsOpen(true), { preventDefault: true });

    React.useLayoutEffect(() =>
    {
        const isMac = navigator.userAgent.includes("Mac");
        setShortcut(isMac ? "Cmd + K" : "Ctrl + K");
    }, []);

    // Fetch search results
    const fetchSearchResults = async (query?: string) =>
    {
        setIsLoading(true);
    
        query = query?.trim();

        const request: GridRequest = GridRequestSchema.parse(
        {
            pageIndex: 1,
            pageSize: 20,
            searchQuery: query,
            sortBy: undefined,
            sortDirection: undefined,
            filterOptions:
            {
                typeFilter: undefined,
                pubdateMin: undefined,
                pubdateMax: undefined,
                tagFilter: undefined,
                regionFilter: undefined,
            },
        });

        try
        {
            const response = await fetch(`/api/resources/grid`,
            {
                method: "POST",
                credentials: "include",
                headers:
                {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(request),
            });

            if (!response.ok)
            {
                toast.error("An error occurred while getting the search results.");
                return;
            }

            const data: ApiResponse = await response.json();

            if (data.success)
            {
                setSearchResults(data.body.items);
                setTotalItems(data.body.totalCount || null);
                setSearchDuration(data.body.durationInMs || null);
            }
            else throw new Error(data.message);
        }
        catch
        {
            toast.error("An error occurred.");
        }
        finally 
        {
            setIsLoading(false);
        }
    };

    // Fetch search results on load
    React.useEffect(() =>
    {
        if (isOpen)
        {
            setSearchQuery("");
            fetchSearchResults();
        }
    }, [isOpen]);

    const handleSearch = useDebouncedCallback(async (query: string) =>
    {
        fetchSearchResults(query);
    }, 300);

    return (
        <>
            <Dialog open={isOpen} onOpenChange={setIsOpen}>
                <DialogTrigger asChild>
                    <Button data-testid="open-quicksearch" variant="outline" className={`m-0 flex w-full items-center justify-between overflow-hidden p-2 transition-all duration-200 ${!minimize && "w-9"}`}>
                        <div className="flex items-center gap-2">
                            <Search className="h-4 w-4" />
                            Search
                        </div>
                        <Kbd>{shortcut}</Kbd>
                    </Button>
                </DialogTrigger>

                <DialogOverlay />
                <DialogContent className="h-full max-h-[450px] w-full content-start gap-2 p-1 sm:max-w-[650px] [&>button:not(.external-close)]:hidden">
                    <DialogClose className="external-close cursor-pointer absolute -top-8 -right-8 opacity-70 transition-opacity hover:opacity-100 focus:outline-none disabled:pointer-events-none">
                        <X className="h-6 w-6 text-white" />
                        <span className="sr-only">Close</span>
                    </DialogClose>
                    <VisuallyHidden.Root>
                        <DialogTitle>Search for anything</DialogTitle>
                        <DialogDescription>Search for anything in the knowledgeBank by typing in the search box below.</DialogDescription>
                    </VisuallyHidden.Root>

                    {/* Search input*/}
                    <DialogHeader>
                        <div className="*:not-first:mt-2">
                            <div className="relative">
                                <Input
                                    ref={inputRef}
                                    className="peer h-10 ps-9 pe-9"
                                    placeholder="Search"
                                    type="text"
                                    value={searchQuery}
                                    onChange={(e) =>
                                    {
                                        setSearchQuery(e.target.value);
                                        handleSearch(e.target.value);
                                    }}
                                />
                                <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
                                    <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
                                </div>
                                {searchQuery && (
                                    <button
                                        onClick={() =>
                                        {
                                            setSearchQuery("");
                                            handleSearch("");
                                            inputRef.current?.focus();
                                        }}
                                        className="text-muted-foreground/80 hover:text-muted-foreground absolute inset-y-0 end-0 flex items-center justify-center pe-3 transition-colors"
                                        aria-label="Clear search"
                                    >
                                        <X className="h-4 w-4" />
                                    </button>
                                )}
                            </div>
                        </div>
                        {/* Search count and duration */}
                        { searchDuration &&
                            <div className="w-full pl-3">
                                <span className="text-xs italic text-gray-500">Found {totalItems} results in {searchDuration}ms</span>
                            </div>
                        }
                    </DialogHeader>

                    {/* Search results*/}
                    <div className="flex h-full flex-col gap-1 overflow-y-auto">
                        {/* No results found */}
                        {searchResults && !isLoading && searchResults.length === 0 && (
                            <div className="flex justify-center w-full">
                                <p className="flex items-center gap-3 p-3">
                                    <OctagonAlert size={16} />
                                    No results found.
                                </p>
                            </div>
                        )}
                        
                        {/* Searching... */}
                        { isLoading && (
                            <div className="flex justify-center w-full">
                                <p className="flex items-center gap-3 p-3">
                                    Searching...
                                </p>
                            </div>
                        )}

                        {/* Results list */}
                        {searchResults && !isLoading &&
                            searchResults.length > 0 &&
                            searchResults.map((file) => (
                                <Link
                                    href={`/archive?id=${file.id}`}
                                    onClick={() => setIsOpen(false)}
                                    key={file.id}
                                    role="button"
                                    className="hover:bg-muted-foreground/20 focus-visible:bg-muted-foreground/20 cursor-pointer rounded-lg bg-transparent p-3 transition-colors outline-none"
                                >
                                    <div className={`${file.description !== "" && "mb-2"} flex items-start gap-2`}>
                                        <GetFileIcon fileType={file.fileType} className="mt-[3px]" />
                                        <h3>{file.name}</h3>
                                    </div>

                                    {file.description !== "" && <p className="text-muted-foreground/80 text-sm line-clamp-3">{file.description}</p>}
                                </Link>
                            ))}
                    </div>
                </DialogContent>
            </Dialog>
        </>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
