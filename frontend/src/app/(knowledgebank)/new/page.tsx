import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import NewResource from "@/components/new/NewResource"
import NewPerson from "@/components/new/NewPerson"
import NewOrganisation from "@/components/new/NewOrganisation";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { ApiResponseSchema } from "@/types/apiResponse.type"
import { Person, PersonArraySchema } from "@/types/person.type"
import { ResourceType, ResourceTypeArraySchema } from "@/types/resourceType.type"
import { Tag, TagArraySchema } from "@/types/tag.type"
import { SelectOption } from "@/components/ui/selection";


export default async function NewPage() 
{
    // Fetch persons
    const personsFetch = await FetchWithValidation(ApiResponseSchema, "http://backend:8080/persons/list");
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!personsFetch.success || (personsFetch.data && !personsFetch.data.success && !PersonArraySchema.safeParse(personsFetch.data.data).success))
    {
        // Throw error to trigger error boundary
        const errorMessage = personsFetch.data?.message || 
                            personsFetch.data?.errors?.join(', ') || 
                            "Failed to fetch author data";
        
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage)
    }
    
    // Convert the authorFetch to a options list
    const persons: Person[] = personsFetch.data.data;
    const personsOptions: SelectOption[] = [];
    persons.forEach((person: Person) => personsOptions.push({ value: person.id, label: person.name } as SelectOption));
    
    
    
    // Fetch resource types
    const typesFetch = await FetchWithValidation(ApiResponseSchema, "http://backend:8080/resources/types/list");
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!typesFetch.success || (typesFetch.data && !typesFetch.data.success && !ResourceTypeArraySchema.safeParse(typesFetch.data.data).success)) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    typesFetch.data?.message ||
                                typesFetch.data?.errors?.join(', ') ||
                                "Failed to fetch resource types";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the typesFetch to a options list
    const resourceTypes: ResourceType[] = typesFetch.data.data;
    const typesOptions: SelectOption[] = [];
    resourceTypes.forEach((type: ResourceType) => typesOptions.push({ value: type.id, label: type.name } as SelectOption));
    
    
    
    // Fetch tags
    const tagsFetch = await FetchWithValidation(TagArraySchema, "http://backend:8080/tags/tags");
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!tagsFetch.success) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    tagsFetch.error.message ||
                                "Failed to fetch tags";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Conver the tagsFetch to a options list
    const tags: Tag[] = tagsFetch.data;
    const tagOptions: SelectOption[] = [];
    tags.forEach((tag: Tag) => tagOptions.push({ value: tag.id, label: tag.name } as SelectOption));



    return (
        <Tabs defaultValue="resource" className="w-full">
            <TabsList>
                <TabsTrigger value="resource">Resource</TabsTrigger>
                <TabsTrigger value="person">Person</TabsTrigger>
                <TabsTrigger value="organisation">Organisation</TabsTrigger>
            </TabsList>
            <TabsContent value="resource"><NewResource persons={personsOptions} resourceTypes={typesOptions} tags={tagOptions} /></TabsContent>
            <TabsContent value="person"><NewPerson /></TabsContent>
            <TabsContent value="organisation"><NewOrganisation /></TabsContent>
        </Tabs>
    );
}