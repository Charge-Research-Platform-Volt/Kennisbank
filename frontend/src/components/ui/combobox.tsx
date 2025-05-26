"use client"

import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList, } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { SelectOption } from "@/components/ui/selection"

export interface ComboboxProps 
{
    id?: string;
    className?: string;
    options: SelectOption[];
    value?: string[];
    hasSearch?: boolean;
    multiSelect?: boolean;
    enabledByDefault?: boolean;
    onValueChange?: (values: string[]) => void;
}

export function Combobox({options, hasSearch = false, multiSelect = false, enabledByDefault = false, onValueChange, className, id, value}: ComboboxProps) {
    const [open, setOpen] = React.useState(false);
    const [selectedValues, setSelectedValues] = React.useState<string[]>(value ? value : enabledByDefault ? options.map((option) => option.value) : []);
    
    React.useEffect(() => 
    {
        if (value !== undefined)
            setSelectedValues(value);
    }, [value]);
    
    React.useEffect(() => 
    {
        if (onValueChange) onValueChange(selectedValues);
    }, [selectedValues]);

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                    <Button id={id} variant="outline" role="combobox" aria-expanded={open} className={"justify-between " + className}>
                        { selectedValues.length == options.length && "All"}
                        { selectedValues.length > 0 && selectedValues.length < options.length && "Some"}
                        { selectedValues.length == 0 && "Select something..."}
                    <ChevronsUpDown className="opacity-50" />
                </Button>
            </PopoverTrigger>
        <PopoverContent className="w-[200px] p-0">
            <Command>
                { hasSearch && <CommandInput placeholder="Search..." className="h-9" /> }
                <CommandList>
                    <CommandEmpty>No framework found.</CommandEmpty>
                    <CommandGroup>
                    { options.map((option) => (
                        <CommandItem
                        key={option.value}
                        value={option.value}
                        onSelect={(currentValue) => {
                            if (multiSelect) 
                            {
                                if (selectedValues.includes(currentValue))
                                    setSelectedValues(selectedValues.filter((value) => value !== currentValue))
                                else
                                    setSelectedValues([... selectedValues, currentValue])
                            }else 
                            {
                                setSelectedValues(currentValue === selectedValues[0] ? [] : [currentValue])
                                setOpen(false)
                            }
                        }}
                        >
                        {option.label}
                        <Check
                            className={cn(
                                "ml-auto",
                                selectedValues.includes(option.value) ? "opacity-100" : "opacity-0"
                            )}
                        />
                        </CommandItem>
                    ))}
                    </CommandGroup>
                </CommandList>
            </Command>
        </PopoverContent>
        </Popover>
    )
}
