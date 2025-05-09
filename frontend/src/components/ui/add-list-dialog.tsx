"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter, DialogClose } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableRow } from "@/components/ui/table"
import { toast } from "sonner";
import { ScrollArea } from "@/components/ui/scroll-area"
import { X } from "lucide-react"
import PopupIcon from "@/icons/popup-icon"
  

interface AddListDialogProps extends Omit<React.ComponentPropsWithoutRef<typeof Button>, 'onChange'> 
{
    title: string;
    placeholder: string;
    inputPlaceholder?: string;
    emptyText?: string;
    onChange?: (value: string[]) => void;
    validateInput?: (value: string) => boolean;
    parseForList?: (value: string) => string;
}

/**
 * @summary A dialog to add multiple items
 * @param title The title of the dialog
 * @param placeholder The placeholder of the dialog trigger
 * @param inputPlaceholder The placeholder of the input in the dialog
 * @param emptyText The text displayed in the dialog when nothing has been added yet
 * @param onChange Function to be called when the set changes
 * @param validateInput Function to be used to validate the input
 * @param parseForList Function that parses all items in the set to a nice display format to be displayed in the dialog trigger
 */
export function AddListDialog({title, placeholder, inputPlaceholder, emptyText, onChange, validateInput, parseForList}: AddListDialogProps) 
{
    const [list, setList] = React.useState<string[]>([]);
    const [input, setInput] = React.useState<string>("");
    
    // Add the item to the list
    const handleAddItem = (): void => 
    {
        setInput(input.trim());
        
        // Check if not empty after trimming
        if (input)
        {
            // If input already exists, show toast
            if (list.includes(input)) 
            {
                toast.error("This source already exists!");
            }
            
            // Check if input is valid
            else if (validateInput ? !validateInput(input) : false)
            {
                toast.error("This input is invalid!");
            }
            
            // Add the input to the list
            else 
            {
                const newList = [...list, input];
                setList(newList);
                if (onChange) onChange(newList);
            }
            
            // Clear the input
            setInput("");
        }
    }
    
    // Removes the item from the list
    const handleRemoveItem = (value: string): void =>
    {
        const newList = list.filter((i) => i !== value);
        setList(newList);
        if (onChange) onChange(newList);
    }
    
    // Handles enter to add
    const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>): void => 
    {
        if (e.key === "Enter") 
        {
            e.preventDefault();
            handleAddItem();
        }
    }

    return (
        <Dialog>
            <DialogTrigger>
                <div className="relative w-full cursor-pointer">
                    <Input 
                        value={list.length > 0 ? list.map(parseForList ? parseForList : (item) => item).join('; ') : ""} 
                        placeholder={placeholder} 
                        readOnly={true} 
                        className="caret-transparent pr-10 cursor-pointer"
                        tabIndex={-1} 
                    />
                    <PopupIcon className="absolute right-3 top-1/2 transform -translate-y-1/2 text-gray-500 h-4 w-4" />
                </div>
            </DialogTrigger>
            <DialogContent>
                <DialogHeader>
                    <DialogTitle>{title}</DialogTitle>
                </DialogHeader>
                
                <div className="flex flex-col sm:flex-row gap-2">
                    <Input className="flex-1" placeholder={inputPlaceholder ? inputPlaceholder : "New value..."} value={input} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setInput(e.target.value)} onKeyDown={handleKeyDown} autoFocus={true} />
                    <Button className="sm:w-24" type="button" onClick={handleAddItem}>Add</Button>
                </div>
                
                <hr />
                
                {
                    list.length > 0 ?
                    
                    <ScrollArea className="w-full h-full max-h-[50vh]">
                        <Table>
                            <TableBody>
                                {
                                    list.map((item) => (
                                        <TableRow key={item} className="hover:bg-transparent">
                                            <TableCell title={item} className="truncate max-w-xs cursor-default">{item}</TableCell>
                                            <TableCell className="w-10 text-right">
                                                <X onClick={() => handleRemoveItem(item)} className="cursor-pointer text-gray-500 hover:text-red-500 transition-colors ml-auto" size={18} />
                                            </TableCell>
                                        </TableRow>
                                    ))
                                }
                            </TableBody>
                        </Table>
                    </ScrollArea>
                    
                    :
                    
                    <span className="w-full text-center">{emptyText ? emptyText : "Nothing added yet."}</span>
                }
                
                <DialogFooter className="sm:justify-center">
                    <DialogClose asChild>
                        <Button type="button">Close</Button>
                    </DialogClose>
                </DialogFooter>
            </DialogContent>
        </Dialog>
    );
}