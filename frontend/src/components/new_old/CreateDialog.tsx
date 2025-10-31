"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter, DialogClose } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import React from "react";

interface CreateDialogProps 
{
    open?: boolean;
    title?: string;
    placeholder?: string;
    onOpenChange?: (open: boolean) => void;
    onCreate?: (name: string) => void;
}

/**
 * @summary Simple dialog with a single input field to use to create new resources.
 * @param open Determines if the dialog is open
 * @param onOpenChange This function is called when the open state changes
 * @param title The title of the dialog
 * @param placeholder The placeholder for the input field in the dialog
 * @param onCreate This function is called when the create button is clicked
 * @returns 
 */
export default function CreateDialog({open = false, title = "Create New", placeholder = "Name...", onOpenChange, onCreate}: CreateDialogProps) 
{
    const [input, setInput] = React.useState<string>("");
    const [isUploading, setIsUploading] = React.useState<boolean>(false);
    
    React.useEffect(() => 
    {
        if (onOpenChange) onOpenChange(open);
    
        if (open) 
        {
            setInput("");
            setIsUploading(false);
        }setInput("");
    }, [open]);
    
    async function onSubmit() 
    {
        setIsUploading(true);
            
        if (onCreate) onCreate(input);
        
        open = false;
    }
    
    function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) 
    {
        // Check if enter is pressed
        if (e.key === 'Enter') 
        {
            e.preventDefault();
            onSubmit();
        }
    }

    return (
        <Dialog open={open} onOpenChange={onOpenChange}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{title}</DialogTitle>
                </DialogHeader>
                
                <Input placeholder={placeholder} value={input} onChange={(e) => setInput(e.target.value)} disabled={isUploading} onKeyDown={handleKeyDown} />
                
                <DialogFooter className="sm:justify-center">
                    <Button type="button" onClick={onSubmit} disabled={isUploading}>Create</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


