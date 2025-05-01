"use server";

import type { FormResponse } from "@/types/return.type";
import { TagArraySchema, TagCreateDto, TagCreateDtoSchema, TagFilterOptions, TagPageResponse, TagRenameDto } from "@/types/tag.type";
import { revalidatePath } from "next/cache";
import { ReadonlyRequestCookies } from "next/dist/server/web/spec-extension/adapters/request-cookies";
import { cookies } from "next/headers";

export const AddStandardizedTag = async (prevState: FormResponse<TagCreateDto>, formData: FormData): Promise<FormResponse<TagCreateDto>> => {
  const rawData: TagCreateDto = {
    name: formData.get("name") as string,
  };

  if(rawData.name.length > 50){
    return{
      success: false,
      message: "Tag is too long",
      inputs: rawData,
    }
  }

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

  if(rawData.name.length > 50){
    return{
      success: false,
      message: "Tag is too long",
      inputs: rawData,
    }
  }

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

export const MergeTag = async (formData: FormData): Promise<FormResponse<{ tagId1: string; tagId2: string }>> => {
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
    `http://backend:8080/tags/merge-tag/${encodeURIComponent(tagId1)}/${encodeURIComponent(tagId2)}`,
    {
      method: "PATCH",
      headers: {
        "Content-Type": "application/json",
        Cookie: cookieHeader.toString() || "",
      },
      credentials: "include",
    }
  );

  const data = await response.json();

  if (!response.ok) {
    return {
      success: false,
      message: data.message || "Failed to merge tags.",
    };
  }

  revalidatePath("/tags", "layout");

  return {
    success: true,
    message: data.message || "Tags merged successfully.",
  };
};

/**
 * 
 * @param pageIndex - The page to fetch
 * @param query - Query to filter the tags with
 * @returns - A promise with the tags and the page information
 */
export const ListTagsPaged = async (pageIndex: number, searchQuery: string): Promise<TagPageResponse> => {
  console.log("Getting tags paged:");
  
  const tagFilterOptions : TagFilterOptions = {
    usePaging: true,
    pageIndex: pageIndex,
    pageSize: 50,
    searchQuery: searchQuery,
    onlyOwnedByCurrentUser: false,
    includeUsageCount: true,
    sortDescending: false,
  }

  // Send the data to the backend
  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response : Response = await fetch(
      `http://backend:8080/Tags/tags`,
      {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
          body: JSON.stringify(tagFilterOptions),
      },
  );
  //parse the data from the response
  const data = await response.json();

  // Check if the request was succesful, if not, return an error
  if (!response.ok) {
      return {
          success: false,
          message: data.message,
      };
  }

  //validate the data
  if(!data){
      return {
          success: false,
          message: "No data found",
      };
  }

  //validate the tags array
  const validatedTags = TagArraySchema.safeParse(data.tags);
  if (!validatedTags.success) {
      return {
          success: false,
          message: validatedTags.error.errors[0].message,
      };
  }

  revalidatePath("/tags");
  
  // Check if the request was succesful, if not, return an error
  return {
      success: true,
      message: "Tags fetched successfully",
      tags: validatedTags.data,
      pageIndex: data.pageIndex,
      pageSize: data.pageSize,
      pageCount: data.pageCount,
  }
}

/**
 * 
 * @param searchQuery - The name of the tag it tries to search
 * @param K  - Max amount of tags to return 
 * @returns A maximum of K tags that correspond with the query
 */
export const fetchTagSearch = async (searchQuery?: string, K?: number): Promise<TagPageResponse> => {
  searchQuery = searchQuery?.trim();

  const tagFilterOptions : TagFilterOptions = {
    usePaging: true,
    pageIndex: 1,
    pageSize: K,
    searchQuery: searchQuery,
    onlyOwnedByCurrentUser: false,
    includeUsageCount: true,
    sortDescending: true,
    weightedSort: "IsStandardized:2,IsApproved:1,UsageCount:0.5",
  }

  const cookieHeader : ReadonlyRequestCookies = await cookies();
  const response : Response = await fetch(
      `http://backend:8080/Tags/tags`,
      {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json", Cookie: cookieHeader.toString() || "" },
          body: JSON.stringify(tagFilterOptions),
      },
  );

  if (!response.ok) {
    console.log("problem with finding tags");
    return {
      success: false,
      message: "problem with fetching tags",
    }
  }

  const data = await response.json();
  //validate the data
  if(!data){
    return {
        success: false,
        message: "No data found",
    };
  }

  //validate the tags array
  const validatedTags = TagArraySchema.safeParse(data.tags);
  if (!validatedTags.success) {
      return {
          success: false,
          message: validatedTags.error.errors[0].message,
      };
  }

  revalidatePath("/tags");

  // Check if the request was succesful, if not, return an error
  return {
      success: true,
      message: "Tags fetched successfully",
      tags: validatedTags.data,
      pageIndex: data.pageIndex,
      pageSize: data.pageSize,
      pageCount: data.pageCount,
  }
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


