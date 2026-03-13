import { z } from "zod"

/**
 * Schema to define a relation
 */
export const RelatedEntrySchema = z.object(
{
    Id: z.string().uuid("Invalid UUID").min(1, "Please provide an ID"),
    Relation: z.string().optional(),
})

/**
 * Type to define a relation
 */
export type RelatedEntry = z.infer<typeof RelatedEntrySchema>;

/**
 * Schema for creating a resource create DTO
 */
export const ResourceCreateDtoSchema = z.object(
{
   Title: z.string().min(1, "Title is required"),
   Description: z.string().nullish(),
   TypeId: z.string().uuid("ResourceType ID should be a valid UUID").optional(),
   LanguageCode: z.string().length(2, "Language code should be exactly two characters long"),
   PublicationCode: z.string().nullish(),
   PublicationDate: z.string().datetime("Invalid date format").nullish(),
   PublicationDatePrecision: z.enum(['Year', 'Month', 'Day']).nullish(),
   CreationDate: z.string().datetime("Invalid date format").nullish(),
   License: z.string().nullish(),
   SourceUrl: z.string().nullish(),
   Note: z.string().nullish(),
   Tags: z.string().array().default([]),
   // Authors with type information (value is GUID for existing, or name for new; type is needed for new entities)
   Authors: z.array(z.object({
       value: z.string(),
       type: z.string().optional() // "person" or "organisation" - needed when creating new entities
   })).default([]),
   // Organisations can be either GUIDs (existing) or names (new to be created)
   Organisations: z.array(z.object({ Id: z.string(), Relation: z.string().optional() })).default([]),
   Regions: z.string().array().default([]),
   // RelatedPersons can be either GUIDs (existing) or names (new to be created)
   RelatedPersons: z.array(z.object({ Id: z.string(), Relation: z.string().optional() })).default([]),
});

/**
 * DTO to send to backend to create a resource
 */
export type ResourceCreateDto = z.infer<typeof ResourceCreateDtoSchema>;

/**
 * Schema for creating a resource create DTO with a file
 */
export const FileResourceCreateDtoSchema = ResourceCreateDtoSchema.extend(
{
    Hash: z.string().length(64, "Hash must be 64 characters (SHA-256)").optional(),
    FileExtension: z.string().default(""),
    Id: z.string().uuid("Invalid file ID").min(1, "File ID is required"),
});

/**
 * DTO to send to backend to create a resource with a file
 */
export type FileResourceCreateDto = z.infer<typeof FileResourceCreateDtoSchema>;

/**
 * Schema for creating a website create DTO
 */
export const WebsiteCreateDtoSchema = ResourceCreateDtoSchema.extend(
{
    Url: z.string().min(1, "URL is required").url("Invalid URL"),
});

/**
 * DTO to send to backend to create a website
 */
export type WebsiteCreateDto = z.infer<typeof WebsiteCreateDtoSchema>;

/**
 * Schema for creating a resource create DTO that is a video
 */
export const VideoCreateDtoSchema = FileResourceCreateDtoSchema.extend(
{
    Length: z.number().optional(),
});

/**
 * DTO to send to backend to create a video resource
 */
export type VideoCreateDto = z.infer<typeof VideoCreateDtoSchema>;

/**
 * Schema for creating a resource create DTO that is an audio file
 */
export const AudioCreateDtoSchema = FileResourceCreateDtoSchema.extend(
{
    Length: z.number().optional(),
});

/**
 * DTO to send to backend to create an audio resource
 */
export type AudioCreateDto = z.infer<typeof AudioCreateDtoSchema>;

/**
 * Schema for creating a resource create DTO that is a document
 */
export const DocumentCreateDtoSchema = FileResourceCreateDtoSchema.extend(
{
    Abstract: z.string().optional(),
});

/**
 * DTO to send to backend to create a document
 */
export type DocumentCreateDto = z.infer<typeof DocumentCreateDtoSchema>;



/**
 * Schema for creating a person create DTO
 */
export const PersonCreateDtoSchema = z.object(
{
    Name: z.string().min(1, "Name is required"),
    Occupation: z.string().min(1, "Occupation is required"),
    Description: z.string().optional(),
    EmailAddress: z.string().email("Invalid e-mail address").or(z.literal("")).optional(),
    Linkedin: z.string().url("Invalid URL").or(z.literal("")).optional(),
    OrganisationRelations: z.array(RelatedEntrySchema),
    PersonRelations: z.array(RelatedEntrySchema),
});

/**
 * DTO to send to backend to create a person
 */
export type PersonCreateDto = z.infer<typeof PersonCreateDtoSchema>;



/**
 * Schema for creating an organisation create DTO
 */
export const OrganisationCreateDtoSchema = z.object(
{
    Name: z.string().min(1, "Name is required"),
    Description: z.string().optional(),
    Website: z.string().optional(),
    EmailAddress: z.string().email("Invalid e-mail address").or(z.literal("")).optional(),
    OrganisationRelations: z.array(RelatedEntrySchema),
});

/**
 * DTO to send to backend to create an organistation
 */
export type OrganisationCreateDto = z.infer<typeof OrganisationCreateDtoSchema>;



/**
 * Schema for creating a resource type create DTO
 */
export const ResourceTypeCreateDtoSchema = z.object(
{
    Name: z.string().min(1, "Name is required"),
});

/**
 * DTO to send to backend to create a resource type
 */
export type ResourceTypeCreateDto = z.infer<typeof ResourceTypeCreateDtoSchema>;



/**
 * Schema for creating a region create DTO
 */
export const RegionCreateDtoSchema = z.object(
{
    Name: z.string().min(1, "Name is required"),
});

/**
 * DTO to send to backend to create a region
 */
export type RegionCreateDto = z.infer<typeof RegionCreateDtoSchema>;



/**
 * Schema for finalizing a large file upload
 */
export const LargeFileFinalizeDtoSchema = z.object(
{
    ResourceId: z.string().uuid("Invalid UUID").min(1, "Please provide an ID"),
    FileType: z.string().min(1, "FileType is required"),
    FileName: z.string().min(1, "FileName is required"),
    BlockIds: z.array(z.string())
});

/**
 * DTO to send to backend to finalize large file upload
 */
export type LargeFileFinalizeDto = z.infer<typeof LargeFileFinalizeDtoSchema>;


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
