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
    const [isOpen, setIsOpen] = React.useState<boolean>(open);
    const [input, setInput] = React.useState<string>("");
    
    React.useEffect(() => 
    {
        if (onOpenChange) onOpenChange(open);
    
        if (isOpen) 
            setInput("");
    }, [isOpen]);
    
    async function onSubmit() 
    {
        const id: string = await UploadNewTag({ name: input } as TagCreateDto);
        
        if (onCreate) onCreate(id, input);
    }

    return (
        <Dialog open={isOpen} onOpenChange={setIsOpen}>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>Create New Tag</DialogTitle>
                </DialogHeader>
                
                <Input placeholder="Tag name..." value={input} onChange={(e) => setInput(e.target.value)} />
                
                <DialogFooter className="sm:justify-center">
                    <Button type="button" onClick={onSubmit}>Create</Button>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}