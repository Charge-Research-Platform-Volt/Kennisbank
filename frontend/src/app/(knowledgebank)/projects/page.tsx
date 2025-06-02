import ProjectSearchClient from "./components/project-search-client";
import { ListProjectsPaged } from "@/actions/projectActions";
import { GetCurrentUserId } from "@/actions/userActions";
import { ApiResponse } from "@/types/apiResponse.type";
import { Project } from "@/types/project.type";

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

  const projects: Project[] = projectFetch.body.projects;

  return (
    <ProjectSearchClient 
    projects={projects.map(f => {
      return {folder: f, addedBy: ""}
    })}
    currentUserId={currentUserId}
    />
  );
}