import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import NewResource from "@/components/new/NewResource"
import NewPerson from "@/components/new/NewPerson"
import NewOrganisation from "@/components/new/NewOrganisation";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { ApiResponseSchema } from "@/types/apiResponse.type"
import { Person, PersonArraySchema } from "@/types/person.type"
import { MultiselectOption } from "@/components/ui/multiselect-combobox"


export default async function NewPage() 
{
    const personsFetch = await FetchWithValidation(ApiResponseSchema, "http://backend:8080/persons/list");
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!personsFetch.success || (personsFetch.data && !personsFetch.data.success && !PersonArraySchema.safeParse(personsFetch.data.data).success)) {
        // Use error() function to trigger the nearest error.js boundary
        const errorMessage = personsFetch.data?.message || 
                            personsFetch.data?.errors?.join(', ') || 
                            "Failed to fetch author data";
        
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage)
    }
    
    // Convert the authorFetch to a options list
    const persons: Person[] = personsFetch.data.data;
    const personsOptions: MultiselectOption[] = [];
    persons.forEach((person: Person) => personsOptions.push({ value: person.id, label: person.name } as MultiselectOption));

    return (
        <Tabs defaultValue="resource" className="w-full">
            <TabsList>
                <TabsTrigger value="resource">Resource</TabsTrigger>
                <TabsTrigger value="person">Person</TabsTrigger>
                <TabsTrigger value="organisation">Organisation</TabsTrigger>
            </TabsList>
            <TabsContent value="resource"><NewResource persons={personsOptions} /></TabsContent>
            <TabsContent value="person"><NewPerson /></TabsContent>
            <TabsContent value="organisation"><NewOrganisation /></TabsContent>
        </Tabs>
    );
}