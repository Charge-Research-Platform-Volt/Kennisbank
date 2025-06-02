"use client";

import ListProjects from "@/components/projects/list-projects";
import { FolderProject } from "@/types/project.type";
import { ResourceProject } from "@/types/resource.type";
import { getProjectContentById } from "@/actions/projectActions";

interface ProjectSearchClientProps {
  projects: FolderProject[];
  resources?: ResourceProject[];
  currentUserId: string;
  userRole: string | null;
}

/**
 * Projects content
 * 
 * @author Jelle v.h. Schut
 * @param {FolderProject[]} projects - Projects or folders to display
 * @param {ResourceProject[]} resources - Resources to display
 * @param {string} currentUserId - The current user
 * @param {string | null} userRole - Role of the current user
 * @returns - View of all contents that currently need to be displayed
 */
export default function ProjectSearchClient({ projects, resources = [], currentUserId, userRole}: ProjectSearchClientProps) {

  return (
    <>
      <div className="flex pt-2 flex-grow h-[calc(100vh-8rem)]">
        <ListProjects 
          initialResources={resources} 
          initialProjects={projects} 
          fetchProjectAction={getProjectContentById}
          currentUserId={currentUserId}
          userRole={userRole}
        />
      </div>
    </>
  );
}