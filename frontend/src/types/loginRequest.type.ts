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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


