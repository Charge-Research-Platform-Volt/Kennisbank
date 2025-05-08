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
import { getFileHasher } from "@/utils/fileHashWorker"
import { toast } from "sonner"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle } from "lucide-react"
import { ApiResponseSchema } from "@/types/apiResponse.type"
import { UploadNewResource, UploadWithDto } from "@/actions/uploadActions"
import { useRouter } from "next/navigation"
import { RequiredAstrix } from "@/components/ui/required-astrix"
import { AddRelationsDialog } from "@/components/ui/add-relations-dialog"
import { RegionCreateDto, RelatedEntry, RelatedEntrySchema, ResourceTypeCreateDto } from "@/types/uploadTypes"
import CreateDialog from "./CreateDialog"
import { TagCreateDto } from "@/types/tag.type"
import { Drawer, DrawerClose, DrawerContent, DrawerDescription, DrawerFooter, DrawerHeader, DrawerTitle, DrawerTrigger } from "@/components/ui/drawer"
import NewPerson from "@/components/new/NewPerson"
import NewOrganisation from "./NewOrganisation"
import { useDrawerRerender } from "@/utils/useDrawerRerenderer"

// Define upload types
const UploadTypeEnum = z.enum(["document", "website", "audio", "video"])

// Define constants
const urlDefault = "http://no.url/"
const fileDefault = new File([""], "placeholder.txt", { type: "text/plain" });

export const resourceCreateFormSchema = z.object(
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
    organisations: z.array(RelatedEntrySchema),
    regions: z.string().uuid().array(),
    uploadType: UploadTypeEnum,
    url: z.string().min(1, "URL is required").url("Invalid URL"),
    file: z.any().refine(val => val !== undefined , { message: "File is required" }),
    hash: z.string().optional(),
    accessedOn: z.string().date("Invalid Date").optional(),
    abstract: z.string().optional(),
    length: z.number().optional(),
    relatedPersons: z.array(RelatedEntrySchema),
    relatedOrganisations: z.array(RelatedEntrySchema),
});

interface NewResourceProps 
{
    personOptions: SelectOption[];
    organisationOptions: SelectOption[];
    resourceTypeOptions: SelectOption[];
    tagOptions: SelectOption[];
    regionOptions: SelectOption[];
}

