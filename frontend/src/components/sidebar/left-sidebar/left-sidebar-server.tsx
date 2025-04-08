import React from "react";
import LeftSidebarClient from "./left-sidebar-client";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema, UserTagsArraySchema } from "@/types/tag.type";

export default async function LeftSidebarServer() {
  const standardizedTagsResult = await FetchWithValidation(TagsArraySchema, "http://backend:8080/Tag/all-tags");
  const userTagsResult = await FetchWithValidation(UserTagsArraySchema, "http://backend:8080/UserTag/all-tags");

  if (!standardizedTagsResult.success) {
    console.log("Failed to fetch tags");
    console.log(standardizedTagsResult);
    return <h1>ERROR</h1>;
  }

  if(!userTagsResult.success) {
    console.log("Failed to fetch user tags");
    console.log(userTagsResult);
    return <h1>ERROR</h1>;
  }

  return <LeftSidebarClient userTags={userTagsResult.data} standardizedTags={standardizedTagsResult.data} />;
}
