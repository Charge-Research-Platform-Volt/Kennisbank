"use client";

import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import { useDebouncedCallback } from "use-debounce";
import { toast } from "sonner";
import { useEffect, useState } from "react";
import { OctagonAlert } from "lucide-react";
import FilterButton from "./components/filter-button";
import { ResourcePageWithTagsResponse } from "@/types/resource.type";
import ListResources from "@/components/list-resources";

type filterDto = {
  tagFilters: string[];
  startDate?: Date;
  endDate?: Date;
};

export default function ArchivePage() {
  // State for search results, is null when no fetch has been completed yet, a string when an error occurs, or the fetch response.
  const [searchResults, setSearchResults] = useState<ResourcePageWithTagsResponse | null | string>(null);
  const [startYear, setStartYear] = useState<number | null>(null);
  const [endYear, setEndYear] = useState<number | null>(null);
  const [tagFilters, setTagFilters] = useState<string[]>([]);
  const [currentQuery, setCurrentQuery] = useState<string>("");

  // Fetch initial files
  useEffect(() => {
    fetchFiles(
      "http://localhost:8080/Storage/list-all",
      "GET",
      (response) => {
        if (searchResults === null) setSearchResults(response);
      },
      "An error occurred while fetching initial files.",
    );
  }, []);

  // Fetch search results
  async function fetchQuery(query: string = currentQuery, tags: string[] = tagFilters, start: number | null = startYear, end: number | null = endYear) {
    query = query.trim();

    fetchFiles(
      `http://localhost:8080/Search/search-full-text?${`query=${query}&`}pageIndex=1&pageSize=100`,
      "POST",
      (response) => {
        setSearchResults(response);
      },
      "An error occurred while fetching search results.",
      undefined,
      {
        tagFilters: tags,
        startDate: start ? new Date(start, 0, 1) : undefined,
        endDate: end ? new Date(end, 11, 31, 23, 59, 59, 999) : undefined,
      },
    );
  }

  // Respond to search input changes
  // Debounce the search input to avoid too many requests
  const handleSearch = useDebouncedCallback(
    async (query: string | undefined = undefined, tags: string[] | undefined = undefined, start: number | null | undefined = undefined, end: number | null | undefined = undefined) => {
      fetchQuery(query, tags, start, end);
    },
    300,
  );

  return (
    <>
      <div className="*:not-first:mt-2">
        <div className="relative w-full">
          <Input
            className="peer h-10 ps-9"
            placeholder="Search"
            type="text"
            onChange={(e) => {
              handleSearch(e.target.value);
              setCurrentQuery(e.target.value);
            }}
          />
          <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
            <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
          </div>
          <FilterButton
            onApplyAction={(tagFilters, startDate, endDate) => {
              console.log("Filter applied:", tagFilters, startDate, endDate);
              setTagFilters(tagFilters);
              setStartYear(startDate);
              setEndYear(endDate);
              handleSearch(undefined, tagFilters, startDate, endDate);
            }}
          />
        </div>
      </div>

      <div className="flex py-2">
        {typeof searchResults === "string" || searchResults instanceof String ? (
          <p className="flex items-center gap-3 p-3">
            <OctagonAlert size={16} /> {searchResults}
          </p>
        ) : (
          <ListResources data={searchResults ?? { message: "", pageIndex: 0, pageSize: 0, resources: [], responseType: "" }} />
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
