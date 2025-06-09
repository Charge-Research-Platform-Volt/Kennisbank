import { z } from "zod";

// Define the ApiResponse schema
const ApiResponseSchema = z.object({
  success: z.boolean(),
  message: z.string(),
  body: z.any().nullable().optional(),
});

// Type inference - this creates a TypeScript type from the schema
type ApiResponse = z.infer<typeof ApiResponseSchema>;

export { ApiResponseSchema };
export type { ApiResponse };

export type NewApiResponse<T> = ApiSuccessResponse<T> | ApiErrorResponse;

export interface ApiSuccessResponse<T> {
  success: true;
  message: string;
  body: T;
}

export interface ApiErrorResponse {
  success: false;
  message: string;
}
