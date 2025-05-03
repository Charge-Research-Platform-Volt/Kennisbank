import { z } from 'zod';

// Define the ApiResponse schema
const ApiResponseSchema = z.object({
  success: z.boolean(),
  message: z.string(),
  errors: z.array(z.string()).nullable().optional(),
  body: z.any().nullable().optional()
});

// Type inference - this creates a TypeScript type from the schema
type ApiResponse = z.infer<typeof ApiResponseSchema>;

export { ApiResponseSchema };
export type { ApiResponse };