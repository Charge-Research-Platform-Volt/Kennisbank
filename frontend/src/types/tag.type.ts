import { z } from "zod";

/**
 * Base tag scheme
 */
export const TagBaseSchema = z.object({
    name: z.string().min(1, { message: "Name is required" }),
});

export const TagSchema = TagBaseSchema.extend({
    id: z.string().uuid(),
});

export const UserTagBaseSchema = TagBaseSchema.extend({});

export const UserTagSchema = TagSchema.extend({
    isApproved: z.boolean().default(false),
    user: z
        .object({
            id: z.string().uuid(),
            name: z.string(),
        })
        .optional(),  // TODO: not optional once authentication etc has been implemented
});

export const TagsArraySchema = z.array(TagSchema);

// Type definitions derived from the schemas
export type TagBase = z.infer<typeof TagBaseSchema>;
export type Tag = z.infer<typeof TagSchema>;
export type UserTagBase = z.infer<typeof UserTagBaseSchema>;
export type UserTag = z.infer<typeof UserTagSchema>;