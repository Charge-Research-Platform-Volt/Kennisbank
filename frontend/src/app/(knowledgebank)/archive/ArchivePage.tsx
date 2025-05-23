"use client";

import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import { useDebouncedCallback } from "use-debounce";
import { toast } from "sonner";
import { useEffect, useState } from "react";
import { OctagonAlert } from "lucide-react";
import FilterButton from "../../../components/archive/filter-button";
import { ResourcePageWithTagsResponse } from "@/types/resource.type";
import ListResources from "@/components/list-resources";
import { useUserRole } from "@/context/user-role-context";
import { Button } from "@/components/ui/button";
import { useArchive } from "@/context/archive-provider";

type filterDto = {
  tagFilters: string[];
  startDate?: Date;
  endDate?: Date;
  archived?: boolean;
};

export default function ArchivePage() {
  //context provider
  const {tagFilters, setTagFilters} = useArchive();

  // State for search results, is null when no fetch has been completed yet, a string when an error occurs, or the fetch response.
  const [searchResults, setSearchResults] = useState<ResourcePageWithTagsResponse | null | string>(null);
  const [startYear, setStartYear] = useState<number | null>(null);
  const [endYear, setEndYear] = useState<number | null>(null);
  const [currentQuery, setCurrentQuery] = useState<string>("");
  const [refreshKey, setRefreshKey] = useState<number>(0); // Key to trigger re-fetching of data
  const [initialLoadingComplete, setInitialLoadingComplete] = useState<boolean>(false); // State to track if initial loading is complete
  const [showArchived, setShowArchived] = useState<boolean>(false); // State to track if archived items should be shown
  const { userRole } = useUserRole();
  
  // Fetch search results
  async function fetchQuery(query: string = currentQuery, tags: string[] = tagFilters, start: number | null = startYear, end: number | null = endYear) {
    query = query.trim();

    fetchFiles(
      `/api/Search/search-full-text?${`query=${query}&`}pageIndex=1&pageSize=100`,
      "POST",
      (response) => {
        setSearchResults(response);
        setInitialLoadingComplete(true);
      },
      "An error occurred while fetching search results.",
      undefined,
      {
        tagFilters: tags,
        startDate: start ? new Date(start, 0, 1) : undefined,
        endDate: end ? new Date(end, 11, 31, 23, 59, 59, 999) : undefined,
        archived: showArchived,
      },
    );
  }

  // Listen for resource list updates
  useEffect(() => {
      const handleResourceListUpdated = () => {
          setRefreshKey((prevKey) => (prevKey) + 1); // increment the refresh key to trigger a re-fetch
      };

      // Add the event listener to the window object
      window.addEventListener("resourceListUpdated", handleResourceListUpdated);

      // Cleanup the event listener on component unmount
      return () => {
          window.removeEventListener("resourceListUpdated", handleResourceListUpdated);
      };
  }, []);

  // Respond to search input changes
  // Debounce the search input to avoid too many requests
  const handleSearch = useDebouncedCallback(
    async (query: string | undefined = undefined, tags: string[] | undefined = undefined, start: number | null | undefined = undefined, end: number | null | undefined = undefined) => {
      fetchQuery(query, tags, start, end);
    },
    300,
  );

  // Call the search function when the input changes or when the refresh key changes
  useEffect(() => {
    handleSearch(currentQuery, tagFilters, startYear, endYear);
  }, [currentQuery, tagFilters, startYear, endYear, showArchived, refreshKey, handleSearch]);

  return (
    <>
      <div className="flex w-full items-center gap-2">
        <div className="relative flex-grow">
            <Input
            className="peer h-10 ps-9"
            placeholder="Search"
            type="text"
            onChange={(e) => {
                setCurrentQuery(e.target.value);
            }}
            />
            <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
            <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
            </div>
            <FilterButton
            onApplyAction={(tagFilters, startDate, endDate) => {
                setTagFilters(tagFilters);
                setStartYear(startDate);
                setEndYear(endDate);
            }}
            />
        </div>

        {userRole == "admin" && (
            <Button
            onClick={() => { setShowArchived((prev) => !prev); setInitialLoadingComplete(false); setSearchResults(null); }}
            variant="outline"
            className="h-10 whitespace-nowrap w-36"
            >
                {showArchived ? "Close trashbin" : "Show trashbin"}
            </Button>
        )}
        </div>


      <div className="flex pt-2">
        {typeof searchResults === "string" || searchResults instanceof String ? (
          <p className="flex items-center gap-3 p-3">
            <OctagonAlert size={16} /> {searchResults}
          </p>
        ) : (
          <ListResources data={searchResults ?? { message: "", pageIndex: 0, pageSize: 0, resources: [], responseType: "" }} initialLoadingComplete={initialLoadingComplete} />
        )}
      </div>
    </>
  );

  // Helper function for fetching files
  // Todo: Should maybe be abstracted higher up.
  async function fetchFiles(
    url: string,
    method: string = "GET",
    onSuccess: (response: ResourcePageWithTagsResponse) => void,
    errorMessage: string,
    onError: () => void = () => {
      toast.error(errorMessage);
      setSearchResults(errorMessage);
    },
    filter?: filterDto,
  ) {
    try {
      const response = await fetch(url, {
        credentials: "include",
        method: method,
        headers: {
          "Content-Type": "application/json",
        },
        ...(filter ? { body: JSON.stringify(filter) } : {}),
      });

      if (response.ok) {
        const data = await response.json();
        onSuccess(data);
      } else {
        console.error(response.body);
        onError();
      }
    } catch (e) {
      console.error(e);
      onError();
    }
  }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
