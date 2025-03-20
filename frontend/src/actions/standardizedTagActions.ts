"use server";

import type { FormResponse } from "@/types/return.type";
import { Tag, TagBase, TagBaseSchema, TagSchema } from "@/types/tag.type";
import { revalidatePath } from "next/cache";
import { cookies } from "next/headers";

export const AddStandardizedTag = async (prevState: FormResponse<TagBase>, formData: FormData): Promise<FormResponse<TagBase>> => {
  const rawData: TagBase = {
    name: formData.get("name") as string,
  };

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
  const cookieHeader = cookies();
  const response = await fetch(`http://backend:8080/Tag/add-tag/${encodeURIComponent(rawData.name)}`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",	
  });
  const data = await response.json();

  // Check if the request was successful, if not, return an error
  if (!response.ok) {
    return {
      success: false,
      message: data.message,
    };
  }

  // Revalidate the cache for the standardizedtags page
  revalidatePath("/standardizedtags", "layout");
  return {
    success: true,
    message: data.message,
  };
};

export const DeleteStandardizedTag = async (tag: Tag): Promise<FormResponse<Tag>> => {
  console.log("Deleting tag: ", tag.id);

  const rawData: Tag = {
    id: tag.id,
    name: tag.name,
  };

  // Validate the raw data, if it fails, return an error
  const validatedData = TagSchema.safeParse(rawData);

  if (!validatedData.success) {
    return {
      success: false,
      message: validatedData.error.errors[0].message,
      inputs: rawData,
    };
  }

  // Send the data to the backend
  const cookieHeader = cookies();
  const response = await fetch(`http://backend:8080/Tag/delete-tag/${encodeURIComponent(tag.id)}`, {
    method: "DELETE",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
  });
  const data = await response.json();

  // Check if the request was successful, if not, return an error
  if (!response.ok) {
    return {
      success: false,
      message: data.message,
    };
  }

  // Revalidate the cache for the standardizedtags page
  revalidatePath("/standardizedtags", "layout");
  return {
    success: true,
    message: data.message,
  };
};

export const SaveStandardizedTag = async (prevState: FormResponse<Tag>, formData: FormData): Promise<FormResponse<Tag>> => {
  const rawData: Tag = {
    name: formData.get("name") as string,
    id: formData.get("id") as string,
  };

  // Validate the raw data, if it fails, return an error
  const validatedData = TagSchema.safeParse(rawData);

  if (!validatedData.success) {
    return {
      success: false,
      message: validatedData.error.errors[0].message,
      inputs: rawData,
    };
  }

  // Send the data to the backend
  const cookieHeader = cookies();
  const response = await fetch(`http://backend:8080/Tag/change-tag-name/${encodeURIComponent(rawData.id)}/${encodeURIComponent(rawData.name)}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
  });
  const data = await response.json();

  // Check if the request was successful, if not, return an error
  if (!response.ok) {
    return {
      success: false,
      message: data.message,
    };
  }

  // Revalidate the cache for the standardizedtags page
  revalidatePath("/standardizedtags", "layout");
  return {
    success: true,
    message: data.message,
  };
};
