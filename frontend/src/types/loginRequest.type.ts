import { z } from "zod";

/**
 * Register request schema
 */
export const LoginRequestSchema = z.object({
    email: z.string().min(1 , { message: "Email is required" }),
    password: z.string().min(1 , { message: "Password is required" }),
});

// Type definitions derived from the schemas
export type LoginRequest = z.infer<typeof LoginRequestSchema>;
