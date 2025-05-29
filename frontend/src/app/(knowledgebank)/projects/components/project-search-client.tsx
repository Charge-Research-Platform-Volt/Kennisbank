"use client";

import ListProjects from "@/components/projects/list-projects";
import { FolderProject } from "@/types/project.type";
import { ResourceProject } from "@/types/resource.type";
import { getProjectContentById } from "@/actions/projectActions";

interface ProjectSearchClientProps {
  projects: FolderProject[];
  resources?: ResourceProject[];
  currentUserId: string;
}

export default function ProjectSearchClient({ projects, resources = [], currentUserId}: ProjectSearchClientProps) {

  return (
    <>
      <div className="flex pt-2 flex-grow h-[calc(100vh-8rem)]">
        <ListProjects 
          initialResources={resources} 
          initialProjects={projects} 
          fetchProjectAction={getProjectContentById}
          currentUserId={currentUserId}
        />
      </div>
    </>
  );
}