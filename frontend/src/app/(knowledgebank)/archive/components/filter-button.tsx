"use client";

import { Button } from "@/components/ui/button";
import Filter from "@/icons/filter";
import FilterPopup from "./filter-popup";
import { useEffect, useRef, useState } from "react";

export default function FilterButton({onApplyAction}: {onApplyAction: (tagFilters: string[], startDate: number, endDate: number) => void}) {
  const [isOpen, setIsOpen] = useState(false);
  const filterButtonRef = useRef<HTMLDivElement>(null);

  //close popup when clicking outside of it
  useEffect(() => { 
    const handleClickOutside = (event: MouseEvent) => {
      if (filterButtonRef.current && !filterButtonRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };
    document.addEventListener("mousedown", handleClickOutside);

    // Cleanup the event listener on component unmount
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  return (
    <>
      <div ref={filterButtonRef} className="absolute inset-y-0 right-2 flex items-center justify-center ">
        <Button
              className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
              variant="default"
              type="button"
              onClick={() => setIsOpen(!isOpen)}
            >
              <Filter className="h-6 w-4" aria-hidden="true" fill="currentColor" />
        </Button>
        <FilterPopup onApplyAction={onApplyAction} isVisible={isOpen} className="absolute top-full mt-2 right-0" />
      </div>
    </>
  )
}
