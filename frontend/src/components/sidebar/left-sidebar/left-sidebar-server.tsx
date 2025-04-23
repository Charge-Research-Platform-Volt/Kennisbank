import React from "react";
import LeftSidebarClient from "./left-sidebar-client";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagArraySchema } from "@/types/tag.type";
import { z } from "zod";

/**
 * @summary This function does the needed fetches from a server component and gives them to the client side component for use.
 * @returns Left side bar made from server component
 */
export default async function LeftSidebarServer() {
  const tagsResult = await FetchWithValidation(TagArraySchema, `${process.env.API_URL}/tags/all-tags`);

  if (!tagsResult.success) {
    console.log("Failed to fetch tags");
    console.log(tagsResult);
    return <h1>ERROR</h1>;
  }

  const userEmail = await FetchWithValidation(z.object({ email: z.string() }), `${process.env.API_URL}/auth/ping`);

  if (!userEmail.success) {
    console.log("Failed to fetch user email");
    console.log(userEmail);
    return <h1>ERROR</h1>;
  }

  const userRole = await FetchWithValidation(z.object({ role: z.string(), isAuthenticated: z.boolean() }), `${process.env.API_URL}/roles/current`);

  if (!userRole.success) {
    console.log("Failed to fetch user role");
    console.log(userRole);
    return <h1>ERROR</h1>;
  }

  return <LeftSidebarClient tags={tagsResult.data} userEmail={userEmail.data.email} userRole={userRole.data.role} />;
}
