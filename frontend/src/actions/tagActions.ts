"use server";

import type { FormResponse } from "@/types/return.type";
import { TagCreateDto, TagCreateDtoSchema, TagRenameDto } from "@/types/tag.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

export const AddStandardizedTag = async (prevState: FormResponse<TagCreateDto>, formData: FormData): Promise<FormResponse<TagCreateDto>> => {
  const rawData: TagCreateDto = {
    name: formData.get("name") as string,
  };

  // Validate the raw data, if it fails, return an error
  const validatedData = TagCreateDtoSchema.safeParse(rawData);

  if (!validatedData.success) {
    return {
      success: false,
      message: validatedData.error.errors[0].message,
      inputs: rawData,
    };
  }

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response : Response = await fetch(`http://backend:8080/tags/add-standard-tag`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
    body: JSON.stringify(rawData),
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
  revalidatePath("/tags", "layout");
  return {
    success: true,
    message: data.message,
  };
};

export const AddUserTag = async (prevState: FormResponse<TagCreateDto>, formData: FormData): Promise<FormResponse<TagCreateDto>> => {
  console.log("Adding user tag");

  const rawData: TagCreateDto = {
    name: formData.get("name") as string,
  };

  // Validate the raw data, if it fails, return an error
  const validatedData = TagCreateDtoSchema.safeParse(rawData);

  if (!validatedData.success) {
    return {
      success: false,
      message: validatedData.error.errors[0].message,
      inputs: rawData,
    };
  }

  console.log("Input validated");

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response : Response = await fetch(`http://backend:8080/tags/add-user-tag`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
    body: JSON.stringify(rawData),
  });

  const data = await response.json();

  // Check if the request was successful, if not, return an error
  if (!response.ok) {
    console.log("Error in response: ", data.message);
    return {
      success: false,
      message: data.message,
    };
  }
  
  console.log("Tag created");

  // Revalidate the cache for the standardizedtags page
  revalidatePath("/tags", "layout");
  return {
    success: true,
    message: data.message,
  };
};

export const DeleteTag = async (tagId: string): Promise<FormResponse<{id: string}>> => {
  console.log("Deleting tag: ", tagId);

  // Send the data to the backend
  const cookieHeader = await cookies();
  const response = await fetch(`http://backend:8080/tags/delete-tag/${encodeURIComponent(tagId)}`, {
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
  revalidatePath("/tags", "layout");
  return {
    success: true,
    message: data.message,
  };
};

export const RenameTag = async (prevState: FormResponse<TagRenameDto>, formData: FormData): Promise<FormResponse<TagRenameDto>> => {
  if(formData.get("name") == formData.get("originalTagName")) {
    return {
      success: true,
      message: "Tag name changed.",
    };
  }

  const rawData: TagRenameDto = {
    newName: formData.get("name") as string,
    id: formData.get("id") as string,
  };

  console.log("Renaming tag: ", rawData.id, " to ", rawData.newName);

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response = await fetch(`http://backend:8080/tags/rename-tag/${encodeURIComponent(rawData.id)}/${encodeURIComponent(rawData.newName)}`, {
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
  revalidatePath("/tags", "layout");
  return {
    success: true,
    message: data.message,
  };
};

export const ApproveTag = async (tagId: string): Promise<FormResponse<{id: string}>> => {
  console.log("Approving user tag: ", tagId);

  const cookieHeader = await cookies();
  const response = await fetch(
    `http://backend:8080/tags/approve-tag/${encodeURIComponent(tagId)}`,
    {
      method: "PATCH",
      headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
      credentials: "include",
    }
  );

  const data = await response.json();

  if (!response.ok) {
    return {
      success: false,
      message: data.message,
    };
  }

  revalidatePath("/tags");

  return {
    success: true,
    message: data.message,
  };
};

export const MakeStandardized = async (tagId: string): Promise<FormResponse<{ id: string }>> => {
  console.log("Making tag standardized: ", tagId);

  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response = await fetch(
    `http://backend:8080/tags/make-standardized/${encodeURIComponent(tagId)}`,
    {
      method: "PATCH",
      headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
      credentials: "include",
    }
  );

  const data = await response.json();

  if (!response.ok) {
    return {
      success: false,
      message: data.message,
    };
  }

  revalidatePath("/tags");

  return {
    success: true,
    message: data.message,
  };
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


