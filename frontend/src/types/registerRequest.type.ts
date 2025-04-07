import { z } from "zod";

/**
 * Register request schema
 */
export const RegisterRequestSchema = z.object({
    // firstname: z.string().min(1 , { message: "First name is required" }),
    // lastname: z.string().min(1 , { message: "Last name is required" }),
    email: z.string().min(1 , { message: "Email is required" }),
    password: z.string().min(1 , { message: "Password is required" }),
    token: z.string().min(1 , { message: "Token is required "}),
});

// Type definitions derived from the schemas
export type RegisterRequest = z.infer<typeof RegisterRequestSchema>;
