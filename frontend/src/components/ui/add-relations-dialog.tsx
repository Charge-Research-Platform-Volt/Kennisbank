"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter, DialogClose } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableRow } from "@/components/ui/table"
import { ScrollArea } from "@/components/ui/scroll-area"
import { SelectOption, Selection } from "./selection"
import { RelatedEntry } from "@/types/uploadTypes"
  
// Remove the extension of button props to avoid the type conflict
interface AddRelationsDialogProps {
    title: string;
    placeholder: string;
    options: SelectOption[];
    emptyText?: string;
    onChange?: (value: RelatedEntry[]) => void;
    value?: RelatedEntry[];
    // Add missing props from react-hook-form
    onBlur?: () => void;
    name?: string;
    disabled?: boolean;
    ref?: React.Ref<unknown>;
}

export function AddRelationsDialog({title, placeholder, options, emptyText, onChange}: AddRelationsDialogProps) 
{
    const [list, setList] = React.useState<RelatedEntry[]>([]);
    
    function handleSelectionChange(value: string | string[])
    {
        const newList: RelatedEntry[] = [];
        
        const arr = Array.isArray(value) ? value : [value];
        arr.forEach((item) => 
        {
            newList.push(list.find((entry) => entry.Id === item) || { Id: item, Relation: undefined} as RelatedEntry);
        });
        
        setList(newList);
        if (onChange) onChange(newList);
    }
    
    function handleRelationDefinition(id: string, value: string) 
    {
        const newList: RelatedEntry[] = list.map((entry) => entry.Id === id ? { Id: entry.Id, Relation: value } : entry);
        
        setList(newList);
        if (onChange) onChange(newList);
    }

    return (
        <div className="flex flex-col sm:flex-row gap-2">
            <Selection placeholder={placeholder} options={options} onChange={handleSelectionChange} multiSelect={true} />
            
            <Dialog>
                <DialogTrigger asChild>
                    <Button className="sm:w-40" type="button">Define Relations</Button>
                </DialogTrigger>
                <DialogContent>
                    <DialogHeader>
                        <DialogTitle>{title}</DialogTitle>
                    </DialogHeader>
                    
                    <hr />
                    
                    {list.length > 0 ? (
                        <ScrollArea className="w-full h-full max-h-[50vh]">
                            <Table>
                                <TableBody>
                                    {list.map((item, index) => (
                                        <TableRow key={`${item.Id}-${index}`} className="hover:bg-transparent">
                                            <TableCell title={item.Id} className="truncate max-w-xs cursor-default">
                                                {options.find((option) => option.value === item.Id)?.label || item.Id}
                                            </TableCell>
                                            <TableCell className="truncate max-w-xs cursor-default">
                                                <Input placeholder="Enter relation..." value={item.Relation || ""} onChange={(e) => handleRelationDefinition(item.Id, e.target.value)} autoFocus={false} />
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        </ScrollArea>
                    ) : (
                        <span className="w-full text-center">
                            {emptyText ? emptyText : "Nothing added yet."}
                        </span>
                    )}
                    
                    <DialogFooter className="sm:justify-center">
                        <DialogClose asChild>
                            <Button type="button">Close</Button>
                        </DialogClose>
                    </DialogFooter>
                </DialogContent>
            </Dialog>
        </div>
    
        
    );
}