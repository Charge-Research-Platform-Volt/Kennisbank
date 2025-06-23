import { z } from "zod";

// Zod schema for the ResourceType model
export const ResourceTypeSchema = z.object(
{
    id: z.string().uuid("ID must be a valid UUID"),
    name: z.string().min(1, "Name is required"),
});

// Type inference
export type ResourceType = z.infer<typeof ResourceTypeSchema>;

// Array
export const ResourceTypeArraySchema = z.array(ResourceTypeSchema);

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


