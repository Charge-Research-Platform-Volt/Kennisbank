"use client";

import DateFilterSlider from "@/components/filter-documents/filter-documents";
import TagSelectionDropdown from "@/components/TagSelectionDropdown";
import { Button } from "@/components/ui/button";
import { InputHeader } from "@/components/ui/Popup";
import { useEffect, useState } from "react";

export default function FilterPopup({ isVisible, onApplyAction, className }: { isVisible: boolean, onApplyAction: (tagFilters: string[], startDate: number, endDate: number) => void, className?: string }) {
    const [error, setError] = useState(false);

    const [standardizedTags, setStandardizedTags] = useState([]);
    const [userTags, setUserTags] = useState([]);

    const [startYear, setStartYear] = useState<number>(0);
    const [endYear, setEndYear] = useState<number>(0);
    const [tagFilters, setTagFilters] = useState<string[]>([]);

    useEffect(() => {
            setTagFilters([]);
            fetchTags();
        }
    , []);
    
    return (
        <>
            <div hidden={!isVisible} className={`z-50 flex-1 bg-white p-2 border-2 border-gray-100 rounded-sm ${className}`}>
                {!error ? (
                    <>
                        <InputHeader>Published between:</InputHeader>
                        <DateFilterSlider setStartYearAction={setStartYear} setEndYearAction={setEndYear} className="w-[450px] mt-[10px]" />
                        <TagSelectionDropdown onSelectionChangedAction={setTagFilters} createButton={false} userTags={userTags} standardizedTags={standardizedTags} />
                        <div className="flex justify-end mt-[10px]">
                            <Button onClick={() => onApplyAction(tagFilters, startYear, endYear)}>Apply</Button>
                        </div>
                    </>
                ) : (<p>Something went wrong, please reload the site</p>)}
            </div>
        </>        
    );

    async function fetchTags() {
        try{
            const standardizedTagsResponse = await fetch("http://localhost:8080/Tag/all-tags", {
                credentials: "include",
                method: "GET"
            });
    
            if(!standardizedTagsResponse.ok){
                throw new Error("Failed to fetch standardized tags");
            }

            setStandardizedTags(await standardizedTagsResponse.json());
    
            const userTagsResponse = await fetch("http://localhost:8080/UserTag/all-tags", {
                credentials: "include",
                method: "GET"
            });
            
            if(!userTagsResponse.ok){
                throw new Error("Failed to fetch user tags");
            }

            setUserTags(await userTagsResponse.json());


        }
        catch(error) {
            console.error("Error fetching tags:", error);
            setError(true);
        }
    
    }
}
