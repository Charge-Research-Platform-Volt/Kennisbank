"use client"

import ListDocuments from "@/components/list-documents";
import { DocumentPageResponseSchema } from "@/types/document.type";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import { useDebouncedCallback } from "use-debounce";
import { toast } from "sonner";
import { useEffect, useRef, useState } from "react";
import { OctagonAlert } from "lucide-react";
import GetFileIcon from "@/components/getFileIcon";
import Link from "next/link";
import { FetchWithValidation } from "@/lib/fetchWithValidation";

interface File {
  name: string;
  id: string;
  description: string | null;
  hash: string | null;
  fileType: string;
  createdAt: string;
  updatedAt: string;
}

export default function ArchivePage() {
  const isInitial = useRef(true)

  const [searchResults, setSearchResults] = useState<File[]>([]);

  useEffect(() => {
    initialFetch()
  }, [])

  async function initialFetch() {
    const data = await FetchWithValidation(DocumentPageResponseSchema, "http://backend:8080/Storage/list-all")
    if(isInitial) setSearchResults(data.data!.files)
  }

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
      isInitial.current = false;
      setSearchResults(data.files);
    } catch {
      toast.error("An error occurred.");
    }
  };

  const handleSearch = useDebouncedCallback(async (query: string) => {
    fetchSearchResults(query);
  }, 300);

  return <>
      <div className="*:not-first:mt-2">
              <div className="relative">
                <Input
                  className="peer h-10 ps-9"
                  placeholder="Search"
                  type="text"
                  onChange={(e) => {
                    initialFetch();
                  }}
                />
                <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
                  <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
                </div>
              </div>
          </div>
          <div className="flex h-full flex-col gap-1 overflow-y-auto">
            {/* No results found */}
            {searchResults.length === 0 && (
              <p className="flex items-center gap-3 p-3">
                <OctagonAlert size={16} />
                {isInitial.current ? 'No results found.' : 'Error loading documents'}
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
  </>
}
