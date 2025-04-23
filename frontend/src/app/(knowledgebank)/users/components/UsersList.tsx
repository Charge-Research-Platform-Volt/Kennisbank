"use client";

import { useEffect, useRef, useState } from "react";
import { ListUsersPaged } from "@/actions/userActions";
import { toast } from "sonner";
import { UserArray } from "@/types/user.type";
import { Button } from "@/components/ui/button";
import { useSearchParams } from "next/navigation";
import { useRouter } from "next/navigation";
import UserListItem from "./UserListItem";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import { useDebouncedCallback } from "use-debounce";

export default function UsersList() {
    const router = useRouter();
    const searchParams = useSearchParams();
    const pageParam : string | null = searchParams.get("page");
    const initialPage : number = pageParam && !isNaN(Number(pageParam)) ? parseInt(pageParam) : 1;
    const emptyUserArray: UserArray = [];
    const [users, setUsers] = useState(emptyUserArray);
    const [pageNumber, setPageNumber] = useState(initialPage);
    const [isLoading, setIsLoading] = useState(true);
    const [error, setError] = useState("");
    const [pageCount, setPageCount] = useState(0);
    const [searchQuery, setSearchQuery] = useState("");
    const fetches = useRef(0);

    //load user page when the page number changes
    useEffect(() => {
         async function fetchUsers() {
            setIsLoading(true);
            const fetchAmount = fetches.current + 1;
            fetches.current = fetchAmount;
            setError("");
            try {
                const response = await ListUsersPaged(pageNumber, searchQuery);
                console.log("query: ", searchQuery);
                
                if(!response.success) {
                    setError(response.message);
                    return;
                }

                // only update the state if this is the latest fetch
                if(fetchAmount === fetches.current) {
                    console.log("Fetched users: ", response.users);
                    setUsers(response.users || []);
                    setPageCount(response.pageCount || 0);
                    setIsLoading(false);
                }
            }
            catch {
               toast.error("Error loading users.");
               // only update the state if this is the latest fetch
               if(fetchAmount === fetches.current) {
                    setIsLoading(false);
                }
            } 
        }

        fetchUsers();
    }
    , [pageNumber, searchQuery]);

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
    }

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
                    Loading users...
                </div>
            ) : users.length > 0 ? 
            //display the users
            (
                <div>
                    <div className="flex flex-col space-y-2">
                        {users.map((user) => (
                            <UserListItem key={crypto.randomUUID()} user={user} />
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
                    No users found.
                </div>
            )}
        </div>
    )    
}
  

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


