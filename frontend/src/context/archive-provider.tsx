"use client";

import React from "react";
import { usePathname, useSearchParams, useRouter } from "next/navigation";
import { ResourceItem, ResourceResponse } from "@/components/archive/resources-grid";
import { GridRequest, GridRequestSchema } from "@/types/gridRequest.type";
import { NewApiResponse } from "@/types/apiResponse.type";

export type ArchiveContextType = {
  // Filters
  resetFilters: () => void;
  tagFilter: string[];
  setTagFilter: (filter: string[]) => void;
  typeFilter: string[];
  setTypeFilter: (typeFilter: string[]) => void;
  publicationDateRangeMin: string;
  setPublicationDateRangeMin: (publicationDateRangeMin: string) => void;
  publicationDateRangeMax: string;
  setPublicationDateRangeMax: (publicationDateRangeMax: string) => void;
  regionFilter: string[];
  setRegionFilter: (filter: string[]) => void;

  // Search
  searchQuery: string;
  searchInput: string;
  setSearchInput: (searchInput: string) => void;

  // Pagination
  currentPage: number;
  goToPage: (pageIndex: number) => void;
  pageSize: number;
  setPageSize: (pageSize: number) => void;
  totalItems: number;
  setTotalItems: (totalItems: number) => void;
  totalPages: number;

  // States
  trashOpen: boolean;
  setTrashOpen: (open: boolean) => void;

  // Data
  rowData: ResourceItem[];
  setRowData: (rowData: ResourceItem[]) => void;

  // Loading state
  loading: boolean;
  setLoading: (loading: boolean) => void;

  // Result mode "search-results" or "general-results"
  // this will change after the fech is done
  mode: "search-results" | "general-results";
  setMode: (mode: "search-results" | "general-results") => void;

  // Sort
  sortBy: string;
  setSortBy: (sortBy: string) => void;
  sortDirection: "asc" | "desc";
  setSortDirection: (sortDirection: "asc" | "desc") => void;

  // Triggers
  gridReloadTrigger: boolean;
  triggerGridReload: () => void;

  // Fetch data function
  fetchData: () => Promise<void>;
};

const ArchiveContext = React.createContext<ArchiveContextType | undefined>(undefined);

export const useArchive = (): ArchiveContextType => {
  const context = React.useContext(ArchiveContext);

  if (!context) throw new Error("useArchive must be used within a ArchiveProvider");

  return context;
};

