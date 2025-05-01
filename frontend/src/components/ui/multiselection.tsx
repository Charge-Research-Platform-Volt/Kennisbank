"use client"

import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"

export interface SelectOption 
{
    value: string;
    label: string;
}

interface SelectionProps extends Omit<React.ComponentPropsWithoutRef<typeof Button>, 'onChange'>
{
    placeholder: string;
    options: SelectOption[];
    onChange?: (value: string) => void;
    required?: boolean;
    error?: boolean;
    name?: string;
    id?: string;
}

export function MultiSelection({
    placeholder, 
    options, 
    onChange, 
    required, 
    error,
    name,
    id,
    className,
    ...props
}: SelectionProps) 
{
    const [open, setOpen] = React.useState(false);
    const [value, setValue] = React.useState("");
    
    function handleOnSelect(currentLabel: string) 
    {
        // Find the option based on the label to get its value
        const selectedOption = options.find((option) => option.label.toLowerCase() === currentLabel.toLowerCase());
        
        if (selectedOption) 
        {
            // Extract the new value
            const newValue = value === selectedOption.value ? "" : selectedOption.value;
            
            // Set the new value and call the onChange if it exists
            setValue(newValue);
            if (onChange) onChange(newValue);
        }
        
        // Close the selection
        setOpen(false);
    }
    
    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button 
                    variant="outline" 
                    role="combobox" 
                    aria-expanded={open} 
                    className={cn(
                        "w-full justify-between",
                        error && "border-red-500 focus-visible:ring-red-500", 
                        className
                    )}
                    name={name}
                    id={id}
                    aria-required={required}
                    {...props}
                >
                    { value ? options.find((option) => option.value === value)?.label : placeholder }
                    <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
                </Button>
            </PopoverTrigger>
            <PopoverContent className="w-full p-0">
                <Command>
                    <CommandInput placeholder={placeholder} />
                    <CommandList>
                        <CommandEmpty>Nothing found.</CommandEmpty>
                        <CommandGroup>
                            {
                                options.map((option) => (
                                    <CommandItem key={option.value} value={option.label} onSelect={handleOnSelect}>
                                        {option.label}
                                        <Check className={cn("mr-2 h-4 w-4", value === option.value ? "opacity-100" : "opacity-0")} />
                                    </CommandItem>
                                ))
                            }
                        </CommandGroup>
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}