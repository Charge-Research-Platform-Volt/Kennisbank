import { z } from "zod";

/**
 * Base tag scheme
 */
export const TagBaseSchema = z.object({
    name: z.string(),
});

export const TagSchema = TagBaseSchema.extend({
    id: z.string().uuid(),
});

export const TagsArraySchema = z.array(TagSchema);

// Type definitions derived from the schemas
export type TagBase = z.infer<typeof TagBaseSchema>;
export type Tag = z.infer<typeof TagSchema>;