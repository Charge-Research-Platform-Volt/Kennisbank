import { z } from "zod";
import { ResourceResponseSchema } from "./resource.type";

/**
 * Tag scheme
 */
export const TagSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1, { message: "Name is required" }),
  isStandardized: z.boolean(),
  isApproved: z.boolean(),
  approvedOn: z.string().nullable(),
  approvedBy: z.string().uuid().nullable(),
  createdBy: z.string().uuid().min(1, { message: "CreatedBy is required" }),
  createdOn: z.string(),
});

export const TagArraySchema = z.array(TagSchema);

// Type definitions derived from the schemas
export type Tag = z.infer<typeof TagSchema>;

export type TagArray = z.infer<typeof TagArraySchema>;

/**
 * Tag create dto scheme
 */
export const TagCreateDtoSchema = z.object({
  name: z.string().min(1, { message: "Name is required" }),
});

export type TagCreateDto = z.infer<typeof TagCreateDtoSchema>;

/**
 * Tag rename dto schema
 */
export const TagRenameDtoSchema = z.object({
  id: z.string().uuid().min(1, {message: "ID is required"}),
  newName: z.string().min(1, { message: "New name is required"})
})

export type TagRenameDto = z.infer<typeof TagRenameDtoSchema>;

/**
 * Tag response schema
 */
export const TagResponseSchema = z.object({
  message: z.string(),
  tagId: z.string().uuid().nullable(),
  tags: z.array(TagArraySchema).nullable(),
})

export type TagResponse = z.infer<typeof TagResponseSchema>;




/**
 * Tag relation response schema
 */
export const TagRelationSchema = z.object({
  resourceId: z.string().uuid(),
  tagId: z.string().uuid(),
  isApproved: z.boolean(),
  approvedOn: z.string().nullable(),
  approvedBy: z.string().uuid().nullable(),
  tag: TagSchema.nullable()
})

export type TagRelation = z.infer<typeof TagRelationSchema>;

export const TagRelationArraySchema = z.array(TagRelationSchema);