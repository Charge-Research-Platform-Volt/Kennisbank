"use client";

import React, { useState } from "react";
import { Button } from "@/components/ui/button";
import { CirclePlusIcon } from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuTrigger,
  DropdownMenuContent,
  DropdownMenuItem,
} from "@/components/ui/dropdown-menu";

interface ProjectActionsDropdownProps {
  currentLevel: number;
  isLoading: boolean;
  onCreateProject: () => void;
  onCreateFolder: () => void;
  onAddResource: () => void;
}

/**
 * Action buttons (new project / add folder / add resource) that result in popups
 * 
 * @author Jelle v.h. Schut
 * @param {number} currentLevel - What depth of the project the user is in
 * @param {boolean} isLoading - Whether or not some action is loading
 * @param {() => void} onCreateProject - Function to execute after creating project
 * @param {() => void} onCreateFolder - Function to execute after creating folder
 * @param {() => void} onAddResource - Function to execute after adding a resource
 * @returns Popup of some action
 */
export function ProjectActionsDropdown({ 
  currentLevel, 
  isLoading, 
  onCreateProject,
  onCreateFolder,
  onAddResource 
}: ProjectActionsDropdownProps) {
  const [open, setOpen] = useState(false);
  
  return (
    <DropdownMenu open={open} onOpenChange={setOpen}>
      <DropdownMenuTrigger
      data-testid="add"
      asChild>
        <Button 
          variant="outline"
          size="sm"
          disabled={isLoading}
        >
          <CirclePlusIcon size={16} className="mr-1" /> 
          Add
        </Button>
      </DropdownMenuTrigger>
      <DropdownMenuContent 
        align="start" 
        sideOffset={4} 
        className="z-50 bg-white border border-border rounded-md shadow-md"
      >
        {currentLevel === 0 ? (
          // Root level, only show project creation
          <DropdownMenuItem 
            className="cursor-pointer px-3 py-2 text-sm hover:bg-muted hover:text-foreground"
            data-testid="add-project"
            onSelect={() => {
              setOpen(false);
              onCreateProject();
            }}
          >
            Create New Project
          </DropdownMenuItem>
        ) : (
          // Inside a project, show folder creation and resource add
          <>
            <DropdownMenuItem 
              className="cursor-pointer px-3 py-2 text-sm hover:bg-muted hover:text-foreground"
              data-testid="add-folder"
              onSelect={() => {
                setOpen(false);
                onCreateFolder();
              }}
            >
              Create New Folder
            </DropdownMenuItem>
            <DropdownMenuItem 
              className="cursor-pointer px-3 py-2 text-sm hover:bg-muted hover:text-foreground"
              data-testid="add-resource"
              onSelect={() => {
                setOpen(false);
                onAddResource();
              }}
            >
              Add Resource
            </DropdownMenuItem>
          </>
        )}
      </DropdownMenuContent>
    </DropdownMenu>
  );
}