export default function NewResource({ personOptions, organisationOptions, resourceTypeOptions, tagOptions, regionOptions }: NewResourceProps) 
{
    // React states
    const [uploadType, setUploadType] = React.useState<z.infer<typeof UploadTypeEnum>>("document");
    const [isChecking, setIsChecking] = React.useState<boolean>(false);
    const [duplicateId, setDuplicateId] = React.useState<string>("");
    
    const [persons, setPersons] = React.useState<SelectOption[]>(personOptions);
    const [organisations, setOrganisations] = React.useState<SelectOption[]>(organisationOptions);
    const [resourceTypes, setResourceTypes] = React.useState<SelectOption[]>(resourceTypeOptions);
    const [tags, setTags] = React.useState<SelectOption[]>(tagOptions);
    const [regions, setRegions] = React.useState<SelectOption[]>(regionOptions);
    
    const [resourceType, setResourceType] = React.useState<string>("");
    
    const [createTagOpen, setCreateTagOpen] = React.useState<boolean>(false);
    const [createRegionOpen, setCreateRegionOpen] = React.useState<boolean>(false);
    const [createResourceTypeOpen, setCreateResourceTypeOpen] = React.useState<boolean>(false);
    const [createPersonOpen, setCreatePersonOpen] = React.useState<boolean>(false);
    const [createOrganisationOpen, setCreateOrganisationOpen] = React.useState<boolean>(false);
    
    const [isRelated, setIsRelated] = React.useState<boolean>(false);
    
    const personDrawerRef = React.useRef<HTMLDivElement>(null);
    const organisationDrawerRef = React.useRef<HTMLDivElement>(null);
    
    const router = useRouter();
    
    useDrawerRerender([createPersonOpen, createOrganisationOpen]);
    
    // Define the form
    const form = useForm<z.infer<typeof resourceCreateFormSchema>>({
        resolver: zodResolver(resourceCreateFormSchema),
        defaultValues: {
            title: "",
            description: "",
            typeId: "",
            languageCode: "",
            publicationDate: "",
            publicationCode: "",
            license: "",
            note: "",
            tags: [],
            authors: [],
            organisations: [],
            regions: [],
            sources: [],
            uploadType: "document",
            url: urlDefault,
            hash: "",
            file: fileDefault,
            relatedOrganisations: [],
            relatedPersons: [],
        },
    });
    
    // Effect for uploadType
    React.useEffect(() => 
    {
        // Update the upload type in the form
        form.setValue("uploadType", uploadType);
    
        // When website, set url to nothing, else to a valid url
        form.setValue("url", uploadType === "website" ? "" : urlDefault);
        
        // Clear file on switch
        form.setValue("file", uploadType === "website" ? fileDefault : undefined);
        form.clearErrors("file");
        
        // Clear the duplicate ID, since we clear the fields
        setDuplicateId("");
        setIsChecking(false);
    }, [form, uploadType]);
    
    // Keep track of the type ID
    const typeId = form.watch("typeId");
    
    // Effect for resourceType
    React.useEffect(() => 
    {
       // Find the selected resource type from resourceTypes array
        const selectedType = resourceTypes.find(type => type.value === typeId);
        
        setResourceType(selectedType?.label || ""); 
    }, [typeId, resourceTypes]);
    
    // Handle file changing
    async function onFileChange(file: File | undefined) 
    {
        // Set the value in the form
        form.setValue("file", file);
        
        // Reset values
        setDuplicateId("");
        setIsChecking(false);
    
        // Do nothing else if there is no file
        if (!file) return;
        
        setIsChecking(true);
        
        const fileHasher = getFileHasher();
        const result = await fileHasher.checkDuplicate(file);
        
        setDuplicateId(result.isDuplicate ? result.id : "");
        form.setValue("hash", result.hash);
        
        // Display a toast
        if (result.isDuplicate)
            toast.warning("This file already exists!");
        
        setIsChecking(false);
    }
    
    // Handle website url changing
    async function onWebsiteUrlChange(url: string) 
    {
        // Set the value in the form
        form.setValue("url", url);
        
        // Do nothing if empty
        if (!url) return;
        
        setIsChecking(true);
        
        const urlSafeUrl = encodeURIComponent(url);
        const response = await fetch("http://localhost:8080/resources/exists?url=" + urlSafeUrl, { credentials: "include" });
        
        if (response.ok) 
        {
            const rawData = await response.json();
            
            try 
            {
                const existsResponse = ApiResponseSchema.parse(rawData);
                
                if (existsResponse.body.exists)
                    setDuplicateId(existsResponse.body.id);
                else
                    setDuplicateId("");
            }
            catch (error: unknown)
            {
                throw new Error(`Invalid response format: ${String(error)}`);
            }
        }
        else 
        {
            throw new Error(`Server error: ${response.status}`);
        }
        
        setIsChecking(false);
    }
    
    // Function to be called when form is submitted
    async function onSubmit(values: z.infer<typeof resourceCreateFormSchema>) 
    {
        // Disable the submit button
        setIsChecking(true);
    
        const id: string = await UploadNewResource(values);
        toast.info(`Resource uploaded succesfully with ID '${id}'`);
        
        router.push('/');
    }
    
    // Handle tag creation
    async function onTagCreation(name: string) 
    {
        const id: string = await UploadWithDto("/api/tags/add-user-tag", { name: name } as TagCreateDto);
    
        const newTags = [...tags, { value: id, label: name } as SelectOption]
        setTags(newTags);
        const newSelected = [...form.getValues("tags"), id]
        form.setValue("tags", newSelected);
        
        setCreateTagOpen(false);
    }
    
    // Handle resource type creation
    async function onResourceTypeCreation(name: string) 
    {
        const id: string = await UploadWithDto("/api/resources/types/new", { Name: name } as ResourceTypeCreateDto);
    
        const newResourceTypes = [...resourceTypes, { value: id, label: name } as SelectOption]
        setResourceTypes(newResourceTypes);
        form.setValue("typeId", id);
        
        setCreateResourceTypeOpen(false);
    }
    
    // Handle region creation
    async function onRegionCreation(name: string) 
    {
        const id: string = await UploadWithDto("/api/regions/new", { Name: name } as RegionCreateDto);
        
        const newRegions = [...regions, { value: id, label: name } as SelectOption];
        setRegions(newRegions);
        const newSelected = [...form.getValues("regions"), id];
        form.setValue("regions", newSelected);
        
        setCreateRegionOpen(false);
    }
    
    // Handle person creation
    async function onPersonCreation(id: string, name: string) 
    {
        const newPersons = [...persons, { value: id, label: name } as SelectOption];
        setPersons(newPersons);
        
        if (isRelated) 
        {
            const newSelected = [...form.getValues("relatedPersons"), { Id: id, Relation: ""} as RelatedEntry]
            form.setValue("relatedPersons", newSelected);
        }
        else 
        {
            const newSelected = [...form.getValues("authors"), id];
            form.setValue("authors", newSelected);
        }
        
        setCreatePersonOpen(false);
    }
    
    // Handle organisation creation
    async function onOrganisationCreation(id: string, name: string) 
    {
        const newOrganisations = [...organisations, { value: id, label: name } as SelectOption];
        setOrganisations(newOrganisations);
        
        const newSelected = [...form.getValues(isRelated ? "relatedOrganisations" : "organisations"), { Id: id, Relation: "" } as RelatedEntry]
        form.setValue(isRelated ? "relatedOrganisations" : "organisations", newSelected);
        
        setCreateOrganisationOpen(false);
    }

    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl mt-5">
            {/* Create dialogs */}
            <CreateDialog open={createTagOpen} title="Create New Tag" placeholder="Tag name..." onOpenChange={setCreateTagOpen} onCreate={onTagCreation} />
            <CreateDialog open={createResourceTypeOpen} title="Create New Resource Type" placeholder="Resource type name..." onOpenChange={setCreateResourceTypeOpen} onCreate={onResourceTypeCreation} />
            <CreateDialog open={createRegionOpen} title="Create New Region" placeholder="Region name..." onOpenChange={setCreateRegionOpen} onCreate={onRegionCreation} />
            
            {/* Create drawers */}
            <Drawer open={createPersonOpen} onOpenChange={setCreatePersonOpen}>
                <DrawerContent className="flex flex-col max-h-[90vh]" ref={personDrawerRef} forceMount>
                    <DrawerHeader hidden={true}>
                        <DrawerTitle>Create a new person</DrawerTitle>
                    </DrawerHeader>
                    <div className="flex-1 overflow-y-auto p-4">
                        <NewPerson personOptions={persons} organisationOptions={organisations} onCreate={onPersonCreation} container={personDrawerRef.current} />
                    </div>
                </DrawerContent>
            </Drawer>
            
            <Drawer open={createOrganisationOpen} onOpenChange={setCreateOrganisationOpen}>
                <DrawerContent className="flex flex-col max-h-[90vh]" ref={organisationDrawerRef}>
                    <DrawerHeader hidden={true}>
                        <DrawerTitle>Create a new organisation</DrawerTitle>
                    </DrawerHeader>
                    <div className="flex-1 overflow-y-auto p-4">
                        <NewOrganisation organisationOptions={organisations} onCreate={onOrganisationCreation} container={organisationDrawerRef.current} />
                    </div>
                </DrawerContent>
            </Drawer>
            
            <h1 className="text-2xl tracking-tight text-gray-900 dark:text-gray-100 md:text-3xl lg:text-4xl mb-2">
                Create a new resource
            </h1>
            
            <hr className="mb-4" />
            
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
                                            <Input placeholder="Paste URL to website..." { ... field } onBlur={(e) => onWebsiteUrlChange(e.target.value)} />
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
                                            <FileInput placeholder="Select file..." onChange={(e) => onFileChange(e.target.files?.[0] || undefined)} onBlur={field.onBlur} name={field.name} ref={field.ref} />
                                        </FormControl>
                                        <FormMessage />
                                    </FormItem>
                                )} />
                            }
                        </div>
                    </div>
                    
                    {/* Duplicate file alert */}
                    <Alert variant="destructive" className="border-destructive" hidden={duplicateId === ""}>
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>Duplicate Resource</AlertTitle>
                        <AlertDescription>
                            This resource already exists! It has ID: {duplicateId}
                        </AlertDescription>
                    </Alert>
                    
                    {/* Title input */}
                    <FormField control={form.control} name="title" render={({field}) => (
                        <FormItem>
                            <FormLabel>Title <RequiredAstrix /></FormLabel>
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
                                <FormLabel>Publication Date <RequiredAstrix /></FormLabel>
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
                                <FormLabel>Language Code <RequiredAstrix /></FormLabel>
                                <FormControl>
                                    <Selection placeholder="Select Language..." options={LanguageCodes} { ... field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                        
                        {/* Type */}
                        <FormField control={form.control} name="typeId" render={({field}) => (
                            <FormItem>
                                <FormLabel>Type <RequiredAstrix /></FormLabel>
                                <FormControl>
                                    <Selection placeholder="Select Type..." options={resourceTypes} hasCreateButton={true} onCreateButton={() => setCreateResourceTypeOpen(true)} { ... field} />
                                </FormControl>
                                <FormMessage />
                            </FormItem>
                        )} />
                    </div>
                    
                    {/* Abstract */}
                    {
                        resourceType === "Scientific Article" && 
                        (
                            <FormField control={form.control} name={"abstract"} render={({field}) => (
                                <FormItem>
                                    <FormLabel>Abstract <RequiredAstrix /></FormLabel>
                                    <FormControl>
                                        <Textarea placeholder="The abstract of the scientific article" rows={5} { ... field} />
                                    </FormControl>
                                </FormItem>
                            )} />
                        )
                    }
                    
                    {/* Authors */}
                    <FormField control={form.control} name="authors" render={({field}) => (
                        <FormItem>
                            <FormLabel>Authors <RequiredAstrix /></FormLabel>
                            <FormControl>
                                <Selection placeholder="Select authors..." options={persons} multiSelect={true} hasCreateButton={true} onCreateButton={() => { setIsRelated(false); setCreatePersonOpen(true); }} { ... field} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Organisation of Origin */}
                    <FormField control={form.control} name="organisations" render={({field}) => (
                        <FormItem>
                            <FormLabel>Organisations of Origin <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <AddRelationsDialog title="Add Roles" placeholder="Select organisations..." inputPlaceholder="Enter role..." buttonText="Define Roles" toastText="Roles saved" options={organisations} emptyText="No organisations selected yet." hasCreateButton={true} onCreateButton={() => { setIsRelated(false); setCreateOrganisationOpen(true); }} { ... field} />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Sources */}
                    <FormField control={form.control} name="sources" render={({field}) => (
                        <FormItem>
                            <FormLabel>Sources <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <AddListDialog title="Add sources to resource" placeholder="Add sources..." inputPlaceholder="Paste URL here..." emptyText="No sources have been added yet." validateInput={(item) => URL.canParse(item) } parseForList={(item) => new URL(item).hostname} { ... field } />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Tags */}
                    <FormField control={form.control} name="tags" render={({field}) => (
                        <FormItem>
                            <FormLabel>Tags <RequiredAstrix /></FormLabel>
                            <FormControl>
                                <Selection placeholder="Select tags..." options={tags} multiSelect={true} hasCreateButton={true} onCreateButton={() => setCreateTagOpen(true)} { ... field } />
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
                                <Selection placeholder="Select regions..." options={regions} multiSelect={true} hasCreateButton={true} onCreateButton={() => setCreateRegionOpen(true)} { ... field } />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Related Persons */}
                    <FormField control={form.control} name="relatedPersons" render={({field}) => (
                        <FormItem>
                            <FormLabel>Related Persons</FormLabel>
                            <FormControl>
                                <AddRelationsDialog title="Add Roles" placeholder="Add related persons..." inputPlaceholder="Enter role..." buttonText="Define Roles" toastText="Roles saved" options={persons} emptyText="No persons selected yet." hasCreateButton={true} onCreateButton={() => { setIsRelated(true); setCreatePersonOpen(true); }} { ... field } />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Related Organisations */}
                    <FormField control={form.control} name="relatedOrganisations" render={({field}) => (
                        <FormItem>
                            <FormLabel>Related Organisations</FormLabel>
                            <FormControl>
                                <AddRelationsDialog title="Add Roles" placeholder="Add related organisations..." inputPlaceholder="Enter role..." buttonText="Define Roles" toastText="Roles saved" options={organisations} emptyText="No organisations selected yet." hasCreateButton={true} onCreateButton={() => { setIsRelated(true); setCreateOrganisationOpen(true); }} { ... field } />
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
                    <Button type="submit" className="w-full" disabled={isChecking || duplicateId !== ""}>Submit</Button>
                </form>
            </Form>
        </div>
    );
}
