"use server";

import { ApiResponse, ApiResponseSchema } from "@/types/apiResponse.type";
import type { FormResponse } from "@/types/return.type";
import { TagCreateDto, TagFilterOptions, TagRenameDto } from "@/types/tag.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

export const AddStandardizedTag = async (prevState: FormResponse<TagCreateDto>, formData: FormData): Promise<ApiResponse> => {
  const rawFormData: TagCreateDto = {
    name: formData.get("name") as string,
  };

  if(rawFormData.name.length > 50){
    return{
      success: false,
      message: "Tag is too long",
    }
  }

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response : Response = await fetch(`${process.env.API_URL}/tags/add-standard-tag`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
    body: JSON.stringify(rawFormData),
  });
  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);


  // Revalidate the cache for the standardizedtags page
  revalidatePath("/tags", "layout");
  return data
};

export const AddUserTag = async (prevState: FormResponse<TagCreateDto>, formData: FormData): Promise<ApiResponse> => {
  console.log("Adding user tag");

  const rawFormData: TagCreateDto = {
    name: formData.get("name") as string,
  };

  if(rawFormData.name.length > 50){
    return{
      success: false,
      message: "Tag is too long",
    }
  }

  console.log("Input validated");

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response : Response = await fetch(`${process.env.API_URL}/tags/add-user-tag`, {
    method: "PUT",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
    body: JSON.stringify(rawFormData),
  });

  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);
  
  console.log("Tag created");

  // Revalidate the cache for the standardizedtags page
  revalidatePath("/tags", "layout");
  return data
};

export const DeleteTag = async (tagId: string): Promise<ApiResponse> => {
  console.log("Deleting tag: ", tagId);

  // Send the data to the backend
  const cookieHeader = await cookies();
  const response = await fetch(`${process.env.API_URL}/tags/delete-tag/${encodeURIComponent(tagId)}`, {
    method: "DELETE",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
  });
  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);

  // Revalidate the cache for the standardizedtags page
  revalidatePath("/tags", "layout");
  return data
};

export const RenameTag = async (prevState: FormResponse<TagRenameDto>, formData: FormData): Promise<ApiResponse> => {
  if(formData.get("name") == formData.get("originalTagName")) {
    return {
      success: true,
      message: "Tag name changed.",
    };
  }

  const rawFormData: TagRenameDto = {
    newName: formData.get("name") as string,
    id: formData.get("id") as string,
  };

  console.log("Renaming tag: ", rawFormData.id, " to ", rawFormData.newName);

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response = await fetch(`${process.env.API_URL}/tags/rename-tag/${encodeURIComponent(rawFormData.id)}/${encodeURIComponent(rawFormData.newName)}`, {
    method: "PATCH",
    headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
    credentials: "include",
  });

  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);

  // Revalidate the cache for the standardizedtags page
  revalidatePath("/tags", "layout");
  return data
};

export const ApproveTag = async (tagId: string): Promise<ApiResponse> => {
  console.log("Approving user tag: ", tagId);

  const cookieHeader = await cookies();
  const response = await fetch(
    `${process.env.API_URL}/tags/approve-tag/${encodeURIComponent(tagId)}`,
    {
      method: "PATCH",
      headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
      credentials: "include",
    }
  );

  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);

  revalidatePath("/tags");

  return data
};

export const MakeStandardized = async (tagId: string): Promise<ApiResponse> => {
  console.log("Making tag standardized: ", tagId);

  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response = await fetch(
    `${process.env.API_URL}/tags/make-standardized/${encodeURIComponent(tagId)}`,
    {
      method: "PATCH",
      headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
      credentials: "include",
    }
  );

  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);

  revalidatePath("/tags");

  return data
};

export const MergeTag = async (formData: FormData): Promise<ApiResponse> => {
  const tagId1 = formData.get("tagId1") as string;
  const tagId2 = formData.get("tagId2") as string;


  if (!tagId1 || !tagId2 || tagId1 === tagId2) {
    return {
      success: false,
      message: "Invalid source or target tag ID.",
    };
  }

  console.log("Merging tag", tagId2, "into", tagId1);

  const cookieHeader = await cookies();
  const response = await fetch(
    `${process.env.API_URL}/tags/merge/${encodeURIComponent(tagId1)}/${encodeURIComponent(tagId2)}`,
    {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Cookie: cookieHeader.toString() || "",
      },
      credentials: "include",
    }
  );

  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);

  revalidatePath("/tags", "layout");
  return data
};

/**
 * 
 * @param pageIndex - The page to fetch
 * @param query - Query to filter the tags with
 * @returns - A promise with the tags and the page information
 */
export const ListTagsPaged = async (pageIndex: number, searchQuery: string): Promise<ApiResponse> => {
  console.log("Getting tags paged:");
  
  const tagFilterOptions : TagFilterOptions = {
    usePaging: true,
    pageIndex: pageIndex,
    pageSize: 50,
    searchQuery: searchQuery,
    onlyOwnedByCurrentUser: false,
    includeUsageCount: true,
    includeCanEditAndDelete: true,
    sortDescending: false,
  }

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response = await fetch(
      `${process.env.API_URL}/tags/tags`,
      {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
          body: JSON.stringify(tagFilterOptions),
      },
  );

  //parse the data from the response
  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);

  // Check if the request was succesful, if not, return an error
  if (!response.ok) {
    throw new Error("Problem with finding tags");
  }

  revalidatePath("/tags");
  // Check if the request was succesful, if not, return an error
  return data
}

/**
 * 
 * @param searchQuery - The name of the tag it tries to search
 * @param K  - Max amount of tags to return
 * @returns A maximum of K tags that correspond with the query
 */
export const fetchTagSearch = async (searchQuery?: string, K?: number): Promise<ApiResponse> => {
  console.log("Fetching tags");
  searchQuery = searchQuery?.trim();

  const tagFilterOptions : TagFilterOptions = {
    usePaging: true,
    pageIndex: 1,
    pageSize: K,
    searchQuery: searchQuery,
    onlyOwnedByCurrentUser: false,
    includeUsageCount: true,
    includeCanEditAndDelete: true,
    sortDescending: true,
    weightedSort: "IsStandardized:2,IsApproved:1,UsageCount:0.5",
  }

  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response = await fetch(
      `${process.env.API_URL}/tags/tags`,
      {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
          body: JSON.stringify(tagFilterOptions),
      },
  );

  if (!response.ok) {
    throw new Error("Problem with finding tags");
  }

  const rawData = await response.json();
  const data = ApiResponseSchema.parse(rawData);

  revalidatePath("/tags");

  // Check if the request was succesful, if not, return an error
  return data;
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


