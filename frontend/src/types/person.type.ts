import { z } from "zod";

// Zod schema for the Person model
export const PersonSchema = z.object(
{
    id: z.string().uuid("ID must be a valid UUID"),
    name: z.string().min(1, "Name is required"),
    occupation: z.string().min(1, "Occupation is required"),
    description: z.string().nullable().optional(),
    emailAddress: z.string().email("Invalid email address").nullable().optional(),
    linkedin: z.string().url("Invalid LinkedIn URL").nullable().optional()
});

// Type inference
export type Person = z.infer<typeof PersonSchema>;

// Array
export const PersonArraySchema = z.array(PersonSchema);

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


