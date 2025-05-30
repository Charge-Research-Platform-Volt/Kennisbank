import { z } from "zod";
import { TagRelationArraySchema } from "./tag.type";

/**
 * Base document schema without ID
 */
export const ResourceBaseSchema = z.object({
	title: z.string().min(1 , { message: "Title is required" }),
	description: z.string().nullable(),
  typeId: z.string().min(1, {message: "Type is required"}),
  languageCode: z.string().min(1, {message: "languageCode is required"}).max(2, {message: "Cant be longer than 2 characters"}),
  publicationDate: z.date()
});

export const FileResourceSchema = ResourceBaseSchema.extend({
	file: z.instanceof(File, {message: "File is required"}),
	hash: z.string({ message: "Hash should be a string" })
})

export const WebsiteResourceSchema = ResourceBaseSchema.extend({
  url: z.string().min(1, {message: "URL is required"})
})

/**
 * Complete document schema with ID that extends the base document
 */
export const ResourceSchema = ResourceBaseSchema.extend({
  id: z.string().uuid(), // UUID validation
});

export const ResourceArraySchema = z.array(ResourceSchema);

export const BaseResourceResponseSchema = z.object({
  id: z.string().uuid(),
  title: z.string().min(1, { message: "Title is required" }),
  description: z.string().nullable(),
  typeId: z.string().min(1, { message: "Type is required" }),
  fileType: z.string().min(1, { message: "File type is required" }),
  languageCode: z.string().min(1, { message: "Language code is required" }).length(2, { message: "Language code should be two characters long" }),
  publicationCode: z.string().nullable(),
  license: z.string().nullable(),
  note: z.string().nullable(),
  creationDate: z.string().min(1, { message: "Created at is required" }),
  publicationDate: z.string().min(1, { message: "Updated at is required" }),
  archived: z.boolean().nullable().optional(),
  archivedDate: z.string().nullable().optional(),
})

export const ResourceResponseSchema = BaseResourceResponseSchema.extend({
  hash: z.string().min(1, { message: "Hash is required" }).nullable().optional(),
});

export const WebsiteResponseSchema = BaseResourceResponseSchema.extend({
  url: z.string().optional(),
})

export const ResponseSchema = z.union([
  ResourceResponseSchema,
  WebsiteResponseSchema
]);

export const ResourceResponseArraySchema = z.array(ResponseSchema);

export const ResourcePageResponseSchema = z.object({
  pageIndex: z.number().min(0, { message: "Page index should be a positive integer" }),
  pageSize: z.number().min(0, { message: "Page size should be a positive integer" }),
  resources: ResourceResponseArraySchema,
  message: z.string(),
  responseType: z.string().min(1, { message: "Response type is required" }),
});

export const ResourceWithTagsResponseSchema = ResourceResponseSchema.extend({
  tagRelations: TagRelationArraySchema,
});

export const ResourceWithTagsResponseArraySchema = z.array(ResourceWithTagsResponseSchema);

export const ResourcePageWithTagsResponseSchema = ResourcePageResponseSchema.extend({
  resources: ResourceWithTagsResponseArraySchema,
});

/**
 * For projects implementation we use a wrapper
 */

export const ResourceProjectSchema = z.object({
  resource: ResourceSchema,
  addedBy: z.string(),
})

// Type definitions derived from the schemas
export type ResourceBase = z.infer<typeof ResourceBaseSchema>;
export type FileBase = z.infer<typeof FileResourceSchema>;
export type WebsiteBase = z.infer<typeof WebsiteResourceSchema>;
export type Resource = z.infer<typeof ResourceSchema>;
export type ResourcePageResponse = z.infer<typeof ResourcePageResponseSchema>;
export type ResourceWithTagsResponse = z.infer<typeof ResourceWithTagsResponseSchema>;
export type ResourcePageWithTagsResponse = z.infer<typeof ResourcePageWithTagsResponseSchema>;
export type ResourceResponse = z.infer<typeof ResourceResponseSchema>;
export type ResourceProject = z.infer<typeof ResourceProjectSchema>

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


