import React from "react";
import LeftSidebarClient from "./left-sidebar-client";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema } from "@/types/tag.type";

export default async function LeftSidebarServer() {
  const result = await FetchWithValidation(TagsArraySchema, "http://backend:8080/Tag/all-tags");

  if (!result.success) {
    console.log("Failed to fetch tags");
    console.log(result);
    return <h1>ERROR</h1>;
  }

  return <LeftSidebarClient tags={result.data} />;
}
