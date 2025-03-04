"use server";

import type { FormResponse, ReturnType } from "@/types/return.type";
import { Tag } from "@/types/standarized-tag.type";
import { revalidatePath } from "next/cache";

export const AddStandarizedTag = async (
    prevState: FormResponse<Tag>,
    formData: FormData,
): Promise<ReturnType> => {
    const name = formData.get("name") as string;
    if (!name) {return { success: false, message: "Name is required" }}

    const response = await fetch(`http://backend:8080/Tag/add-tag/${encodeURIComponent(name)}`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
    });

    if (!response.ok) {
        return {
            success: false,
            message: "Failed to create tag",
        };
    }

    revalidatePath("/");
    return {
        success: true,
        message: "Tag created",
    };
};

export const DeleteStandarizedTag = async (
    name: string,
): Promise<ReturnType> => {
    if (!name) {return { success: false, message: "Name is required" }}

    console.log("Deleting tag: ", name);

    const response = await fetch(`http://backend:8080/Tag/delete-tag/${encodeURIComponent(name)}`, {
        method: "DELETE",
        headers: { "Content-Type": "application/json" },
    });

    if (!response.ok) {
        throw new Error("Failed to delete tag");
    }

    revalidatePath("/standarizedtags");
    return {
        success: true,
        message: "Tag deleted",
    };
};
