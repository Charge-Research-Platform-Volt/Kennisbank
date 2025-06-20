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
import { useRouter, useSearchParams } from "next/navigation"
import { RequiredAstrix } from "@/components/ui/required-astrix"
import { AddRelationsDialog } from "@/components/ui/add-relations-dialog"
import { PersonCreateDto, PersonCreateDtoSchema, RelatedEntry } from "@/types/uploadTypes"
import NewOrganisation from "@/components/new/NewOrganisation"
import { Drawer, DrawerContent, DrawerHeader, DrawerTitle } from "@/components/ui/drawer"
import { useDrawerRerender } from "@/utils/useDrawerRerenderer"
import { useFormHasValues } from "@/hooks/useFormNonDefaultValues"
import { useTabsContext } from "@/context/tabs-context"

interface NewPersonProps 
{
    personOptions: SelectOption[];
    organisationOptions: SelectOption[];
    onCreate?: (id: string, name: string) => void;
    container?: HTMLElement | null;
    updatePersons?: (organisations: SelectOption[]) => void;
    updateOrganisations?: (organisations: SelectOption[]) => void;
    useUnsavedDialog?: boolean;
}

/**
 * @summary Form component that allows for uploading persons to the database
 * @param personOptions The complete list of all persons in the databse
 * @param organisationOptions The complete list of all organisations in the database
 * @param onCreate This function is called when the person is successfully uploaded to the database
 * @param container The container in which this component is rendered. Required for some components in order to work properly in dialogs/drawers
 * @param updatePersons This function is called when the collection of persons is changed
 * @param updateOrganisations This function is called when the collection of organisations is changed
 * @param useUnsavedDialog Determines if there should be a UnsavedDialog whenever some fields are filled and the user tries to navigate
 */
export default function NewPerson({ personOptions, organisationOptions, onCreate, container = null, updatePersons, updateOrganisations, useUnsavedDialog = true }: NewPersonProps) 
{
    // React states
    const [isChecking, setIsChecking] = React.useState<boolean>(false);
    const [duplicateId, setDuplicateId] = React.useState<string>("");
    
    const [persons, setPersons] = React.useState<SelectOption[]>(personOptions);
    const [organisations, setOrganisations] = React.useState<SelectOption[]>(organisationOptions);
    
    const [createPersonOpen, setCreatePersonOpen] = React.useState<boolean>(false);
    const [createOrganisationOpen, setCreateOrganisationOpen] = React.useState<boolean>(false);
    
    const personDrawerRef = React.useRef<HTMLDivElement>(null);
    const organisationDrawerRef = React.useRef<HTMLDivElement>(null);
    
    const router = useRouter();
    const searchParams = useSearchParams();
        
    const returnUrl = searchParams.get('returnUrl') || '/';
    
    useDrawerRerender([createPersonOpen, createOrganisationOpen]);
    
    // Effects to do upstream synchronization of the persons and organisations list
    React.useEffect(() => { if (updatePersons) updatePersons(persons) }, [persons]);
    React.useEffect(() => { if (updateOrganisations) updateOrganisations(organisations) }, [organisations]);
    
    // Define the form
    const form = useForm<z.infer<typeof PersonCreateDtoSchema>>(
    {
        resolver: zodResolver(PersonCreateDtoSchema),
        defaultValues:
        {
            Name: "",
            Occupation: "",
            Description: "",
            EmailAddress: "",
            Linkedin: "",
            OrganisationRelations: [],
            PersonRelations: [],
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
        const response = await fetch("/api/persons/exists?name=" + urlSafeName, { credentials: "include" });
        
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
    async function onSubmit(dto: PersonCreateDto) 
    {
        // Disable the submit button
        setIsChecking(true);
    
        const id = await UploadWithDto("/api/persons/new", dto);
        toast.info(`Person created successfully with ID '${id}'`);
        
        if (onCreate)
            onCreate(id, dto.Name);
        else
            router.push(returnUrl);
    }
    
    // Handle person creation
    async function onPersonCreation(id: string, name: string) 
    {
        const newPersons = [...persons, { value: id, label: name } as SelectOption];
        setPersons(newPersons);
        
        const newSelected = [...form.getValues("PersonRelations"), { Id: id, Relation: "" } as RelatedEntry];
        form.setValue("PersonRelations", newSelected);
        
        form.trigger("PersonRelations");
        
        setCreatePersonOpen(false);
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
            <Drawer open={createPersonOpen} onOpenChange={setCreatePersonOpen}>
                <DrawerContent className="flex flex-col max-h-[90vh]" ref={personDrawerRef} forceMount>
                    <DrawerHeader hidden={true}>
                        <DrawerTitle>Create a new person</DrawerTitle>
                    </DrawerHeader>
                    <div className="flex-1 overflow-y-auto p-4">
                        <NewPerson personOptions={persons} organisationOptions={organisations} onCreate={onPersonCreation} container={personDrawerRef.current} updatePersons={setPersons} updateOrganisations={setOrganisations} useUnsavedDialog={false} />
                    </div>
                </DrawerContent>
            </Drawer>
            
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
                Create a new person
            </h1>

            <hr className="mb-4" />
            
            <Form { ... form}>
                <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-6">
                    {/* Name input */}
                    <FormField control={form.control} name="Name" render={({field}) => (
                    <FormItem>
                        <FormLabel>Name <RequiredAstrix /></FormLabel>
                        <FormControl>
                            <Input placeholder="The name of the person" { ... field } onBlur={(e) => onNameChange(e.target.value)} />
                        </FormControl>
                        <FormMessage />
                    </FormItem>
                    )} />
                    
                    {/* Duplicate person alert */}
                    <Alert variant="destructive" className="border-destructive" hidden={duplicateId === ""}>
                        <AlertCircle className="h-4 w-4" />
                        <AlertTitle>Duplicate Person</AlertTitle>
                        <AlertDescription>
                            This person already exists! It has ID: {duplicateId}
                        </AlertDescription>
                    </Alert>
                    
                    {/* Occupation input */}
                    <FormField control={form.control} name="Occupation" render={({field}) => (
                        <FormItem>
                            <FormLabel>Occupation <RequiredAstrix /></FormLabel>
                            <FormControl>
                                <Input placeholder="The occupation of the person" { ... field } />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Description */}
                    <FormField control={form.control} name="Description" render={({field}) => (
                        <FormItem>
                            <FormLabel>Description <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Textarea placeholder="A short description of the person" rows={5} { ... field} />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Email */}
                    <FormField control={form.control} name="EmailAddress" render={({field}) => (
                        <FormItem>
                            <FormLabel>Email Address <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Input type="email" placeholder="The email address of the person" { ... field } />
                            </FormControl>
                            <FormMessage />
                        </FormItem>
                    )} />
                    
                    {/* Linkedin */}
                    <FormField control={form.control} name="Linkedin" render={({field}) => (
                        <FormItem>
                            <FormLabel>LinkedIn <code>(Optional)</code></FormLabel>
                            <FormControl>
                                <Input placeholder="Paste LinkedIn URL here..." { ... field } />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Related Persons */}
                    <FormField control={form.control} name="PersonRelations" render={({field}) => (
                        <FormItem>
                            <FormLabel>Related Persons</FormLabel>
                            <FormControl>
                                <AddRelationsDialog title="Define Relations" placeholder="Add related persons..." options={persons} emptyText="No persons selected yet..." container={container} hasCreateButton={true} onCreateButton={() => setCreatePersonOpen(true)} { ... field } />
                            </FormControl>
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


