"use server";

import type { FormResponse } from "@/types/return.type";
import { UserTag, UserTagBase, UserTagBaseSchema, UserTagSchema } from "@/types/tag.type";
import { revalidatePath } from "next/cache";

// Add a new User Tag
export const AddUserTag = async (
    prevState: FormResponse<UserTagBase>,
    formData: FormData,
): Promise<FormResponse<UserTagBase>> => {
    const rawData: UserTagBase = {
        name: formData.get("name") as string,
    }

    const validatedData = UserTagBaseSchema.safeParse(rawData);

    if (!validatedData.success) {
        return {
            success: false,
            message: validatedData.error.errors[0].message,
            inputs: rawData,
        };
    }

    // Send the data to the backend
    const response = await fetch(
        `http://backend:8080/UserTag/add-usertag/${encodeURIComponent(rawData.name)}`,
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

    // Revalidate the cache for the usertags page
    revalidatePath("/usertags");
    return {
        success: true,
        message: data.message,
    };
};

// Delete a User Tag
export const DeleteUserTag = async (tag: UserTag): Promise<FormResponse<UserTag>> => {
    console.log("Deleting user tag: ", tag.id);

    const rawData: UserTag = {
        id: tag.id,
        name: tag.name,
        isApproved: tag.isApproved,
        user: tag.user,
    }

    // Validate the raw data, if it fails, return an error
    const validatedData = UserTagSchema.safeParse(rawData);

    if (!validatedData.success) {
        return {
            success: false,
            message: validatedData.error.errors[0].message,
            inputs: tag,
        };
    }

    // Send the data to the backend
    const response = await fetch(
        `http://backend:8080/UserTag/delete-usertag/${encodeURIComponent(tag.id)}`,
        {
            method: "DELETE",
            headers: { "Content-Type": "application/json" },
        },
    );
    const data = await response.json();

    // Check if the request was succesful, if not, return an error
    if (!response.ok) {
        return {
            success: false,
            message: data.message,
        };
    }

    // Revalidate the cache for the usertags page
    revalidatePath("/usertags");
    return {
        success: true,
        message: data.message,
    };
};

export const SaveUserTag = async (
    prevState : FormResponse<UserTag>,
    formData: FormData,
): Promise<FormResponse<UserTag>> => {
    const rawData: UserTag = {
        name: formData.get("name") as string,
        id: formData.get("id") as string,
        isApproved: false,
        user: {
            id: formData.get("userId") as string, // TODO: should come from auth later
            name: formData.get("username") as string,
        },
    };

    // Validate the raw data, if it fails, return an error
    const validatedData = UserTagSchema.safeParse(rawData);

    if (!validatedData.success) {
        return {
            success: false,
            message: validatedData.error.errors[0].message,
            inputs: rawData,
        };
    }

    // Send the data to the backend
    const response = await fetch(
        `http://backend:8080/UserTag/change-usertag-name/${encodeURIComponent(rawData.id)}`,
        {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(rawData),
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

    // Revalidate the cache for the standardizedtags page
    revalidatePath("/usertags");
    return {
        success: true,
        message: data.message,
    };
}

// Approve a User Tag (only admins can do this)
export const ApproveUserTag = async (tagId: string): Promise<FormResponse<{ id: string }>> => {
    console.log("Approving user tag: ", tagId);

    const response = await fetch(
        `http://backend:8080/UserTag/approve-usertag/${encodeURIComponent(tagId)}`,
        {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
        },
    );
    const data = await response.json();

    if (!response.ok) {
        return {
            success: false,
            message: data.message,
        };
    }

    revalidatePath("/usertags");
    return {
        success: true,
        message: data.message,
    };
};
