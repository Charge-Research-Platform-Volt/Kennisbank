"use client"

import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter, DialogClose } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import React from "react";
import { UploadNewTag } from "@/actions/uploadActions";
import { TagCreateDto } from "@/types/tag.type";

interface CreateTagDialogProps 
{
    open?: boolean;
    onOpenChange?: (open: boolean) => void;
    onCreate?: (id: string, name: string) => void;
}

export default function CreateTagDialog({open = false, onOpenChange, onCreate}: CreateTagDialogProps) 
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
    
        const id: string = await UploadNewTag({ name: input } as TagCreateDto);
        
        if (onCreate) onCreate(id, input);
        
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
                    <DialogTitle>Create New Tag</DialogTitle>
                </DialogHeader>
                
                <Input placeholder="Tag name..." value={input} onChange={(e) => setInput(e.target.value)} disabled={isUploading} onKeyDown={handleKeyDown} />
                
                <DialogFooter className="sm:justify-center">
                    <Button type="button" onClick={onSubmit} disabled={isUploading}>Create</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}