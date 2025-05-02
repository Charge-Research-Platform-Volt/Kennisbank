import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import NewResource from "@/components/new/NewResource"
import NewPerson from "@/components/new/NewPerson"
import NewOrganisation from "@/components/new/NewOrganisation";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { ApiResponseSchema } from "@/types/apiResponse.type"
import { Person, PersonArraySchema } from "@/types/person.type"
import { Organisation, OrganisationArraySchema } from "@/types/organisation.type";
import { ResourceType, ResourceTypeArraySchema } from "@/types/resourceType.type"
import { Tag, TagArraySchema } from "@/types/tag.type"
import { Region, RegionArraySchema } from "@/types/region.type"
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
                            "Failed to fetch persons";
        
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the personFetch to a options list
    const persons: Person[] = personsFetch.data.data;
    const personsOptions: SelectOption[] = [];
    persons.forEach((person: Person) => personsOptions.push({ value: person.id, label: person.name } as SelectOption));
    
    
    // Fetch organisations
    const organisationFetch = await FetchWithValidation(ApiResponseSchema, "http://backend:8080/organisations/list");
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!organisationFetch.success || (organisationFetch.data && !organisationFetch.data.success && !OrganisationArraySchema.safeParse(organisationFetch.data.data).success)) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    organisationFetch.data?.message ||
                                organisationFetch.data?.errors?.join(", ") ||
                                "Failed to fetch organisations";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the organisationFetch to a options list
    const organisations: Organisation[] = organisationFetch.data.data;
    const organisationOptions: SelectOption[] = [];
    organisations.forEach((organisation: Organisation) => organisationOptions.push({ value: organisation.id, label: organisation.name } as SelectOption));
    
    
    
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
    
    
    
    // Fetch regions
    const regionsFetch = await FetchWithValidation(ApiResponseSchema, "http://backend:8080/regions/list");
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!regionsFetch.success || (regionsFetch.data && !regionsFetch.data.success && !RegionArraySchema.safeParse(regionsFetch.data.data).success)) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    regionsFetch.data?.message ||
                                regionsFetch.data?.errors?.join(', ') ||
                                regionsFetch.error?.message ||
                                "Failed to fetch regions";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the regionsFetch to a options list
    const regions: Region[] = regionsFetch.data.data;
    const regionOptions: SelectOption[] = [];
    regions.forEach((region: Region) => regionOptions.push({ value: region.id, label: region.name } as SelectOption));



    return (
        <Tabs defaultValue="resource" className="w-full">
            <TabsList>
                <TabsTrigger value="resource">Resource</TabsTrigger>
                <TabsTrigger value="person">Person</TabsTrigger>
                <TabsTrigger value="organisation">Organisation</TabsTrigger>
            </TabsList>
            <TabsContent value="resource"><NewResource persons={personsOptions} organisations={organisationOptions} resourceTypes={typesOptions} tags={tagOptions} regions={regionOptions} /></TabsContent>
            <TabsContent value="person"><NewPerson /></TabsContent>
            <TabsContent value="organisation"><NewOrganisation /></TabsContent>
        </Tabs>
    );
}