"use client"

import React from "react";
import { Dialog, DialogTrigger, DialogContent, DialogTitle } from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";
import { setProperty } from "@/actions/right-sidebarActions";
import { useSidebar } from "@/context/sidebar-provider"

export default function Edit({
    property,
    currentText,
    setNewText,
} : {
    property: string;
    currentText: string | null;
    setNewText: (val: string) => void;
})
{
    const { currentId, currentType } = useSidebar();
    const [text, setText] = React.useState(currentText || "");
    const [isOpen, setIsOpen] = React.useState(false);
    const [ isLoading, setIsLoading ] = React.useState(false);

    // Reset text when dialog opens
    React.useEffect(() => {
        if (isOpen) {
            setText(currentText || "");
        }
    }, [isOpen, currentText]);

    async function handleSave () {
        if (isLoading) return; // Prevent double-clicks
        
        setIsLoading(true);

        try {
        await setProperty(property, currentId, text as string, currentType);
        setNewText(text as string);
        setIsOpen(false);
        } catch (error) {
            console.error("Error saving property:", error);
        } finally {
        setIsLoading(false);
        }
    };

    return (
        <Dialog  open={isOpen} onOpenChange={setIsOpen}>
            <DialogTrigger className="select-none">
                <div className="hover:underline cursor-pointer select-none">
                    Edit
                </div>
            </DialogTrigger>
            <DialogContent className="w-[600px] h-[400px] flex flex-col">
                <DialogTitle>
                    Edit {property}
                </DialogTitle>
                <div className="flex-1 flex flex-col py-4">
                    <textarea
                        value={text}
                        onChange={(e) => setText(e.target.value)}
                        className="flex-1 w-full p-3 border border-gray-300 rounded-md resize-none focus:outline-none focus:ring-1 focus:border-transparent"
                        placeholder={`Enter ${property.toLowerCase()}...`}
                        autoFocus
                    />
                </div>
                <Button
                    onClick={handleSave}
                    className="px-4 py-2"
                    disabled={isLoading}>
                    Save
                </Button>
            </DialogContent>
        </Dialog>
    )
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
