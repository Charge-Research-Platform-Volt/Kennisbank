import { z } from "zod";

/**
 * Invite request schema
 */
export const InviteRequestSchema = z.object({
    email: z.string().email("Please enter a valid email address"),
});

// Type definitions derived from the schema
export type InviteRequest = z.infer<typeof InviteRequestSchema>;
