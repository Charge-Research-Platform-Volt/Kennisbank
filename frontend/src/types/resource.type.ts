import { z } from "zod";

/**
 * Base document schema without ID
 */
export const ResourceBaseSchema = z.object({
	title: z.string().min(1 , { message: "Title is required" }),
	description: z.string(),
  typeId: z.string().min(1, {message: "Type is required"}),
  languageCode: z.string().min(1, {message: "languageCode is required"}).max(2, {message: "Cant be longer than 2 characters"}),
  publicationDate: z.date(),
	file: z.instanceof(File, {message: "File is required"}),
  fileType: z.string().min(1, {message: "File type is required"}),
	//tags: z.string().min(1, {message: "At least 1 tag is required"}),
	hash: z.string({ message: "Hash should be a string" }),
});

/**
 * Complete document schema with ID that extends the base document
 */
export const ResourceSchema = ResourceBaseSchema.extend({
  id: z.string().uuid(), // UUID validation
});

export const ResourceArraySchema = z.array(ResourceSchema);

export const ResourceResponseSchema = z.object({
  id: z.string().uuid(),
  title: z.string().min(1, { message: "Title is required" }),
  description: z.string().min(0, { message: "Description is required" }),
  typeId: z.string().min(1, { message: "Type is required" }),
  languageCode: z.string().min(1, { message: "Language code is required" }).length(2, { message: "Language code should be two characters long" }),
  publicationCode: z.string().nullable(),
  license: z.string().nullable(),
  note: z.string().nullable(),
  fileType: z.string().min(1, { message: "File type is required" }),
  hash: z.string().min(1, { message: "Hash is required" }).nullable(),
  creationDate: z.string().min(1, { message: "Created at is required" }),
  publicationDate: z.string().min(1, { message: "Updated at is required" }),
});

export const ResourceResponseArraySchema = z.array(ResourceResponseSchema);

export const ResourcePageResponseSchema = z.object({
  pageIndex: z.number().min(0, { message: "Page index should be a positive integer" }),
  pageSize: z.number().min(0, { message: "Page size should be a positive integer" }),
  resources: ResourceResponseArraySchema,
  message: z.string(),
  responseType: z.string().min(1, { message: "Response type is required" }),
});

// Type definitions derived from the schemas
export type ResourceBase = z.infer<typeof ResourceBaseSchema>;
export type Resource = z.infer<typeof ResourceSchema>;
export type ResourcePageResponse = z.infer<typeof ResourcePageResponseSchema>;
