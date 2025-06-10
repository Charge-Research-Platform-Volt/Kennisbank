"use client";

import React, { useState } from 'react';
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogClose,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import { createNewProject } from '@/actions/projectActions';
import { ProjectCreateDto } from '@/types/project.type'; 
import { ApiResponse } from '@/types/apiResponse.type';
import { SelectTagDropdown, SelectUserDropdown } from '@/components/Selection/SelectionDropdown';
import { Tag } from '@/types/tag.type';
import { User } from '@/types/user.type';

interface CreateProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  currentUserId: string;
}

/**
 * Popup for creating a project
 * 
 * @author Jelle v.h. Schut
 * @param {boolean} isOpen - Whether or not the popup is open
 * @param {() => void} onClose - Which function to execute after closing the popup.
 * @param {() => void} onSuccess - Which function to execute after successfully creating project
 * @param {string} currentUserId - Id of the current user
 * @returns Popup for creating a project
 */
export default function CreateProjectModal({ isOpen, onClose, onSuccess, currentUserId }: CreateProjectModalProps) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tags, setTags] = useState<Tag[]>([]);
  const [creators, setCreators] = useState<User[]>([]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!title.trim()) {
      setError("Title is required.");
      return;
    }

    setIsLoading(true);

    const projectData: ProjectCreateDto = {
        title: title.trim(),
        description: description.trim() || null,
        projectType: 'root',
        tags: tags.map(t => t.id),
        creators: creators.map(t => t.id),
    };

    try {
      const response: ApiResponse = await createNewProject(projectData);
      if (response.success) {
        onSuccess(); // Call success callback to refresh list
        onClose();   // Close modal
        setTitle(''); // Reset form
        setDescription('');
      } else {
        setError(response.message || "Failed to create project.");
      }
    } catch (err) {
      console.error("Create project error:", err);
      setError(err instanceof Error ? err.message : "An unexpected error occurred.");
    } finally {
      setIsLoading(false);
    }
  };

  // Handle closing the dialog and resetting state
  const handleCloseDialog = () => {
    if (!isLoading) {
      onClose();
      setTitle('');
      setDescription('');
      setError(null);
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleCloseDialog()}>
      <DialogContent className="sm:max-w-[425px]">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Create New Project</DialogTitle>
            <DialogDescription>
              Enter the details for your new project. Click create when you&apos;re done.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-4 items-center gap-4">
              <Label htmlFor="title" className="text-left">
                Title
              </Label>
              <Input
                id="title"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="col-span-3"
                data-testid="input-project-title"
                placeholder="Required title for the new project"
                required
              />
            </div>
            <div className="grid grid-cols-4 items-center gap-4">
              <Label htmlFor="description" className="text-left">
                Description
              </Label>
              <Textarea
                id="description"
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                className="col-span-3"
                placeholder="Optional project description"
              />
            </div>
            <div className="grid grid-cols-4 items-center gap-4">
              {/**creators */}
              <Label htmlFor="creators" className="text-left">
                Co-Creators
              </Label>
              <SelectUserDropdown
                className="col-span-3"
                selectMultiple={true}
                onChangeAction={setCreators}
                currentUserId={currentUserId}
              ></SelectUserDropdown>
            </div>
            <div className="grid grid-cols-4 items-center gap-4">
              {/**tags */}
              <Label htmlFor="tags" className="text-left">
                Tags
              </Label>
              <SelectTagDropdown 
                className="col-span-3"
                selectMultiple={true}
                onChangeAction={setTags}></SelectTagDropdown>
            </div>
            {error && (
              <p className="col-span-4 text-sm text-red-500 text-center">{error}</p>
            )}
          </div>
          <DialogFooter>
            <DialogClose asChild>
              <Button type="button" variant="outline" onClick={handleCloseDialog} disabled={isLoading}>
                Cancel
              </Button>
            </DialogClose>
            <Button type="submit" disabled={isLoading} data-testid="create-project">
              {isLoading ? 'Creating...' : 'Create'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)