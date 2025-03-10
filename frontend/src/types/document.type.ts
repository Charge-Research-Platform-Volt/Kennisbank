import { z } from "zod";

/**
 * Base document schema without ID
 */
export const DocumentBaseSchema = z.object({
	name: z.string().min(1 , { message: "Name is required" }),
	description: z.string().min(1 , { message: "Description is required" }),
	file: z.instanceof(File, {message: "File is required"}),
	tags: z.string().min(1, {message: "At least 1 tag is required"})
});

/**
 * Complete document schema with ID that extends the base document
 */
export const DocumentSchema = DocumentBaseSchema.extend({
	id: z.string().uuid(), // UUID validation
});

export const DocumentArraySchema = z.array(DocumentSchema);

// Type definitions derived from the schemas
export type DocumentBase = z.infer<typeof DocumentBaseSchema>;
export type Document = z.infer<typeof DocumentSchema>;
