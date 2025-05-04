import { z } from "zod"

export const RelatedEntrySchema = z.object(
    {
        Id: z.string().uuid("Invalid UUID").min(1, "Please provide an ID"),
        Relation: z.string().optional(),
    })
    
    export type RelatedEntry = z.infer<typeof RelatedEntrySchema>;

export const ResourceCreateDtoSchema = z.object(
{
   Title: z.string().min(1, "Title is required"),
   Description: z.string().optional(),
   TypeId: z.string().min(1, "Resource Type ID is required").uuid("ResourceType ID should be a valid UUID"),
   LanguageCode: z.string().length(2, "Language code should be exactly two characters long"),
   PublicationCode: z.string().optional(),
   PublicationDate: z.string().min(1, "Publication date is required").date("Invalid date format"),
   License: z.string().optional(),
   Sources: z.string().array().optional(),
   Note: z.string().optional(),
   Tags: z.string().uuid("Please provide valid tag IDs").array(),
   Authors: z.string().uuid("Please provide valid person IDs").array(),
   Organisations: z.array(RelatedEntrySchema).optional(),
   Regions: z.string().uuid("Please provide valid region IDs").array().optional(),
   RelatedPersons: z.array(RelatedEntrySchema).optional(),
   RelatedOrganisations: z.array(RelatedEntrySchema).optional(),
   RelatedSources: z.string().array().optional(),
});

export type ResourceCreateDto = z.infer<typeof ResourceCreateDtoSchema>;

export const FileResourceCreateDtoSchema = ResourceCreateDtoSchema.extend(
{
    Hash: z.string().min(1, { message: "Please provide a hash" }),
});

export type FileResourceCreateDto = z.infer<typeof FileResourceCreateDtoSchema>;

export const WebsiteCreateDtoSchema = ResourceCreateDtoSchema.extend(
{
    Url: z.string().min(1, "URL is required").url("Invalid URL"),
    AccessedOn: z.string().date("Invalid date format").optional(),
});

export type WebsiteCreateDto = z.infer<typeof WebsiteCreateDtoSchema>;

export const VideoCreateDtoSchema = FileResourceCreateDtoSchema.extend(
{
    Length: z.number().optional(),
});

export type VideoCreateDto = z.infer<typeof VideoCreateDtoSchema>;

export const AudioCreateDtoSchema = FileResourceCreateDtoSchema.extend(
{
    Length: z.number().optional(),
});

export type AudioCreateDto = z.infer<typeof AudioCreateDtoSchema>;

export const DocumentCreateDtoSchema = FileResourceCreateDtoSchema.extend(
{
    Abstract: z.string().optional(),
});

export type DocumentCreateDto = z.infer<typeof DocumentCreateDtoSchema>;