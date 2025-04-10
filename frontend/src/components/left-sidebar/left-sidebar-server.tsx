import React from "react";
import LeftSidebarClient from "./left-sidebar-client";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagArraySchema } from "@/types/tag.type";

export default async function LeftSidebarServer() {
  const tagsResult = await FetchWithValidation(TagArraySchema, "http://backend:8080/tags/all-tags");

  if (!tagsResult.success) {
    console.log("Failed to fetch tags");
    console.log(tagsResult);
    return <h1>ERROR</h1>;
  }

  return <LeftSidebarClient tags={tagsResult.data} />;
}
