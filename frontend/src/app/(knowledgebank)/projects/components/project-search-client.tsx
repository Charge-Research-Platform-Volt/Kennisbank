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
      <div className="flex flex-grow h-[calc(100vh-1rem)]">
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)