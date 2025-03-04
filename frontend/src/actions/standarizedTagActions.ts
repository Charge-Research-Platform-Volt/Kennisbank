"use server";

import type { FormResponse } from "@/types/return.type";
import { Tag, TagBase, TagBaseSchema } from "@/types/tag.type";
import { revalidatePath } from "next/cache";

export const AddStandarizedTag = async (
    prevState: FormResponse<Tag>,
    formData: FormData,
): Promise<FormResponse<TagBase>> => {
    const rawData: TagBase = {
        name: formData.get("name") as string,
    }
    
    // Validate the raw data, if it fails, return an error
    const validatedData = TagBaseSchema.safeParse(rawData);

    if (!validatedData.success) {
        return {
            success: false,
            message: validatedData.error.errors[0].message,
            inputs: rawData,
        };
    }

    // Send the data to the backend
    const response = await fetch(
        `http://backend:8080/Tag/add-tag/${encodeURIComponent(rawData.name)}`,
        {
            method: "POST",
            headers: { "Content-Type": "application/json" },
        },
    );
    const data = await response.json();

    // Check if the request was successful, if not, return an error
    if (!response.ok) {
        return {
            success: false,
            message: data.message,
        };
    }

    // Revalidate the cache for the standarizedtags page
    revalidatePath("/standarizedtags");
    return {
        success: true,
        message: data.message,
    };
};

export const DeleteStandarizedTag = async (id: string): Promise<FormResponse<TagBase>> => {
    console.log("Deleting tag: ", id);
    
    const rawData: TagBase = {
        name: id,
    }

    // Validate the raw data, if it fails, return an error
    const validatedData = TagBaseSchema.safeParse(rawData);

    if (!validatedData.success) {
        return {
            success: false,
            message: validatedData.error.errors[0].message,
            inputs: rawData,
        };
    }

    // Send the data to the backend
    const response = await fetch(
        `http://backend:8080/Tag/delete-tag/${encodeURIComponent(id)}`,
        {
            method: "DELETE",
            headers: { "Content-Type": "application/json" },
        },
    );
    const data = await response.json();

    // Check if the request was successful, if not, return an error
    if (!response.ok) {
        return {
            success: false,
            message: data.message
        }
    }

    // Revalidate the cache for the standarizedtags page
    revalidatePath("/standarizedtags");
    return {
        success: true,
        message: data.message,
    };
};
