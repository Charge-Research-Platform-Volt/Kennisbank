"use client"

import * as React from "react"
import { Check, ChevronsUpDown, X } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { Badge } from "@/components/ui/badge"

export interface MultiselectOption
{
    value: string
    label: string
}

interface MultiSelectComboboxProps
{
    options: MultiselectOption[]
    placeholder?: string
    emptyMessage?: string
    onChange?: (values: string[]) => void
    className?: string,
    createNew?: () => void
}

export function MultiSelectCombobox({ options, placeholder = "Select items...", emptyMessage = "No item found.", onChange, className, createNew}: MultiSelectComboboxProps) {
  const [open, setOpen] = React.useState(false)
  const [selected, setSelected] = React.useState<string[]>([])
  const [searchValue, setSearchValue] = React.useState("")
  const [sortedOptions, setSortedOptions] = React.useState<MultiselectOption[]>([...options])
  const sortOnNextOpenRef = React.useRef(true)

  // Sort options when dropdown opens
  React.useEffect(() => {
    if (open && sortOnNextOpenRef.current) {
      // Sort with selected items on top when opening
      const sorted = [...options].sort((a, b) => {
        const aSelected = selected.includes(a.value)
        const bSelected = selected.includes(b.value)
        if (aSelected && !bSelected) return -1
        if (!aSelected && bSelected) return 1
        return 0
      })
      setSortedOptions(sorted)
      sortOnNextOpenRef.current = false
    } else if (!open) {
      // Reset flag when dropdown closes
      sortOnNextOpenRef.current = true
      // Reset search when dropdown closes
      setSearchValue("")
    }
  }, [open, options, selected])

  // Filter options based on search input
  const filteredOptions = React.useMemo(() => {
    if (!searchValue) return sortedOptions
    
    return sortedOptions.filter((option) =>
      option.label.toLowerCase().includes(searchValue.toLowerCase())
    )
  }, [sortedOptions, searchValue])

  // Initialize options
  React.useEffect(() => {
    if (sortedOptions.length === 0) {
      setSortedOptions([...options])
    }
  }, [options, sortedOptions])

  // Notify parent component of changes
  React.useEffect(() => {
    if (onChange) {
      onChange(selected)
    }
  }, [selected, onChange])

  // This function is no longer directly used by CommandItem
  // It's now wrapped in an inline function that passes the option value
  const handleSelect = (value: string) => {
    setSelected((current) => {
      if (current.includes(value)) {
        return current.filter((item) => item !== value)
      }
      return [...current, value]
    })
  }

  const handleRemove = (value: string, e: React.MouseEvent) => {
    e.stopPropagation()
    setSelected((current) => current.filter((item) => item !== value))
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <PopoverTrigger asChild>
        <Button
          variant="outline"
          role="combobox"
          aria-expanded={open}
          className={cn("w-full justify-between min-h-10 py-2", className)}
        >
          <div className="flex flex-wrap gap-1 text-left">
            {selected.length === 0 ? (
              <span className="text-muted-foreground">{placeholder}</span>
            ) : (
              <div className="flex flex-wrap gap-1 max-w-full py-0.5">
                {selected.map((value) => (
                  <Badge key={value} variant="secondary" className="mr-1 mb-1 py-0.5 h-6 flex items-center pl-2 pr-1">
                    <span className="inline-flex items-center text-xs leading-none">
                      {options.find((option) => option.value === value)?.label || value}
                      <span
                        role="button"
                        tabIndex={0}
                        className="ml-1 rounded-full outline-none focus:ring-2 focus:ring-ring focus:ring-offset-2 inline-flex items-center justify-center cursor-pointer"
                        onMouseDown={(e) => {
                          e.preventDefault()
                          e.stopPropagation()
                        }}
                        onClick={(e) => handleRemove(value, e)}
                        onKeyDown={(e) => {
                          if (e.key === 'Enter' || e.key === ' ') {
                            e.preventDefault()
                            handleRemove(value, e as unknown as React.MouseEvent)
                          }
                        }}
                      >
                        <X className="h-3 w-3 text-muted-foreground hover:text-foreground" />
                      </span>
                    </span>
                  </Badge>
                ))}
              </div>
            )}
          </div>
          <ChevronsUpDown className="ml-2 h-4 w-4 shrink-0 opacity-50" />
        </Button>
      </PopoverTrigger>
      <PopoverContent className="w-full p-0">
        <Command>
          <CommandInput 
            placeholder="Search..." 
            value={searchValue}
            onValueChange={setSearchValue}
          />
          <CommandList>
            <CommandEmpty>{emptyMessage}</CommandEmpty>
            <CommandGroup>
              {filteredOptions.map((option) => (
                <CommandItem
                  key={option.value}
                  value={option.label} // Use label for search matching
                  onSelect={() => handleSelect(option.value)} // Still pass value to selection handler
                  className="justify-between"
                >
                  {option.label}
                  <Check
                    className={cn(
                      "mr-2 h-4 w-4",
                      selected.includes(option.value) ? "opacity-100" : "opacity-0"
                    )}
                  />
                </CommandItem>
              ))}
            </CommandGroup>
          </CommandList>
          
          {
            createNew ?
            (
                <div className="w-full">
                    <hr></hr>
                    <Button variant="ghost" onClick={createNew} className="w-full">Create New</Button>
                </div>
            ) : ""
          }
        </Command>
      </PopoverContent>
    </Popover>
  )
}