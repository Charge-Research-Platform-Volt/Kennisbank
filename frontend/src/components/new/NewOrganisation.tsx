"use client"

import * as React from "react"
import { z } from "zod"
import { zodResolver } from "@hookform/resolvers/zod"
import { useForm } from "react-hook-form"
import { Input } from "@/components/ui/input"
import { Button } from "@/components/ui/button"
import { Textarea } from "@/components/ui/textarea"
import { Form, FormControl, FormField, FormItem, FormLabel, FormMessage } from "@/components/ui/form"
import { SelectOption } from "@/components/ui/selection"
import { toast } from "sonner"
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert"
import { AlertCircle } from "lucide-react"
import { ApiResponseSchema } from "@/types/apiResponse.type"
import { UploadWithDto } from "@/actions/uploadActions"
import { useRouter } from "next/navigation"
import { RequiredAstrix } from "@/components/ui/required-astrix"
import { AddRelationsDialog } from "@/components/ui/add-relations-dialog"
import { OrganisationCreateDto, OrganisationCreateDtoSchema, RelatedEntry } from "@/types/uploadTypes"
import { Drawer, DrawerContent, DrawerHeader, DrawerTitle } from "@/components/ui/drawer"
import { useDrawerRerender } from "@/utils/useDrawerRerenderer"
import { useFormHasValues } from "@/hooks/useFormNonDefaultValues"
import { useTabsContext } from "@/context/tabs-context"

interface NewOrganisationProps 
{
    organisationOptions: SelectOption[];
    onCreate?: (id: string, name: string) => void;
    container?: HTMLElement | null;
    updateOrganisations?: (organisations: SelectOption[]) => void;
    useUnsavedDialog?: boolean;
    returnUrl?: string;
}

/**
 * @summary Form component that allows for uploading organisations to the database
 * @param organisationOptions The complete list of all organisations in the database
 * @param onCreate This function is called after a successfull upload
 * @param updateOrganisations This function is called when the collection of organisations changes
 * @param useUnsavedDialog Determines if there should be an UnsavedDialog whenever any field is filled and the user tries to navigate
 * @param returnUrl The URL to where the user will be sent after upload
 */
