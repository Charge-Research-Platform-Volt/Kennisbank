"use server";

import type { FormResponse, ReturnType } from "@/types/return.type";
import { Tag } from "@/types/tag.type";
import { UUID } from "crypto";
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
    id: string,
): Promise<ReturnType> => {
    console.log("Deleting tag: ", id);
    if (!id) {return { success: false, message: "Id is required" }}

    console.log("Deleting tag: ", id);

    const response = await fetch(`http://backend:8080/Tag/delete-tag/${encodeURIComponent(id)}`, {
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
