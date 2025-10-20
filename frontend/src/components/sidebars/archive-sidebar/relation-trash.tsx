"use client";

import React from "react";
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover";
import { Trash } from "lucide-react";
import { Button } from "@/components/ui/button";

interface RelationTrashProps
{
    removeAction: () => Promise<boolean>;
    successAction: () => void;
}

export function RelationTrash({removeAction, successAction}: RelationTrashProps)
{
    const [open, setOpen] = React.useState(false);

    const handleConfirm = async () =>
    {
        const success = await removeAction();
        if (success)
        {
            successAction();
            setOpen(false);
        }
    };

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger onClick={(e) => e.stopPropagation()}>
                <Trash className="text-red-500 ml-1" width={15} />
            </PopoverTrigger>
            <PopoverContent>
                <div>
                    <h1>Are you sure?</h1>
                    <div className="flex justify-between gap-2 mt-2">
                        <Button className="flex-1" onClick={() => setOpen(false)} variant="outline">No</Button>
                        <Button className="flex-1" onClick={handleConfirm} variant="destructive">Yes</Button>
                    </div>
                </div>
            </PopoverContent>
        </Popover>
    )
}