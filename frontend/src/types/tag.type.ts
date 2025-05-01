import { z } from "zod";

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
  usageCount: z.number().int().min(0).optional(),
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
  tags: z.array(TagSchema).nullable(),
})

export type TagResponse = z.infer<typeof TagResponseSchema>;

/**
 * Tag page response schema
 */
export type TagPageResponse = {
  success: boolean;
  message: string;
  pageIndex?: number;
  pageSize?: number;
  pageCount?: number;
  tags?: TagArray;
}

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


/**
 * This is used to fetch tags in the backend with filter options
 */
export const TagFilterOptionsSchema = z.object({
  usePaging: z.boolean().optional(),
  pageIndex: z.number().int().min(1).optional(),
  pageSize: z.number().int().min(1).optional(),
  searchQuery: z.string().optional(),
  createdBy: z.string().uuid().optional(),
  onlyOwnedByCurrentUser: z.boolean().optional(),
  isApproved: z.boolean().optional(),
  isStandardized: z.boolean().optional(),
  createdFromDate: z.date().optional(),
  createdToDate: z.date().optional(),
  approvedFromDate: z.date().optional(),
  approvedToDate: z.date().optional(),
  includeUsageCount: z.boolean().optional(),
  sortBy: z.string().optional(),
  sortDescending: z.boolean().optional(),
  weightedSort: z.string().optional(),
})

export type TagFilterOptions = z.infer<typeof TagFilterOptionsSchema>;

export type TagRelation = z.infer<typeof TagRelationSchema>;

export const TagRelationArraySchema = z.array(TagRelationSchema);

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


