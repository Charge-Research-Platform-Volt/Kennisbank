import { z } from "zod";

// Zod schema for the Region model
export const RegionSchema = z.object(
{
    id: z.string().uuid("ID must be a valid UUID"),
    name: z.string().min(1, "Name is required"),
});

// Type inference
export type Region = z.infer<typeof RegionSchema>;

// Array
export const RegionArraySchema = z.array(RegionSchema);