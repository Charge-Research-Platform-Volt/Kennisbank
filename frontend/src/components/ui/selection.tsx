"use client"

import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { useFormField } from "@/components/ui/form"

export interface SelectOption 
{
    value: string;
    label: string;
}

interface SelectionProps extends Omit<React.ComponentPropsWithoutRef<typeof Button>, 'onChange'>
{
    placeholder: string;
    options: SelectOption[];
    onChange?: (value: string | string[]) => void;
    multiSelect?: boolean;
    hasCreateButton?: boolean;
    onCreateButton?: () => void;
}

export function Selection({
    placeholder, 
    options, 
    onChange,
    multiSelect = false,
    hasCreateButton = false,
    onCreateButton,
    className
}: SelectionProps) 
{
    const { error } = useFormField();

    const [open, setOpen] = React.useState(false);
    const [value, setValue] = React.useState<string | string[]>(multiSelect ? [] : "");
    
    function handleOnSelect(currentLabel: string) 
    {
        // Find the option based on the label to get its value
        const selectedOption = options.find((option) => option.label.toLowerCase() === currentLabel.toLowerCase());
        
        // Option not found (should not happen, but better safe then sorry)
        if (!selectedOption) return;
        
        if (multiSelect) 
        {
            // For multiselect, we handle an array of values
            const valueArray = Array.isArray(value) ? value : [];
            const newValue = valueArray.includes(selectedOption.value) ? valueArray.filter(v => v !== selectedOption.value) : [...valueArray, selectedOption.value];
            
            // Set the value
            setValue(newValue);
            if (onChange) onChange(newValue);
            
            // Don't close popover for multiselect
        }
        else 
        {
            // Extract the new value
            const newValue = value === selectedOption.value ? "" : selectedOption.value;
            
            // Set the new value and call the onChange if it exists
            setValue(newValue);
            if (onChange) onChange(newValue);
            
            // Close the selection
            setOpen(false);
        }
    }
    
    // Function to get the display content
    const getDisplayText = () => 
    {
        // No value, return placeholder
        if (!value || (Array.isArray(value) && value.length == 0)) 
            return "";
        
        // Multiselect content
        if (multiSelect && Array.isArray(value)) 
        {
            // Array is empty, so show placeholder
            if (value.length === 0)
                return "";
                
            // Get all the selected labels and join them with semicolons (filters undefined values)
            const selectedLabels = value.map(v => options.find(o => o.value === v)?.label).filter(Boolean).join("; ");
            
            return selectedLabels;
        }
        
        // Single select content
        else 
        {
            const option = options.find(o => o.value === value);
            return option ? option.label : placeholder;
        }
    }
    
     // Handle click on the custom input
    const handleInputClick = () => {
        setOpen(!open);
    };
    
    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <div 
                    className={cn(
                        "flex h-10 w-full rounded-md border bg-background px-3 py-2 text-sm",
                        error ? "border-destructive" : "border-input",
                        "cursor-pointer relative caret-transparent",
                        className
                    )}
                    role="combobox"
                    aria-expanded={open}
                    onClick={handleInputClick}
                    tabIndex={-1}
                >
                    <input 
                        readOnly={true}
                        placeholder={placeholder}
                        value={getDisplayText()}
                        className="w-full bg-transparent border-0 outline-none placeholder:text-muted-foreground cursor-pointer truncate caret-transparant"
                        onClick={(e) => e.preventDefault()}
                        tabIndex={-1}
                    />
                    <ChevronsUpDown className="h-4 w-4 shrink-0 opacity-50 ml-2 self-center" />
                </div>
            </PopoverTrigger>
            <PopoverContent className="w-full p-0">
                <Command>
                    <CommandInput placeholder="Search..." />
                    <CommandList>
                        <CommandEmpty>Nothing found.</CommandEmpty>
                        <CommandGroup>
                            {
                                options.map((option) => {
                                    const isSelected = multiSelect && Array.isArray(value) ? value.includes(option.value) : value === option.value;
                                    
                                    return (
                                        <CommandItem key={option.label} value={option.label} onSelect={handleOnSelect}>
                                            <div className="flex items-center justify-between w-full">
                                                {option.label}
                                                <Check className={cn("ml-2 h-4 w-4", isSelected ? "opacity-100" : "opacity-0")} />
                                            </div>
                                        </CommandItem> 
                                    );
                                })
                            }
                            {
                                hasCreateButton && (
                                    <CommandItem>
                                        <hr />
                                        <Button type="button" variant="ghost">Create new</Button>
                                    </CommandItem>
                                )
                            }
                        </CommandGroup>
                    </CommandList>
                </Command>
            </PopoverContent>
        </Popover>
    );
}