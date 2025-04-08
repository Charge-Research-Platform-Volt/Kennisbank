import { z } from "zod";

/**
 * Tag scheme
 */
export const TagSchema = z.object({
  id: z.string().uuid(),
  name: z.string().min(1, { message: "Name is required" }),
  isStandardized: z.boolean(),
  isApproved: z.boolean(),
  approvedOn: z.date().nullable(),
  approvedBy: z.string().uuid().nullable(),
  createdBy: z.string().min(1, { message: "CreatedBy is required" }),
  createdOn: z.date(),
});

export const TagsArraySchema = z.array(TagSchema);

// Type definitions derived from the schemas
export type Tag = z.infer<typeof TagSchema>;

export type TagsArray = z.infer<typeof TagsArraySchema>;

/**
 * Tag create dto scheme
 */
export const TagCreateDtoSchema = z.object({
  name: z.string().min(1, { message: "Name is required" }),
  isApproved: z.boolean().nullable(),
  approvedBy: z.string().uuid().nullable(),
  createdBy: z.string().min(1, { message: "Created by is required!" })
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
 * Splits tags into standardized and non-standardized arrays
 */
export const splitTagsByStandardization = (tags: TagsArray) => {
  return tags.reduce(
    (acc, tag) => {
      if (tag.isStandardized) {
        acc.standardized.push(tag);
      } else {
        acc.nonStandardized.push(tag);
      }
      return acc;
    },
    {
      standardized: [] as Tag[],
      nonStandardized: [] as Tag[],
    }
  );
};