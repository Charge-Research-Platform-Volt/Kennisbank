"use client";

import DateFilterSlider from "@/components/filter-documents/date-filter-slider";
import { Button } from "@/components/ui/button";
import { InputHeader } from "@/components/ui/Popup";
import TagSelectionDropdown from "@/components/uploadComponents/TagSelectionDropdown";
import { useEffect, useState } from "react";

export default function FilterPopup({ isVisible, onApplyAction, className }: { isVisible: boolean, onApplyAction: (tagFilters: string[], startDate: number, endDate: number) => void, className?: string }) {
    const [error, setError] = useState(false);

    const [tags, setTags] = useState([]);

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
                        <TagSelectionDropdown onSelectionChangedAction={setTagFilters} createButton={false} tags={tags} />
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
            const response = await fetch("/api/Tags/all-tags", {
                credentials: "include",
                method: "GET"
            });
    
            if(!response.ok){
                console.error("Error fetching tags:", response.statusText);
                setError(true);
                return;
            }

            setTags(await response.json());
        }
        catch(error) {
            console.error("Error fetching tags:", error);
            setError(true);
        }
    
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


