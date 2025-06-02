import ProjectSearchClient from "./components/project-search-client";
import { ListProjectsPaged } from "@/actions/projectActions";
import { GetCurrentUserId } from "@/actions/userActions";
import { ApiResponse } from "@/types/apiResponse.type";
import { Project, FolderProject } from "@/types/project.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { z } from "zod";

/**
 * Displays the projects page
 * 
 * @author Jelle v.h. Schut
 * @returns projects page
 */
export default async function ProjectsPage() {
  const projectFetch: ApiResponse = await ListProjectsPaged(1, "");

  if (!projectFetch.success) {
    throw new Error(projectFetch.message || "Failed to fetch projects");
  }

  const currentUserId = (await GetCurrentUserId()).body;

  const projectsWithCreators = projectFetch.body.projects.map((p: any) => {          
    return { folder: p, addedBy: "", creatorRelations: p.projectCreatorRelations || [] };
  });

  // Fetch user role as a const (not state)
  const result = await FetchWithValidation(
    z.object({ role: z.string(), isAuthenticated: z.boolean() }),
    `${process.env.API_URL}/roles/current`
  );
  const userRole = result.success ? result.data.role : null;

  return (
      <ProjectSearchClient 
      projects={projectsWithCreators}
      currentUserId={currentUserId}
      userRole={userRole}
      />
  );
}