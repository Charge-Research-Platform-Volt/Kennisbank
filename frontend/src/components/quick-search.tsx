"use client";

import React, { useEffect, useLayoutEffect, useState } from "react";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogOverlay, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import Search from "@/icons/search-icon";
import { VisuallyHidden } from "radix-ui";
import { Input } from "./ui/input";
import { useHotkeys } from "react-hotkeys-hook";
import { useDebouncedCallback } from "use-debounce";
import { toast } from "sonner";
import { OctagonAlert } from "lucide-react";
import GetFileIcon from "./getFileIcon";
import { openFile } from "@/actions/openFileActions";
import { useQuickSearch } from "../context/quick-search-provider";
import Kbd from "./kbd";
import { Button } from "./ui/button";
import { GridRequest, GridRequestSchema } from "@/types/gridRequest.type";
import { ResourceItem, ResourceResponse } from "./archive/resources-grid";
import { NewApiResponse } from "@/types/apiResponse.type";

/**
 *
 * @returns QuickSearch bar in the top left corner of the screen. Users can then quickly search through the archive and open files / visit websites.
 */
export default function QuickSearch({ minimize = false }: { minimize?: boolean }) {
  const { isOpen, setIsOpen } = useQuickSearch();
  const [shortcut, setShortcut] = useState("");
  const [searchResults, setSearchResults] = useState<ResourceItem[]>([]);

  // Keyboard shortcut
  useHotkeys("mod+k", () => setIsOpen(true), { preventDefault: true });

  useLayoutEffect(() => {
    const isMac = navigator.userAgent.includes("Mac");
    setShortcut(isMac ? "Cmd + K" : "Ctrl + K");
  }, []);

  // Fetch search results
  const fetchSearchResults = async (query?: string) => {
    query = query?.trim();

    const request: GridRequest = GridRequestSchema.parse({
      pageIndex: 1,
      pageSize: 20,
      searchQuery: query,
      sortBy: undefined,
      sortDirection: undefined,
      filterOptions: {
        typeFilter: undefined,
        pubdateMin: undefined,
        pubdateMax: undefined,
        tagFilter: undefined,
        regionFilter: undefined,
      },
    });

    try {
      const response = await fetch(`/api/resources/grid`, {
        method: "POST",
        credentials: "include",
        headers: {
          "Content-Type": "application/json",
        },
        body: JSON.stringify(request),
      });

      if (!response.ok) {
        toast.error("An error occurred while getting the search results.");
        return;
      }

      const data: NewApiResponse<ResourceResponse> = await response.json();
      console.log("Search results:", data);

      if (data.success) setSearchResults(data.body.items);
      else throw new Error(data.message);
    } catch {
      toast.error("An error occurred.");
    }
  };

  // Fetch search results on load
  useEffect(() => {
    if (isOpen) fetchSearchResults();
  }, [isOpen]);

  const handleSearch = useDebouncedCallback(async (query: string) => {
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
        <DialogContent className="h-full max-h-[450px] w-full content-start gap-2 p-1 sm:max-w-[650px]">
          <VisuallyHidden.Root>
            <DialogTitle>Search for anything</DialogTitle>
            <DialogDescription>Search for anything in the knowledgeBank by typing in the search box below.</DialogDescription>
          </VisuallyHidden.Root>

          {/* Search input*/}
          <DialogHeader>
            <div className="*:not-first:mt-2">
              <div className="relative">
                <Input
                  className="peer h-10 ps-9"
                  placeholder="Search"
                  type="text"
                  onChange={(e) => {
                    handleSearch(e.target.value);
                  }}
                />
                <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
                  <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
                </div>
              </div>
            </div>
          </DialogHeader>

          {/* Search results*/}
          <div className="flex h-full flex-col gap-1 overflow-y-auto">
            {/* No results found */}
            {searchResults && searchResults.length === 0 && (
              <p className="flex items-center gap-3 p-3">
                <OctagonAlert size={16} />
                No results found.
              </p>
            )}

            {/* Results list */}
            {searchResults &&
              searchResults.length > 0 &&
              searchResults.map((file) => (
                <div
                  onClick={() => openFile(file.id)}
                  key={file.id}
                  role="button"
                  className="hover:bg-muted-foreground/20 focus-visible:bg-muted-foreground/20 cursor-pointer rounded-lg bg-transparent p-3 transition-colors outline-none"
                >
                  <div className={`${file.description !== "" && "mb-2"} flex items-start gap-2`}>
                    <GetFileIcon fileType={file.fileType} className="mt-[3px]" />
                    <h3>{file.name}</h3>
                  </div>

                  {file.description !== "" && <p className="text-muted-foreground/80 text-sm">{file.description}</p>}
                </div>
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
