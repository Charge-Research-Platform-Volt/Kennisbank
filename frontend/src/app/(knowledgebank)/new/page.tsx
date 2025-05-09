import { FormTabs, FormTabsTrigger } from "@/components/ui/form-tabs"
import { TabsContent, TabsList } from "@/components/ui/tabs"
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

interface NewPageProps 
{
  searchParams: { [key: string]: string | string[] | undefined };
}

/**
 * @summary A page where you can upload any kind entity to our database
 * @param searchParams The parameters given in the URL
 */
export default async function NewPage({ searchParams }: NewPageProps) 
{
    const returnUrl = typeof searchParams.returnUrl === 'string' ? searchParams.returnUrl : '/';
    
    // Fetch persons
    const personsFetch = await FetchWithValidation(ApiResponseSchema, `${process.env.API_URL}/persons/list`);
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!personsFetch.success || (personsFetch.data && !personsFetch.data.success && !PersonArraySchema.safeParse(personsFetch.data.body).success))
    {
        // Throw error to trigger error boundary
        const errorMessage = personsFetch.data?.message || 
                            "Failed to fetch persons";
        
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the personFetch to a options list
    const persons: Person[] = personsFetch.data.body;
    const personsOptions: SelectOption[] = [];
    persons.forEach((person: Person) => personsOptions.push({ value: person.id, label: person.name } as SelectOption));
    
    
    // Fetch organisations
    const organisationFetch = await FetchWithValidation(ApiResponseSchema, `${process.env.API_URL}/organisations/list`);
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!organisationFetch.success || (organisationFetch.data && !organisationFetch.data.success && !OrganisationArraySchema.safeParse(organisationFetch.data.body).success)) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    organisationFetch.data?.message ||
                                "Failed to fetch organisations";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the organisationFetch to a options list
    const organisations: Organisation[] = organisationFetch.data.body;
    const organisationOptions: SelectOption[] = [];
    organisations.forEach((organisation: Organisation) => organisationOptions.push({ value: organisation.id, label: organisation.name } as SelectOption));
    
    
    
    // Fetch resource types
    const typesFetch = await FetchWithValidation(ApiResponseSchema, `${process.env.API_URL}/resources/types/list`);
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!typesFetch.success || (typesFetch.data && !typesFetch.data.success && !ResourceTypeArraySchema.safeParse(typesFetch.data.body).success)) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    typesFetch.data?.message ||
                                "Failed to fetch resource types";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the typesFetch to a options list
    const resourceTypes: ResourceType[] = typesFetch.data.body;
    const typesOptions: SelectOption[] = [];
    resourceTypes.forEach((type: ResourceType) => typesOptions.push({ value: type.id, label: type.name } as SelectOption));
    
    
    
    // Fetch tags
    const tagsFetch = await FetchWithValidation(ApiResponseSchema, `${process.env.API_URL}/tags/all-tags`);
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!tagsFetch.success || (tagsFetch.data && !tagsFetch.data.success && !TagArraySchema.safeParse(tagsFetch.data.body).success)) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    tagsFetch.data?.message ||
                                "Failed to fetch tags";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Conver the tagsFetch to a options list
    const tags: Tag[] = tagsFetch.data.body;
    const tagOptions: SelectOption[] = [];
    tags.forEach((tag: Tag) => tagOptions.push({ value: tag.id, label: tag.name } as SelectOption));
    
    
    
    // Fetch regions
    const regionsFetch = await FetchWithValidation(ApiResponseSchema, `${process.env.API_URL}/regions/list`);
    
    // If fetch failed or API indicates failure, redirect to error page
    if (!regionsFetch.success || (regionsFetch.data && !regionsFetch.data.success && !RegionArraySchema.safeParse(regionsFetch.data.body).success)) 
    {
        // Throw error to trigger error boundary
        const errorMessage =    regionsFetch.data?.message ||
                                regionsFetch.error?.message ||
                                "Failed to fetch regions";
                                
        // This will trigger the nearest error.js boundary
        throw new Error(errorMessage);
    }
    
    // Convert the regionsFetch to a options list
    const regions: Region[] = regionsFetch.data.body;
    const regionOptions: SelectOption[] = [];
    regions.forEach((region: Region) => regionOptions.push({ value: region.id, label: region.name } as SelectOption));



    return (
        <FormTabs defaultValue="resource" className="w-full">
            <TabsList>
                <FormTabsTrigger value="resource">Resource</FormTabsTrigger>
                <FormTabsTrigger value="person">Person</FormTabsTrigger>
                <FormTabsTrigger value="organisation">Organisation</FormTabsTrigger>
            </TabsList>
            <TabsContent value="resource"><NewResource personOptions={personsOptions} organisationOptions={organisationOptions} resourceTypeOptions={typesOptions} tagOptions={tagOptions} regionOptions={regionOptions} returnUrl={returnUrl} /></TabsContent>
            <TabsContent value="person"><NewPerson personOptions={personsOptions} organisationOptions={organisationOptions} returnUrl={returnUrl} /></TabsContent>
            <TabsContent value="organisation"><NewOrganisation organisationOptions={organisationOptions} returnUrl={returnUrl} /></TabsContent>
        </FormTabs>
    );
}