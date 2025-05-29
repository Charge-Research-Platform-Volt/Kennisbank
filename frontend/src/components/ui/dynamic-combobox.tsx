"use client"

import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList, } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { SelectOption } from "@/components/ui/selection"
import { ApiResponse } from "@/types/apiResponse.type"

export interface DynamicComboboxProps 
{
    id?: string;
    className?: string;
    value?: string[];
    multiSelect?: boolean;
    onValueChange?: (values: string[]) => void;
    
    endpoint: string;
    createPayload: (searchQuery: string) => unknown;
    parseResponse: (response: unknown) => SelectOption[];
    usePost?: boolean;
}

export function DynamicCombobox({ multiSelect = false, onValueChange, className, id, value, endpoint, createPayload, parseResponse, usePost = false }: DynamicComboboxProps)
{
    const [open, setOpen] = React.useState(false);
    const [searchInput, setSearchInput] = React.useState<string>('');
    const [searchQuery, setSearchQuery] = React.useState<string>('');
    const [selectedValues, setSelectedValues] = React.useState<string[]>(value ? value : []);
    const [options, setOptions] = React.useState<SelectOption[]>([]);
    
    const clearSearch = React.useCallback(() => 
    {
        setOptions((currentOptions) => currentOptions.filter((option) => selectedValues.includes(option.value)));
        setSearchInput('');
    }, [selectedValues]);
    
    const updateOptions = React.useCallback((fetchedOptions: SelectOption[]) => 
    {
        setOptions((currentOptions) => 
        {
            const selectedOptions: SelectOption[] = currentOptions.filter((option) => selectedValues.includes(option.value));
            const newOptions: SelectOption[] = selectedOptions.concat(fetchedOptions.filter((option) => !selectedValues.includes(option.value)));
            
            return newOptions;
        })
    }, [selectedValues]);
    
    React.useEffect(() => 
    {
        if (open)
            clearSearch();
    }, [open, clearSearch]);
    
    React.useEffect(() => 
    {
        if (value !== undefined)
            setSelectedValues(value);
    }, [value]);
    
    const onValueChangeRef = React.useRef(onValueChange);
    React.useEffect(() => { onValueChangeRef.current = onValueChange });
    
    React.useEffect(() => 
    {
        if (onValueChangeRef.current) onValueChangeRef.current(selectedValues);
    }, [selectedValues]);
    
    React.useEffect(() => 
    {
        const timer = setTimeout(() => 
        {
            setSearchQuery(searchInput);
        }, 500);
        
        return () => clearTimeout(timer);
    }, [searchInput]);
    
    React.useEffect(() => 
    {
        if (searchQuery === '') return;
        
        // Define async function
        const fetchData = async () => 
        {
            try 
            {
                // Create the body using the provided createBody function
                const payload = createPayload(searchQuery);
                
                let response;
                
                if (usePost) 
                {
                    // Fetch the data from the backend using POST
                    response = await fetch(endpoint,
                    {
                        method: 'POST',
                        credentials: 'include',
                        headers:
                        {
                            'Content-Type': 'application/json',
                        },
                        body: JSON.stringify(payload),
                    });
                }
                else 
                {
                    // Fetch the data from the backend using GET
                    response = await fetch(endpoint + `?${payload}`)
                }
                
                if (!response.ok)
                    throw new Error(`HTTP error! Status: ${response.status}`);
                    
                const data: ApiResponse = await response.json();
                
                if (data.success) 
                    // Parse the fetched data to SelectOptions using the provided function
                    updateOptions(parseResponse(data.body));
                else 
                    throw new Error(`Backend error! Message: ${data.message}`);
            }
            catch (error) 
            {
                console.error('Error fetching data: ', error);
            }
        };
     
        // Run the function
        fetchData();   
    }, [searchQuery, createPayload, usePost, endpoint, updateOptions, parseResponse]);

    return (
        <Popover open={open} onOpenChange={setOpen}>
            <PopoverTrigger asChild>
                <Button id={id} variant="outline" role="combobox" aria-expanded={open} className={`flex justify-between items-center w-full ${className}`}>
                        <div className="flex-1 truncate text-left mr-2">
                            { selectedValues.length > 0 && options.filter((option) => selectedValues.includes(option.value)).map((option) => option.label).join(", ")}
                            { selectedValues.length == 0 && "Select something..."}
                        </div>
                    <ChevronsUpDown className="opacity-50 flex-shrink-0" />
                </Button>
            </PopoverTrigger>
        <PopoverContent className="w-[var(--radix-popover-trigger-width)] p-0">
            <Command shouldFilter={false}>
                <CommandInput placeholder="Search..." className="h-9" value={searchInput} onValueChange={setSearchInput} />
                <CommandList>
                    <CommandEmpty>
                        {searchQuery.length == 0 && "Start searching."}
                        {searchQuery.length > 0 && "Nothing found."}
                    </CommandEmpty>
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
