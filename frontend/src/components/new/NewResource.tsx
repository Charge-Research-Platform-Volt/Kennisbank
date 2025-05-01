"use client"

import * as React from "react"
import { z } from "zod"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Form, FormControl, FormDescription, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Selection } from "@/components/ui/selection"
import { LanguageCodes } from "@/lists/languageCodes"
import { SelectOption } from "@/components/ui/selection"
import { AddListDialog } from "@/components/ui/add-list-dialog"

const resourceCreateFormSchema = z.object(
{
    title: z.string().min(1, { message: "Title is required" }),
    description: z.string().optional(),
    typeId: z.string().uuid(),
    languageCode: z.string().length(2, { message: "Please select a language" }),
    publicationCode: z.string().optional(),
    publicationDate: z.string().date("Please select a date"),
    license: z.string().optional(),
    sources: z.string().array().optional(),
    note: z.string().optional(),
    tags: z.string().array().min(1, { message: "Please add a tag" }),
    authors: z.string().array().min(1, { message: "Please add an author" }),
});

interface NewResourceProps 
{
    persons: SelectOption[];
    resourceTypes: SelectOption[];
    tags: SelectOption[];
}

export default function NewResource({ persons, resourceTypes, tags }: NewResourceProps) 
{
    // Define the form
    const form = useForm<z.infer<typeof resourceCreateFormSchema>>(
    {
        resolver: zodResolver(resourceCreateFormSchema),
        defaultValues:
        {
            title: "",
            typeId: "",
            languageCode: "",
            publicationDate: "",
            tags: [],
            sources: []
        },
    });
    
    // Function to be called when form is submitted
    function onSubmit(values: z.infer<typeof resourceCreateFormSchema>) 
    {
        console.log(values);
    }

    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl mt-5">
            <h1 className="text-2xl tracking-tight text-gray-900 dark:text-gray-100 md:text-3xl lg:text-4xl mb-2">
                Create a new resource
            </h1>
            
            <hr className="mb-4"></hr>
            
            <Form { ... form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                    {/* Title input */}
                    <FormField control={form.control} name="title" render={({field}) => (
                        <FormItem>
                            <FormLabel>Title</FormLabel>
                            <FormControl>
                                <Input placeholder="The title of the resource" { ... field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Description input */}
                    <FormField control={form.control} name="description" render={({field}) => (
                        <FormItem>
                            <FormLabel>Description <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Textarea placeholder="A short description of the content of this resource" rows={5} { ... field} />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                        {/* Publication date */}
                        <FormField control={form.control} name="publicationDate" render={({field}) => (
                            <FormItem>
                                <FormLabel>Publication Date</FormLabel>
                                <FormControl>
                                    <Input type="date" { ... field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                        
                        {/* Publication code */}
                        <FormField control={form.control} name="publicationCode" render={({field}) => (
                            <FormItem>
                                <FormLabel>Publication Code <code>(Optional)</code></FormLabel>
                                <FormControl>
                                    <Input placeholder="The publication code (DOI / ISBN / ISSN / etc.)" { ... field} />
                                </FormControl>
                            </FormItem>
                        )} />
                        
                        {/* Language Code */}
                        <FormField control={form.control} name="languageCode" render={({field}) => (
                            <FormItem>
                                <FormLabel>Language Code</FormLabel>
                                <FormControl>
                                    <Selection placeholder="Select Language..." options={LanguageCodes} { ... field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                        
                        {/* Type */}
                        <FormField control={form.control} name="typeId" render={({field}) => (
                            <FormItem>
                                <FormLabel>Type</FormLabel>
                                <FormControl>
                                    <Selection placeholder="Select Type..." options={resourceTypes} { ... field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                    </div>
                    
                    {/* Authors */}
                    <FormField control={form.control} name="authors" render={({field}) => (
                        <FormItem>
                            <FormLabel>Authors</FormLabel>
                            <FormControl>
                                <Selection placeholder="Select authors..." options={persons} multiSelect={true} { ... field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Licence */}
                    <FormField control={form.control} name="license" render={({field}) => (
                        <FormItem>
                            <FormLabel>Licence <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Input placeholder="The license of the resource" { ... field} />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Sources */}
                    <FormField control={form.control} name="sources" render={({field}) => (
                        <FormItem>
                            <FormLabel>Sources</FormLabel>
                            <FormControl>
                                <AddListDialog title="Add sources to resource" placeholder="Add sources..." inputPlaceholder="Paste URL here..." emptyText="No sources have been added yet." validateInput={(item) => URL.canParse(item) } parseForList={(item) => new URL(item).hostname} />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Tags */}
                    <FormField control={form.control} name="tags" render={({field}) => (
                        <FormItem>
                            <FormLabel>Tags</FormLabel>
                            <FormControl>
                                <Selection placeholder="Select tags..." options={tags} multiSelect={true} { ... field } />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Notes */}
                    <FormField control={form.control} name="note" render={({field}) => (
                        <FormItem>
                            <FormLabel>Notes <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Textarea placeholder="Additional notes about this resource" rows={10} { ... field } />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Submit button */}
                    <Button type="submit" className="w-full">Submit</Button>
                </form>
            </Form>
        </div>
    );
}
