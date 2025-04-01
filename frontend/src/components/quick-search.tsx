"use client";

import React, { useEffect, useState } from "react";
import { Button } from "./ui/button";
import { Dialog, DialogContent, DialogDescription, DialogHeader, DialogOverlay, DialogTitle, DialogTrigger } from "@/components/ui/dialog";
import Search from "@/icons/search-icon";
import { VisuallyHidden } from "radix-ui";
import { Input } from "./ui/input";
import { useHotkeys } from "react-hotkeys-hook";
import { useDebouncedCallback } from "use-debounce";
import { toast } from "sonner";
import { OctagonAlert } from "lucide-react";
import GetFileIcon from "./getFileIcon";
import Link from "next/link";

interface File {
  name: string;
  id: string;
  description: string;
  hash: string | null;
  fileType: string;
  createdAt: string;
  updatedAt: string;
}

export default function QuickSearch() {
  const [isOpen, setIsOpen] = useState(false);
  const [searchResults, setSearchResults] = useState<File[]>([]);

  // Keyboard shortcut
  useHotkeys("meta+k", () => setIsOpen(true));

  // Fetch search results
  const fetchSearchResults = async (query?: string) => {
    query = query?.trim();

    try {
      const response = await fetch(`http://localhost:8080/Search/search-full-text?${query ? `query=${query}&` : ""}pageIndex=1&pageSize=10`, {
        method: "GET",
        headers: {
          "Content-Type": "application/json",
        },
      });

      if (!response.ok) {
        toast.error("An error occurred while fetching search results.");
        return;
      }

      const data = await response.json();
      setSearchResults(data.files);
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
          <Button variant="outline" className="flex w-full items-center justify-between p-2">
            <div className="flex items-center gap-2">
              <Search className="h-4 w-4" />
              Search
            </div>
            <kbd className="text-xs">Cmd + K</kbd>
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
            {searchResults.length === 0 && (
              <p className="flex items-center gap-3 p-3">
                <OctagonAlert size={16} />
                No results found.
              </p>
            )}

            {/* Results list */}
            {searchResults.length > 0 &&
              searchResults.map((file) => (
                <Link
                  href={`/file/${file.id}`}
                  key={file.id}
                  className="hover:bg-muted-foreground/20 focus-visible:bg-muted-foreground/20 rounded-lg bg-transparent p-3 transition-colors outline-none"
                >
                  <div className={`${file.description !== "" && "mb-2"} flex items-start gap-2`}>
                    <GetFileIcon fileType={file.fileType} className="mt-[3px]" />
                    <h3>{file.name}</h3>
                  </div>

                  {file.description !== "" && <p className="text-muted-foreground/80 text-sm">{file.description}</p>}
                </Link>
              ))}
          </div>
        </DialogContent>
      </Dialog>
    </>
  );
}
