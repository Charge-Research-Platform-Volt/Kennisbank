"use client"

import * as React from "react"
import { Check, ChevronsUpDown } from "lucide-react"
import { cn } from "@/lib/utils"
import { Button } from "@/components/ui/button"
import { Command, CommandEmpty, CommandGroup, CommandInput, CommandItem, CommandList } from "@/components/ui/command"
import { Popover, PopoverContent, PopoverTrigger } from "@/components/ui/popover"
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Control, FieldPath, UseFormSetValue } from "react-hook-form"

interface AuthorSelectionFormFieldProps<T extends object> 
{
    control: Control<T>;
    name: FieldPath<T>;
    setValue?: UseFormSetValue<T>;
    label?: string;
    placeholder?: string;
}