export default function NewOrganisation({ organisationOptions, onCreate, container = null, updateOrganisations, useUnsavedDialog = true, returnUrl = '/' }: NewOrganisationProps) 
{
    // React states
    const [isChecking, setIsChecking] = React.useState<boolean>(false);
    const [duplicateId, setDuplicateId] = React.useState<string>("");
    
    const [organisations, setOrganisations] = React.useState<SelectOption[]>(organisationOptions);
    
    const [createOrganisationOpen, setCreateOrganisationOpen] = React.useState<boolean>(false);
    
    const organisationDrawerRef = React.useRef<HTMLDivElement>(null);
    
    const router = useRouter();
    
    useDrawerRerender([createOrganisationOpen]);
    
    // Effect to do upstream synchronization of organisations
    React.useEffect(() => { if (updateOrganisations) updateOrganisations(organisations); }, [organisations])
    
    // Define the form
    const form = useForm<z.infer<typeof OrganisationCreateDtoSchema>>(
    {
        resolver: zodResolver(OrganisationCreateDtoSchema),
        defaultValues: 
        {
            Name: "",
            Description: "",
            Website: "",
            EmailAddress: "",
            OrganisationRelations: [],
        },
    });
    
    if (useUnsavedDialog) 
    {
        // Use formHasValues hook to detect if any fields are filled in the form
        // Use the tabsContext hook to display a dialog when some fields are filled
        // And the user tries to navigate
        const formHasValues = useFormHasValues(form);
        const { setFormChanged } = useTabsContext();

        // Use an effect to call the tabsContext function
        React.useEffect(() => {
            setFormChanged(formHasValues);
        }, [formHasValues]);
    }
    
    // Handle name changing
    async function onNameChange(name: string) 
    {
        // Set the value in the form
        form.setValue("Name", name);
        
        // Do nothing if empty
        if (!name) return;
        
        setIsChecking(true);
        
        const urlSafeName = encodeURIComponent(name);
        const response = await fetch("http://localhost:8080/organisations/exists?name=" + urlSafeName, { credentials: "include" });
        
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
    
    // Function to be called when the form is submitted
    async function onSubmit(dto: OrganisationCreateDto) 
    {
        // Disable the submit button
        setIsChecking(true);
    
        const id = await UploadWithDto("/api/organisations/new", dto)
        toast.info(`Organisation created successfully with ID '${id}'`);
        
        if (onCreate)
            onCreate(id, dto.Name);
        else
            router.push(returnUrl);
    }
    
    // Handle organisation creation
    async function onOrganisationCreation(id: string, name: string) 
    {
        const newOrganisations = [...organisations, { value: id, label: name } as SelectOption];
        setOrganisations(newOrganisations);
        
        const newSelected = [...form.getValues("OrganisationRelations"), { Id: id, Relation: "" } as RelatedEntry];
        form.setValue("OrganisationRelations", newSelected);
        
        form.trigger("OrganisationRelations");
        
        setCreateOrganisationOpen(false);
    }

    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl mt-5">
            {/* Create drawers */}
            <Drawer open={createOrganisationOpen} onOpenChange={setCreateOrganisationOpen}>
                <DrawerContent className="flex flex-col max-h-[90vh]" ref={organisationDrawerRef} forceMount>
                    <DrawerHeader hidden={true}>
                        <DrawerTitle>Create a new organisation</DrawerTitle>
                    </DrawerHeader>
                    <div className="flex-1 overflow-y-auto p-4">
                        <NewOrganisation organisationOptions={organisations} onCreate={onOrganisationCreation} container={organisationDrawerRef.current} updateOrganisations={setOrganisations} useUnsavedDialog={false} />
                    </div>
                </DrawerContent>
            </Drawer>
            
            <h1 className="text-2xl tracking-tight text-gray-900 dark:text-gray-100 md:text-3xl lg:text-4xl mb-2">
                Create a new organisation
            </h1>
            
            <hr className="mb-4" />
            
            <Form { ... form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                    {/* Name */}
                    <FormField control={form.control} name="Name" render={({field}) => (
                        <FormItem>
                            <FormLabel>Name <RequiredAstrix /></FormLabel>
                            <FormControl>
                                <Input placeholder="The name of the organisation" { ... field } onBlur={(e) => onNameChange(e.target.value)} />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Duplicate organisation alert */}
                    <Alert variant="destructive" className="border-destructive" hidden={duplicateId === ""}>
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>Duplicate Organisation</AlertTitle>
                        <AlertDescription>
                            This organisation already exists! It has ID: {duplicateId}
                        </AlertDescription>
                    </Alert>
                    
                    {/* Description */}
                    <FormField control={form.control} name="Description" render={({field}) => (
                        <FormItem>
                            <FormLabel>Description <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Textarea placeholder="A short description of the organisation" rows={5} { ... field} />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Website */}
                    <FormField control={form.control} name="Website" render={({field}) => (
                        <FormItem>
                            <FormLabel>Website <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Input placeholder="The website of the organisation" { ... field } />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Email */}
                    <FormField control={form.control} name="EmailAddress" render={({field}) => (
                        <FormItem>
                            <FormLabel>Email Address <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Input type="email" placeholder="The email address of the organisation" { ... field } />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Related Organisations */}
                    <FormField control={form.control} name="OrganisationRelations" render={({field}) => (
                        <FormItem>
                            <FormLabel>Related Organisations</FormLabel>
                            <FormControl>
                                <AddRelationsDialog title="Define Relations" placeholder="Add related organisations..." options={organisations} emptyText="No organisations selected yet..." container={container} hasCreateButton={true} onCreateButton={() => setCreateOrganisationOpen(true)} { ... field } />
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