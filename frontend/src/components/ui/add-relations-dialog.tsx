"use client"

import * as React from "react"
import { Button } from "@/components/ui/button"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger, DialogFooter, DialogClose } from "@/components/ui/dialog"
import { Input } from "@/components/ui/input"
import { Table, TableBody, TableCell, TableRow } from "@/components/ui/table"
import { ScrollArea } from "@/components/ui/scroll-area"
import { SelectOption, Selection } from "./selection"
import { RelatedEntry } from "@/types/uploadTypes"
import { toast } from "sonner"
  
interface AddRelationsDialogProps {
    title: string;
    placeholder: string;
    buttonText?: string;
    options: SelectOption[];
    emptyText?: string;
    toastText?: string;
    inputPlaceholder?: string;
    hasCreateButton?: boolean;
    onCreateButton?: () => void;
    onChange?: (value: RelatedEntry[]) => void;
    value?: RelatedEntry[];
    onBlur?: () => void;
    name?: string;
    disabled?: boolean;
    ref?: React.Ref<unknown>;
    container?: HTMLElement | null;
}

/**
 * @summary A dialog to define relations for the given options
 * @param title The title of the dialog
 * @param placeholder The placeholder for the selection
 * @param inputPlaceholder The placeholder for the input inside the dialog
 * @param buttonText The text to show in the button that is the dialog trigger
 * @param toastText The text to show in the toast to confirm that everything has been saved
 * @param options The complete list of all possible options that can be selected
 * @param emptyText The text to display in the dialog when nothing has been selected yet
 * @param onChange The function to be called when the selection is changed
 * @param hasCreateButton Determines if there is a create button in the selection to create a new option
 * @param onCreateButton Function to be called when the create button is pressed
 * @param value The value of the selection
 * @param container The container in which the selection should be rendered. This has to be defined in dialogs or drawers to properly render popovers 
 */
export function AddRelationsDialog({title, placeholder, inputPlaceholder, buttonText, toastText, options, emptyText, onChange, hasCreateButton, onCreateButton, value, container = null}: AddRelationsDialogProps) 
{
    const [open, setOpen] = React.useState<boolean>(false);
    const [hasOpened, setHasOpened] = React.useState<boolean>(false);
    const [list, setList] = React.useState<RelatedEntry[]>(value ? value : []);
    
    React.useEffect(() => 
    {
        if (value) setList(value);
    }, [value]);
    
    React.useEffect(() => 
    {
        if (open)
            setHasOpened(true);
    
        if (!open && hasOpened) 
            toast.info(toastText ? toastText : "Relations saved");
    }, [open, hasOpened]);
    
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
            <Selection placeholder={placeholder} options={options} onChange={handleSelectionChange} multiSelect={true} hasCreateButton={hasCreateButton} onCreateButton={onCreateButton} value={list.map((item) => item.Id)} container={container} />
            
            <Dialog open={open} onOpenChange={setOpen}>
                <DialogTrigger asChild>
                    <Button className="sm:w-40 h-full" type="button">{buttonText ? buttonText : "Define Relations"}</Button>
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
                                                <Input placeholder={inputPlaceholder ? inputPlaceholder : "Enter relation..."} value={item.Relation || ""} onChange={(e) => handleRelationDefinition(item.Id, e.target.value)} autoFocus={false} />
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


