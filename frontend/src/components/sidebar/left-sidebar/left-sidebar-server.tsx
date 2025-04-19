import React from "react";
import LeftSidebarClient from "./left-sidebar-client";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagArraySchema } from "@/types/tag.type";
import { z } from "zod";

export default async function LeftSidebarServer() {
  const tagsResult = await FetchWithValidation(TagArraySchema, "http://backend:8080/tags/all-tags");

  if (!tagsResult.success) {
    console.log("Failed to fetch tags");
    console.log(tagsResult);
    return <h1>ERROR</h1>;
  }

  const userEmail = await FetchWithValidation(z.object({ email: z.string() }), "http://backend:8080/auth/ping");

  if (!userEmail.success) {
    console.log("Failed to fetch user email");
    console.log(userEmail);
    return <h1>ERROR</h1>;
  }

  const userRole = await FetchWithValidation(z.object({ role: z.string(), isAuthenticated: z.boolean() }), "http://backend:8080/roles/current");

  if (!userRole.success) {
    console.log("Failed to fetch user role");
    console.log(userRole);
    return <h1>ERROR</h1>;
  }

  return <LeftSidebarClient tags={tagsResult.data} userEmail={userEmail.data.email} userRole={userRole.data.role} />;
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


