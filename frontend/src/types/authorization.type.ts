import { z } from 'zod';

// Schema for the expected response
export const RoleResponseSchema = z.object({
  role: z.string(),
  isAuthenticated: z.boolean()
});

// Type for RoleResponse, derived from the schema
export type RoleResponse = z.infer<typeof RoleResponseSchema>;