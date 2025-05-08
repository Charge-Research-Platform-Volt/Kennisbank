"use client";

import DateFilterSlider from "@/components/filter-documents/date-filter-slider";
import { Button } from "@/components/ui/button";
import TagSelectionDropdown from "@/components/uploadComponents/TagSelectionDropdown";
import { useEffect, useState } from "react";
import {
    DialogContent,
    DialogDescription,
    DialogFooter,
    DialogHeader,
    DialogTitle,
} from "@/components/ui/dialog"

import { Label } from "@/components/ui/label";

/**
 * 
 * @param onApplyAction - Function to execute when the filter is applied
 * @param [className] - Styling from parent
 * @returns A popup where users can filter on date and tags
 */
export default function FilterPopup({ onApplyAction, className }: { onApplyAction: (tagFilters: string[], startDate: number, endDate: number) => void, className?: string }) {
    const [startYear, setStartYear] = useState<number>(0); // State containing the start date to filter on
    const [endYear, setEndYear] = useState<number>(0); // State containing the end date to filter on
    const [tagFilters, setTagFilters] = useState<string[]>([]); // State containing the tags to filter on

    useEffect(() => {
            setTagFilters([]);
        }
    , []);

    return (
        <DialogContent className={className}>
            <DialogHeader>
                <DialogTitle>Filter</DialogTitle>
                    <DialogDescription>
                        Choose filters to filter resources on
                        </DialogDescription>
                            </DialogHeader>
                            {/* Items for filtering */}
                            <div className="grid gap-4 py-4">
                                <div className="grid grid-cols-4 items-center gap-4">
                                    <Label htmlFor="first tag" className="text-right">
                                    Published Between:
                                    </Label>
                                        <DateFilterSlider setStartYearAction={setStartYear} setEndYearAction={setEndYear} className="w-[450px] mt-[10px]" />
                                    </div>
                                <div className="grid grid-cols-4 items-center gap-4">
                                    <Label htmlFor="second tag" className="text-right">
                                    Tags:
                                    </Label>
                                        <TagSelectionDropdown onSelectionChangedAction={setTagFilters} createButton={false} />
                                    </div>
                            </div>
            {/* Button to apply filter */}
            <DialogFooter>
                <Button onClick={() => onApplyAction(tagFilters, startYear, endYear)}>Apply</Button>
            </DialogFooter>
        </DialogContent>

    );
}
// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)