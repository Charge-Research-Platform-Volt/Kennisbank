"use client";

import { useState } from "react";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import ListProjects from "@/components/list-projects";
import { Project } from "@/types/project.type";
import { Resource } from "@/types/resource.type";
import { getProjectContentById } from "@/actions/projectActions";

interface ProjectSearchClientProps {
  projects: Project[];
  resources?: Resource[];
}

export default function ProjectSearchClient({ projects, resources = [] }: ProjectSearchClientProps) {
  const [currentQuery, setCurrentQuery] = useState("");

  // Filter projects based on search query
  const filteredProjects = (projects ?? []).filter(project =>
    project.title.toLowerCase().includes(currentQuery.toLowerCase())
  );

  return (
    <>
      <div className="*:not-first:mt-2">
        <div className="relative w-full">
          <Input
            className="peer h-10 ps-9"
            placeholder="Search"
            type="text"
            onChange={(e) => {
              setCurrentQuery(e.target.value);
            }}
          />
          <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
            <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
          </div>
        </div>
      </div>

      <div className="flex pt-2 flex-grow h-[calc(100vh-8rem)]">
        <ListProjects 
          initialResources={resources} 
          initialProjects={filteredProjects} 
          fetchProjectContent={getProjectContentById}
        />
      </div>
    </>
  );
}