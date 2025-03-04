import { z } from "zod";

/**
 * Base tag scheme
 */
export const TagSchema = z.object({
    name: z.string(),
});

export const TagsArraySchema = z.array(TagSchema);

// Type definitions derived from the schemas
export type Tag = z.infer<typeof TagSchema>;
