"use client";

import { Button } from "@/components/ui/button";
import Filter from "@/icons/filter";
import FilterPopup from "./filter-popup";
import { useState } from "react";
import {
  Dialog,
  DialogTrigger,
} from "@/components/ui/dialog"
/**
 * 
 * @param onApplyAction - Action given to the child component that updates the state in the parent component of this one
 * @returns A button which opens a popup to filter when pressed
 */
export default function FilterButton({onApplyAction}: {onApplyAction: (tagFilters: string[], startDate: number, endDate: number) => void}) {
  const [isOpen, setIsOpen] = useState(false);

  return (
    <>
      <div className="absolute inset-y-0 right-2 flex items-center justify-center ">
        <>
          {/* Popup for filtering */}
          <Dialog open={isOpen} onOpenChange={setIsOpen} modal>
              {/* Button to trigger the filter popup */}
              <DialogTrigger asChild>
                  <Button className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground" data-testid="open">
                    <Filter className="h-6 w-4" aria-hidden="true" fill="currentColor" />
                  </Button>
              </DialogTrigger>
              <FilterPopup onApplyAction={onApplyAction} onCloseAction={() => setIsOpen(false)} className="sm:max-w-[700px]" />
          </Dialog>
        </>
      </div>
    </>
  )
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


