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
import { PersonCreateDto, PersonCreateDtoSchema } from "@/types/uploadTypes"

interface NewPersonProps 
{
    personOptions: SelectOption[];
    organisationOptions: SelectOption[];
    onCreate?: (id: string, name: string) => void;
    container?: HTMLElement | null;
}

export default function NewPerson({ personOptions, organisationOptions, onCreate, container = null }: NewPersonProps) 
{
    // React states
    const [isChecking, setIsChecking] = React.useState<boolean>(false);
    const [duplicateId, setDuplicateId] = React.useState<string>("");
    
    const [persons, setPersons] = React.useState<SelectOption[]>(personOptions);
    const [organisations, setOrganisations] = React.useState<SelectOption[]>(organisationOptions);
    
    const router = useRouter();
    
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
    
    // Handle name changing
    async function onNameChange(name: string) 
    {
        // Set the value in the form
        form.setValue("Name", name);
        
        // Do nothing if empty
        if (!name) return;
        
        setIsChecking(true);
        
        const urlSafeName = encodeURIComponent(name);
        const response = await fetch("http://localhost:8080/persons/exists?name=" + urlSafeName, { credentials: "include" });
        
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
            router.push('/');
    }

    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-7xl mt-5">
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
                                <AddRelationsDialog title="Define Relations" placeholder="Add related persons..." options={persons} emptyText="No persons selected yet..." container={container} { ... field } />
                            </FormControl>
                        </FormItem>
                    )} />
                    
                    {/* Related Organisations */}
                    <FormField control={form.control} name="OrganisationRelations" render={({field}) => (
                        <FormItem>
                            <FormLabel>Related Organisations</FormLabel>
                            <FormControl>
                                <AddRelationsDialog title="Define Relations" placeholder="Add related organisations..." options={organisations} emptyText="No organisations selected yet..." container={container} { ... field } />
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