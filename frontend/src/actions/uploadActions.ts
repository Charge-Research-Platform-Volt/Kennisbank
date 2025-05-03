import { z } from "zod"
import { ResourceCreateDto, WebsiteCreateDto, DocumentCreateDto, VideoCreateDto, AudioCreateDto, FileResourceCreateDto } from "@/types/uploadTypes"
import { resourceCreateFormSchema } from "@/components/new/NewResource"
import { ApiResponse } from "@/types/apiResponse.type";

export async function UploadNewResource(values: z.infer<typeof resourceCreateFormSchema>): Promise<string> 
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
        Title: values.title,
        Description: values.description,
        TypeId: values.typeId,
        LanguageCode: values.languageCode,
        PublicationCode: values.publicationCode,
        PublicationDate: convertToUTCDate(values.publicationDate),
        License: values.license,
        Sources: values.sources,
        Note: values.note,
        Tags: values.tags,
        Authors: values.authors,
        Organisations: values.organisations,
        Regions: values.regions,
    };
    
    let dto;
    const endPoint = "http://localhost:8080/resources/new";
    
    // Create the appropiate DTO based on the uploadType
    if (values.uploadType === "website") 
    {
        console.log(values.url);
        dto =
        {
            ...baseResourceDto,
            Url: values.url,
            AccessedOn: values.accessedOn ? convertToUTCDate(values.accessedOn) : null,
        } as WebsiteCreateDto;
    }
    else
    {
        dto =
        {
            ...baseResourceDto,
            Hash: values.hash
        } as FileResourceCreateDto;
    }
    
    switch (values.uploadType) 
    {
        case "document":
            dto =
            {
                ...dto,
                Abstract: values.abstract,
            } as DocumentCreateDto;
            break;
            
        case "video":
            dto =
            {
                ...baseResourceDto,
                Length: values.length,
            } as VideoCreateDto;
            break;
            
        case "audio":
            dto =
            {
                ...baseResourceDto,
                Length: values.length,
            } as AudioCreateDto;
            break;
    }
    
    // Create formdata
    const formData = new FormData();
    
    if (values.file && values.uploadType !== "website")
        formData.append("file", values.file);
    
    formData.append("uploadType", values.uploadType);
    formData.append("dto", JSON.stringify(dto));
    
    // Send the request
    const response = await fetch(endPoint,
    {
        method: "PUT",
        credentials: "include",
        body: formData,
    });
    
    if (!response.ok)
        throw new Error(`Upload failed: ${response.status}`);
        
    const result: ApiResponse = await response.json();
    
    if (!result.success)
        throw new Error(`Upload failed: ${result.errors?.join(', ') || result.message}`);
        
    return result.body;
}