'use client'
import React from "react";
import { useEffect, useState, useRef } from "react";
import { TagArray } from "@/types/tag.type";
import TagListItem from "./tag-list-item";
import { useSearchParams, useRouter } from "next/navigation";
import { ListTagsPaged } from "@/actions/tagActions";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { useDebouncedCallback } from "use-debounce";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";

/**
 * 
 * @param userRole - Role of current user
 * @returns A page where the user, dependent on whether or not its an admin, can see, edit, promote, add, delete and search on tags
 */
export default function ListTags() {

    const router = useRouter();
    const searchParams = useSearchParams();
    const pageParam : string | null = searchParams.get("page");
    const initialPage : number = pageParam && !isNaN(Number(pageParam)) ? parseInt(pageParam) : 1;
    const emptyTagArray: TagArray = [];
    const [tags, setTags] = useState(emptyTagArray);
    const [pageNumber, setPageNumber] = useState(initialPage);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState("");
    const [pageCount, setPageCount] = useState(0);
    const [searchQuery, setSearchQuery] = useState("");
    const [refreshKey, setRefreshKey] = useState(0); // to trigger re-fetching of tags
    const fetches = useRef(0); // to keep track of the number of fetches

    useEffect(() => {
        const handleTagListUpdated = () => {
            setRefreshKey((prevKey) => (prevKey) + 1); // increment the refresh key to trigger a re-fetch
        };

        // Add the event listener to the window object
        window.addEventListener("tagListUpdated", handleTagListUpdated);

        // Cleanup the event listener on component unmount
        return () => {
            window.removeEventListener("tagListUpdated", handleTagListUpdated);
        };
    }, []);

    //load user page when the page number changes
    useEffect(() => {
        async function fetchTags() {
            const fetchAmount = fetches.current + 1;
            fetches.current = fetchAmount;
            setError("");
           try {
               const response = await ListTagsPaged(pageNumber, searchQuery);
                
               if(!response.success) {
                   setError(response.message);
                   return;
               }
               
               // only update the state if this is the latest fetch
               if(fetches.current === fetchAmount) {
                   setTags(response.body['tags'] || []);
                   setPageCount(response.body['pageCount'] || 0);
                   setIsLoading(false);
               }

           }
           catch {
            toast.error("Error loading users.");
            // only update the state if this is the latest fetch
            if (fetches.current === fetchAmount) {
                setIsLoading(false);
                setError("Error loading tags.");
                }
            }
        }

       fetchTags();
   }
   , [pageNumber, searchQuery, refreshKey]);

   const handleInputChange = useDebouncedCallback((event: React.ChangeEvent<HTMLInputElement>) => {
        const value = event.target.value;
        setPageNumber(1); //reset page number to 1 when searching
        setSearchQuery(value);
    }, 300);


   //update the url parameters when changing page, without reloading
   const updatePageInUrl = (newPage: number) => {
       const params : URLSearchParams = new URLSearchParams(searchParams.toString());
       if (newPage > 1) {
           params.set("page", newPage.toString());
       } else {
           params.delete("page"); 
       }
       router.replace(`?${params.toString()}`, { scroll: false });
   };

   //go to other page
   const goToPage = (newPage: number) => {
       setPageNumber(newPage);
       updatePageInUrl(newPage);
   };

   return (
        <div className="w-full">
            <div className="pb-2">
                <div className="relative w-full">
                <Input
                    className="peer h-10 ps-9"
                    placeholder="Search"
                    type="text"
                    onChange={handleInputChange}
                />
                <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
                    <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
                </div>
                </div>
            </div>
        {error.length > 0 ? 
        //display error message if any
        (
            <div className="text-center text-gray-500">
                {error}
            </div>
        ) : isLoading ? 
        //display if loading
        (
            <div className="text-center text-gray-500">
                Loading tags...
            </div>
        ) : tags.length > 0 ? 
        //display the tags
        (
            <div>
                <div className="flex flex-col space-y-4">
                    {tags.map((tag) => (
                        <div key={tag.id} className="mb-2 flex justify-between items-center w-full">
                            {
                                <TagListItem key={tag.id} tag={tag}/>
                            }
                        </div>
                    ))}
                </div>
                <div className="flex justify-between items-center border-t py-4 mt-4">
                    <Button
                        className="btn btn-primary"
                        onClick={() => goToPage(Math.max(pageNumber - 1, 1))}
                        disabled={pageNumber === 1}
                    >
                        Previous
                    </Button>
                    <p>Page: {pageNumber}/{pageCount}</p>
                    <Button
                        className="btn btn-primary"
                        onClick={() => goToPage(Math.min(pageNumber + 1, pageCount))}
                        disabled={pageNumber === pageCount}
                    >
                        Next
                    </Button>
                </div>
            </div>
        ) : (
            <div className="text-center text-gray-500">
                No tags found.
            </div>
        )}
    </div>
   )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


