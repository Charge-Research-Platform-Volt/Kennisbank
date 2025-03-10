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
    isApproved: z.boolean(),
    user: z.string().nullable(), // TODO: should be the actual user object
});

export const TagsArraySchema = z.array(TagSchema);
export const UserTagsArraySchema = z.array(UserTagSchema);

// Type definitions derived from the schemas
export type TagBase = z.infer<typeof TagBaseSchema>;
export type Tag = z.infer<typeof TagSchema>;
export type UserTagBase = z.infer<typeof UserTagBaseSchema>;
export type UserTag = z.infer<typeof UserTagSchema>;