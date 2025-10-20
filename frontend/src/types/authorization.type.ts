import { z } from 'zod';

// Schema for the expected response
export const RoleResponseSchema = z.object({
  role: z.string(),
  isAuthenticated: z.boolean()
});

// Type for RoleResponse, derived from the schema
export type RoleResponse = z.infer<typeof RoleResponseSchema>;

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


