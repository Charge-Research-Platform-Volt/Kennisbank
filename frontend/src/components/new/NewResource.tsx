"use client"

import * as React from "react"
import { z } from "zod"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { Selection } from "@/components/ui/selection"
import { LanguageCodes } from "@/lists/languageCodes"
import { SelectOption } from "@/components/ui/selection"
import { AddListDialog } from "@/components/ui/add-list-dialog"
import { Select, SelectTrigger, SelectContent, SelectItem, SelectValue } from "@/components/ui/select"
import { FileInput } from "@/components/ui/file-input"

// Define upload types
const UploadTypeEnum = z.enum(["document", "website", "audio", "video"])

// Define constants
const urlDefault = "http://no.url/"

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
    tags: z.string().uuid().array().min(1, { message: "Please add a tag" }),
    authors: z.string().uuid().array().min(1, { message: "Please add an author" }),
    organisations: z.string().uuid().array().optional(),
    regions: z.string().uuid().array().optional(),
    uploadType: UploadTypeEnum,
    url: z.string().min(1, "URL is required").url("Invalid URL"),
    file: z.any().refine(val => val !== undefined, { message: "File is required" }),
});

interface NewResourceProps 
{
    persons: SelectOption[];
    organisations: SelectOption[];
    resourceTypes: SelectOption[];
    tags: SelectOption[];
    regions: SelectOption[];
}

export default function NewResource({ persons, organisations, resourceTypes, tags, regions }: NewResourceProps) 
{
    // React states
    const [uploadType, setUploadType] = React.useState<z.infer<typeof UploadTypeEnum>>("document");
    
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
            sources: [],
            uploadType: "document",
            url: urlDefault,
        },
    });
    
    // Effect for uploadType
    React.useEffect(() => 
    {
        // When website, set url to nothing, else to a valid url
        form.setValue("url", uploadType === "website" ? "" : urlDefault);
        
        form.setValue("file", undefined);
        form.clearErrors("file");
    }, [uploadType]);
    
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
                    
                    <div className="flex flex-col sm:flex-row gap-2">
                        <Select onValueChange={(value) => setUploadType(value as z.infer<typeof UploadTypeEnum>)} value={uploadType}>
                            <SelectTrigger className="sm:w-35">
                                <SelectValue placeholder="Upload type" />
                            </SelectTrigger>
                            <SelectContent>
                                <SelectItem value="document">Document</SelectItem>
                                <SelectItem value="website">Website</SelectItem>
                                <SelectItem value="audio">Audio</SelectItem>
                                <SelectItem value="video">Video</SelectItem>
                            </SelectContent>
                        </Select>
                        <div className="flex-1">
                            {
                                uploadType === "website" &&
                                <FormField control={form.control} name="url" render={({field}) => (
                                    <FormItem>
                                        <FormControl>
                                            <Input placeholder="Paste URL to website..." { ... field } />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )} />
                                
                            }
                            
                            {
                                uploadType !== "website" &&   
                                <FormField control={form.control} name="file" render={({field}) => (
                                    <FormItem>
                                        <FormControl>
                                            <FileInput placeholder="Select file..." onChange={(e) => field.onChange(e.target.files?.[0] || undefined)} onBlur={field.onBlur} name={field.name} ref={field.ref} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )} />
                            }
                        </div>
                    </div>
                    
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
                    
                    {/* Organisation of Origin */}
                    <FormField control={form.control} name="organisations" render={({field}) => (
                        <FormItem>
                            <FormLabel>Organisations of Origin <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Selection placeholder="Select organisations..." options={organisations} multiSelect={true} { ... field } />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Sources */}
                    <FormField control={form.control} name="sources" render={({field}) => (
                        <FormItem>
                            <FormLabel>Sources</FormLabel>
                            <FormControl>
                                <AddListDialog title="Add sources to resource" placeholder="Add sources..." inputPlaceholder="Paste URL here..." emptyText="No sources have been added yet." validateInput={(item) => URL.canParse(item) } parseForList={(item) => new URL(item).hostname} { ... field } />
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
                    
                    {/* Licence */}
                    <FormField control={form.control} name="license" render={({field}) => (
                        <FormItem>
                            <FormLabel>Licence <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Input placeholder="The license of the resource" { ... field} />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Geographic regions */}
                    <FormField control={form.control} name="regions" render={({field}) => (
                        <FormItem>
                            <FormLabel>Geographic Regions <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Selection placeholder="Select regions..." options={regions} multiSelect={true} { ... field } />
                            </FormControl>
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
