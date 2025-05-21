import { z } from "zod";

/**
 * Base project schema without ID
 */
export const ProjectBaseSchema = z.object({
  title: z.string().min(1, { message: "Title is required" }),
  description: z.string().nullable(),
  creationDate: z.string().min(1, { message: "Created at is required" }),
  deletionDate: z.string().optional(),
  projectType: z.string().max(10).refine(val => val === "root" || val === "folder", {
    message: "Project type must be either 'root' or 'folder'"
  })
});

/**
 * Complete project schema with ID that extends the base project
 */
export const ProjectSchema = ProjectBaseSchema.extend({
  id: z.string().uuid(), // UUID validation
});

export const ProjectArraySchema = z.array(ProjectSchema);

/**
 * Project creation DTO schema
 */
export const ProjectCreateDtoSchema = z.object({
  title: z.string().min(1, { message: "Title is required" }),
  description: z.string().nullable(),
  creationDate: z.string().optional(),
  deletionDate: z.string().optional(),
  projectType: z.string().refine(val => val === "root" || val === "folder", {
    message: "Project type must be either 'root' or 'folder'"
  }),
  tags: z.array(z.string()),
  creators: z.array(z.string())
});

/**
 * Project filtering schema
 */
export const FilterProjectDtoSchema = z.object({
  // Pagination
  usePaging: z.boolean().default(false),
  pageIndex: z.number().int().min(1).default(1),
  pageSize: z.number().int().min(1).default(100),
  
  // Filtering
  searchQuery: z.string().nullable().optional(),
  createdBy: z.string().nullable().optional(),
  startDate: z.date().nullable().optional(),
  endDate: z.date().nullable().optional(),
  tags: z.array(z.string().uuid()).nullable().optional()
});

/**
 * Project page response schema
 */
export const ProjectPageResponseSchema = z.object({
  projects: ProjectArraySchema,
  pageIndex: z.number().int().min(0).nullable().optional(),
  pageSize: z.number().int().min(0).nullable().optional(),
  pageCount: z.number().int().min(0).nullable().optional()
});

/**
 * Resource reference for ProjectInfoDto
 */
export const ResourceReferenceSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  // Include other essential Resource properties needed in this context
}).nullable();

export const ResourceReferenceArraySchema = z.array(ResourceReferenceSchema);

/**
 * Project Info DTO schema
 */
export const ProjectInfoDtoSchema = z.object({
  project: ProjectSchema.nullable().optional(),
  folders: z.array(ProjectSchema.nullable()),
  resources: ResourceReferenceArraySchema
});

/**
 * Project relation schemas
 */
export const ProjectTagRelationSchema = z.object({
  id: z.string().uuid(),
  projectId: z.string().uuid(),
  tagId: z.string().uuid()
});

export const ProjectCreatorRelationSchema = z.object({
  id: z.string().uuid(),
  projectId: z.string().uuid(),
  creatorId: z.string().uuid()
});

export const ProjectResourceRelationSchema = z.object({
  id: z.string().uuid(),
  projectId: z.string().uuid(),
  resourceId: z.string().uuid()
});

export const ProjectFolderRelationSchema = z.object({
  id: z.string().uuid(),
  parentFolderId: z.string().uuid(),
  childFolderId: z.string().uuid()
});

// Type definitions derived from the schemas
export type ProjectBase = z.infer<typeof ProjectBaseSchema>;
export type Project = z.infer<typeof ProjectSchema>;
export type ProjectCreateDto = z.infer<typeof ProjectCreateDtoSchema>;
export type FilterProjectDto = z.infer<typeof FilterProjectDtoSchema>;
export type ProjectPageResponse = z.infer<typeof ProjectPageResponseSchema>;
export type ProjectInfoDto = z.infer<typeof ProjectInfoDtoSchema>;
export type ProjectTagRelation = z.infer<typeof ProjectTagRelationSchema>;
export type ProjectCreatorRelation = z.infer<typeof ProjectCreatorRelationSchema>;
export type ProjectResourceRelation = z.infer<typeof ProjectResourceRelationSchema>;
export type ProjectFolderRelation = z.infer<typeof ProjectFolderRelationSchema>;

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
