import { z } from "zod";

/**
 * Invite request schema
 */
export const InviteRequestSchema = z.object({
    email: z.string().email("Please enter a valid email address"),
});

// Type definitions derived from the schema
export type InviteRequest = z.infer<typeof InviteRequestSchema>;


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


