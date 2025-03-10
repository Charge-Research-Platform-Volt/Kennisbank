import { z } from "zod";

/**
 * Base document schema without ID
 */
export const DocumentBaseSchema = z.object({
	name: z.string().min(1 , { message: "Name is required" }),
	description: z.string().min(1 , { message: "Description is required" }),
	file: z.instanceof(File, {message: "File is required"}),
	//tags: z.string().min(1, {message: "At least 1 tag is required"}),
	hash: z.string({ message: "Hash should be a string" }),
});

/**
 * Complete document schema with ID that extends the base document
 */
export const DocumentSchema = DocumentBaseSchema.extend({
	id: z.string().uuid(), // UUID validation
});

export const DocumentArraySchema = z.array(DocumentSchema);


export const DocumentResponseSchema = z.object({
	id: z.string().uuid(),
	name: z.string().min(1, { message: "Name is required" }),
	description: z.string().min(1, { message: "Description is required" }),
	fileType: z.string().min(1, { message: "File type is required" }),
	hash: z.string().min(1, { message: "Hash is required" }),
});

export const DocumentResponseArraySchema = z.array(DocumentResponseSchema);

export const DocumentPageResponseSchema = z.object({
	pageIndex: z.number().min(0, { message: "Page index should be a positive integer" }),
	pageSize: z.number().min(0, { message: "Page size should be a positive integer" }),
	files: DocumentResponseArraySchema,
	message: z.string(),
	responseType: z.string().min(1, { message: "Response type is required" }),
});

// Type definitions derived from the schemas
export type DocumentBase = z.infer<typeof DocumentBaseSchema>;
export type Document = z.infer<typeof DocumentSchema>;
export type DocumentPageResponse = z.infer<typeof DocumentPageResponseSchema>;