export const ArchiveProvider = ({ children }: { children: React.ReactNode }) => {
  const searchParams = useSearchParams();
  const router = useRouter();
  const pathname = usePathname();

  // Filters
  const [tagFilter, setTagFilter] = React.useState<string[]>(searchParams.getAll("tagFilter"));
  const [typeFilter, setTypeFilter] = React.useState<string[]>(searchParams.getAll("typeFilter").length > 0 ? searchParams.getAll("typeFilter") : ["resource", "person", "organisation"]);
  const [publicationDateRangeMin, setPublicationDateRangeMin] = React.useState<string>(searchParams.get("pubdateMin") || "");
  const [publicationDateRangeMax, setPublicationDateRangeMax] = React.useState<string>(searchParams.get("pubdateMax") || "");
  const [regionFilter, setRegionFilter] = React.useState<string[]>(searchParams.getAll("regionFilter"));

  const isResettingRef = React.useRef<boolean>(false);

  // Search
  const [searchInput, setSearchInput] = React.useState<string>(searchParams.get("query") || "");
  const [searchQuery, setSearchQuery] = React.useState<string>(searchParams.get("query") || "");

  // Pagination
  const [currentPage, setCurrentPage] = React.useState<number>(Number(searchParams.get("page")) || 1);
  const [pageSize, setPageSize] = React.useState<number>(20);
  const [totalItems, setTotalItems] = React.useState<number>(0);
  const [totalPages, setTotalPages] = React.useState<number>(1);

  // States
  const [trashOpen, setTrashOpen] = React.useState<boolean>(false);

  // Data
  const [rowData, setRowData] = React.useState<ResourceItem[]>([]);
  const [loading, setLoading] = React.useState(false);

  // Result mode
  const [mode, setMode] = React.useState<"search-results" | "general-results">("general-results");

  // Sort states -- Controlled by AgGrid
  const [sortBy, setSortBy] = React.useState<string>("");
  const [sortDirection, setSortDirection] = React.useState<"asc" | "desc">("asc");

  // Triggers
  const [gridReloadTrigger, setGridReloadTrigger] = React.useState<boolean>(false);

  // Update search parameters helper
  const updateParam = React.useCallback(
    (key: string, value?: string | string[] | undefined) => {
      if (isResettingRef.current) return;

      const params = new URLSearchParams(searchParams.toString());

      // Delete existing value
      params.delete(key);

      // Only add when value is provided
      if (value) {
        // Handle arrays
        if (Array.isArray(value)) value.forEach((item) => params.append(key, item));
        // Single item
        else params.set(key, String(value));
      }

      // Update address bar
      router.replace(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams],
  );

  // Debounce search query
  React.useEffect(() => {
    const timer = setTimeout(() => {
      setSearchQuery(searchInput);
    }, 500); // 500 ms delay

    return () => clearTimeout(timer);
  }, [searchInput]);

  // Reset to first page when filters/search change
  React.useEffect(() => {
    setCurrentPage((prev) => (prev !== 1 ? 1 : prev));
  }, [searchQuery, typeFilter, tagFilter, publicationDateRangeMax, publicationDateRangeMin]);

  // Calculate total pages on total items or page size change
  React.useEffect(() => {
    setTotalPages(Math.ceil(totalItems / pageSize));
  }, [totalItems, pageSize]);

  // Page helper
  const goToPage = (pageIndex: number) => {
    if (pageIndex >= 1) setCurrentPage(pageIndex <= totalPages ? pageIndex : totalPages);
  };

  // Filter reset function
  const resetFilters = () => {
    isResettingRef.current = true;

    setTypeFilter(["resource", "person", "organisation"]);
    setPublicationDateRangeMax("");
    setPublicationDateRangeMin("");
    setTagFilter([]);
    setRegionFilter([]);

    const params = new URLSearchParams();

    // Only keep non-filter params
    if (searchQuery) params.set("query", searchQuery);
    if (currentPage > 1) params.set("page", String(currentPage));

    // Update address bar
    router.replace(`${pathname}?${params.toString()}`);

    // Set isResetting to false in the next tick to prevent race conditions
    setTimeout(() => (isResettingRef.current = false), 0);
  };

  // Fetch data function
  const fetchData = React.useCallback(async () => {
    // Only fetch data if we are on the archive page
    if (pathname !== "/archive") return;

    setLoading(true);

    try {
      // Create the request
      const request: GridRequest = GridRequestSchema.parse({
        pageIndex: currentPage,
        pageSize: pageSize,
        searchQuery: searchQuery || undefined,
        sortBy: sortBy || undefined,
        sortDirection: sortDirection || undefined,
        filterOptions: {
          typeFilter: typeFilter || undefined,
          pubdateMin: publicationDateRangeMin || undefined,
          pubdateMax: publicationDateRangeMax || undefined,
          tagFilter: tagFilter || undefined,
          regionFilter: regionFilter || undefined,
        },
      });

      // Fetch the data from the backend
      const response = trashOpen
        ? await fetch(`/api/resources/list?properties=${encodeURIComponent('Id as id,"resource" as type,Title as name,PublicationDate,FileExt as fileType,TrashDate as trashDate')}&trash=true`, {
            credentials: "include",
          })
        : await fetch(`/api/resources/grid`, {
            method: "POST",
            credentials: "include",
            headers: {
              "Content-Type": "application/json",
            },
            body: JSON.stringify(request),
          });

      if (!response.ok) throw new Error(`HTTP error! Status: ${response.status}`);

      const data: NewApiResponse<ResourceResponse | ResourceItem[]> = await response.json();

      if (data.success) {
        setRowData(trashOpen ? (data.body as ResourceItem[]) : (data.body as ResourceResponse).items);
        setTotalItems(trashOpen ? (data.body as ResourceItem[]).length : (data.body as ResourceResponse).totalCount);
      } else {
        console.error("API returned error:", data.message);
        setRowData([]);
        setTotalItems(0);
      }
    } catch (error) {
      console.error("Failed to fetch data:", error);
      setRowData([]);
      setTotalItems(0);
    } finally {
      setLoading(false);
      setMode(searchQuery ? "search-results" : "general-results");
    }
  }, [currentPage, pageSize, searchQuery, typeFilter, publicationDateRangeMax, publicationDateRangeMin, sortBy, sortDirection, tagFilter, regionFilter, setTotalItems, trashOpen, pathname]);

  // Fetch data when dependencies change or when trigger is activated
  React.useEffect(() => {
    fetchData();
  }, [fetchData, gridReloadTrigger]);

  // Search param updaters for each filter, search and pagination:
  React.useEffect(() => updateParam("tagFilter", tagFilter), [updateParam, tagFilter]);
  React.useEffect(() => updateParam("typeFilter", typeFilter.length === 3 ? undefined : typeFilter), [updateParam, typeFilter]);
  React.useEffect(() => updateParam("pubdateMin", publicationDateRangeMin), [updateParam, publicationDateRangeMin]);
  React.useEffect(() => updateParam("pubdateMax", publicationDateRangeMax), [updateParam, publicationDateRangeMax]);
  React.useEffect(() => updateParam("regionFilter", regionFilter), [updateParam, regionFilter]);
  React.useEffect(() => updateParam("query", searchQuery), [updateParam, searchQuery]);
  React.useEffect(() => updateParam("page", currentPage > 1 ? String(currentPage) : undefined), [updateParam, currentPage]);

  // Define triggerse
  const triggerGridReload = () => {
    setGridReloadTrigger((prev) => !prev);
  };

  return (
    <ArchiveContext.Provider
      value={{
        // Filters
        resetFilters,
        tagFilter,
        setTagFilter,
        typeFilter,
        setTypeFilter,
        publicationDateRangeMin,
        setPublicationDateRangeMin,
        publicationDateRangeMax,
        setPublicationDateRangeMax,
        regionFilter,
        setRegionFilter,

        // Search
        searchQuery,
        searchInput,
        setSearchInput,

        // Pagination
        currentPage,
        goToPage,
        pageSize,
        setPageSize,
        totalItems,
        setTotalItems,
        totalPages,

        // Data
        rowData,
        setRowData,

        // Loading state
        loading,
        setLoading,

        // Result mode
        mode,
        setMode,

        // States
        trashOpen,
        setTrashOpen,

        // Sort
        sortBy,
        setSortBy,
        sortDirection,
        setSortDirection,

        // Triggers
        gridReloadTrigger,
        triggerGridReload,

        // Fetch data function
        fetchData,
      }}
    >
      {children}
    </ArchiveContext.Provider>
  );
};
