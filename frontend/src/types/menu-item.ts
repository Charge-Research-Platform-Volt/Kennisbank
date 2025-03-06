import { z } from "zod";

export const MenuItemSchema = z.object({
    name: z.string().min(1 , { message: "Name is required" }),
    icon: z.string().min(1 , { message: "icon is required" }),
    path: z.string().min(1 , { message: "path is required" }),
});


// Type definitions derived from the schemas
export type MenuItem = z.infer<typeof MenuItemSchema>;
