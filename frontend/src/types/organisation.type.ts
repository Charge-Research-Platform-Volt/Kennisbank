import { z } from "zod";

// Zod schema for the Organisation model
export const OrganisationSchema = z.object(
{
    id: z.string().uuid("ID must be a valid UUID"),
    name: z.string().min(1, "Name is required"),
    description: z.string().optional(),
    website: z.string().optional(),
    emailAddress: z.string().optional(),
});

// Type inference
export type Organisation = z.infer<typeof OrganisationSchema>;

// Array
export const OrganisationArraySchema = z.array(OrganisationSchema);