import { z } from "zod"
import { ResourceCreateDto, WebsiteCreateDto, DocumentCreateDto, VideoCreateDto, AudioCreateDto, FileResourceCreateDto, PersonCreateDto, OrganisationCreateDto } from "@/types/uploadTypes"
import { resourceCreateFormSchema } from "@/components/new/NewResource"
import { ApiResponse } from "@/types/apiResponse.type";

/**
 * @summary Uploads a new resource to the databse
 * @param form The form of the NewResource page
 * @returns The ID of the new resource
 */
export async function UploadNewResource(form: z.infer<typeof resourceCreateFormSchema>): Promise<string> 
{
    // Helper function to convert dates to UTC ISO strings
    const convertToUTCDate = (dateValue: string): string => {
        if (/^\d{4}-\d{2}-\d{2}/.test(dateValue)) {
            // Create a date object and convert to UTC ISO string
            const date = new Date(dateValue);
            return date.toISOString();
        }
        return dateValue;
    };

    // Base resource schema that is common to all types
    const baseResourceDto: ResourceCreateDto =
    {
        Title: form.title,
        Description: form.description,
        TypeId: form.typeId,
        LanguageCode: form.languageCode,
        PublicationCode: form.publicationCode,
        PublicationDate: convertToUTCDate(form.publicationDate),
        License: form.license,
        Sources: form.sources,
        Note: form.note,
        Tags: form.tags,
        Authors: form.authors,
        Organisations: form.organisations,
        Regions: form.regions,
        RelatedOrganisations: form.relatedOrganisations,
        RelatedPersons: form.relatedPersons,
    };
    
    let dto;
    const endPoint = "/api/resources/new";
    
    // Create the appropiate DTO based on the uploadType
    if (form.uploadType === "website") 
    {
        console.log(form.url);
        dto =
        {
            ...baseResourceDto,
            Url: form.url,
            AccessedOn: form.accessedOn ? convertToUTCDate(form.accessedOn) : null,
        } as WebsiteCreateDto;
    }
    else
    {
        dto =
        {
            ...baseResourceDto,
            Hash: form.hash
        } as FileResourceCreateDto;
    }
    
    switch (form.uploadType) 
    {
        case "document":
            dto =
            {
                ...dto,
                Abstract: form.abstract,
            } as DocumentCreateDto;
            break;
            
        case "video":
            dto =
            {
                ...baseResourceDto,
                Length: form.length,
            } as VideoCreateDto;
            break;
            
        case "audio":
            dto =
            {
                ...baseResourceDto,
                Length: form.length,
            } as AudioCreateDto;
            break;
    }
    
    // Create formdata
    const formData = new FormData();
    
    if (form.file && form.uploadType !== "website")
        formData.append("file", form.file);
    
    formData.append("uploadType", form.uploadType);
    formData.append("dto", JSON.stringify(dto));
    
    // Send the request
    const response = await fetch(endPoint,
    {
        method: "PUT",
        credentials: "include",
        body: formData,
    });
    
    const result: ApiResponse = await response.json();
    
    if (!response.ok)
        throw new Error(`Upload failed: ${response.status} ${result.message}`);
    
    if (!result.success)
        throw new Error(`Upload failed: ${result.message}`);
        
    return result.body;
}

/**
 * @summary Uploads the given DTO to the given endpoint
 * @param endPoint The endpoint in the backend
 * @param dto The DTO to upload
 * @returns The ID of the newly created entity
 */
export async function UploadWithDto(endPoint: string, dto: unknown): Promise<string> 
{
    // Send the request
    const response = await fetch(endPoint,
    {
        method: "PUT",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(dto),
    });
    
    const result: ApiResponse = await response.json();
    
    if (!response.ok)
        throw new Error(`Upload failed: ${response.status} ${result.message}`);
        
    if (!result.success)
        throw new Error(`Upload failed: ${result.message}`);
        
    return result.body;
}