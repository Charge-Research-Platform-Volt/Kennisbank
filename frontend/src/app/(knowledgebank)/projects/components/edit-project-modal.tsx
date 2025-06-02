"use client";

import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter, DialogDescription } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Project } from '@/types/project.type';
import { updateProject } from '@/actions/projectActions'; 
import { AddUserDropdown, SelectTagDropdown, SelectUserDropdown } from '@/components/Selection/SelectionDropdown';
import { Tag } from '@/types/tag.type';
import { User } from '@/types/user.type';

interface EditProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  project: Project | null;
  currentCreators: User[]
}

/**
 * Popup for editing a project
 * 
 * @author Jelle v.h. Schut, Justin Liem
 * @param {boolean} isOpen - Whether or not the popup is open
 * @param {() => void} onClose - Which function to execute after closing the popup.
 * @param {() => void} onSuccess - Which function to execute after successfully editing project
 * @param {project} project - Project to edit
 * @param {string} currentCreators - The current creators of the project in question
 * @returns Popup for editing a project
 */
export default function EditProjectModal({
  isOpen,
  onClose,
  onSuccess,
  project,
  currentCreators
}: EditProjectModalProps) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [tags, setTags] = useState<Tag[]>([]);
  const [creators, setCreators] = useState<User[]>();
  
  
  const [error, setError] = useState('');

  // Reset form when modal opens/closes or project changes
  useEffect(() => {
    if (isOpen && project) {
      setTitle(project.title || '');
      setDescription(project.description || '');
      setError('');
    } else {
      setTitle('');
      setDescription('');
      setError('');
    }
  }, [isOpen, project]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!project) return;
    
    if (!title.trim()) {
      setError('Title is required');
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      console.log("test")
      const updates: Record<string, unknown> = {};
      if (title.trim() && title.trim() !== project.title) {
        updates.title = title.trim();
      }
      if (typeof description === 'string' && description.trim() !== (project.description || '')) {
        updates.description = description.trim() || null;
      }

      // Only send updates if there are changes

      if(creators && creators?.length != 0)
      {
        updates.creators = creators.map(creator => creator.id);
      }

      // For tags the user is explicitly asked to enter all new ones so just update those
      if(tags.length != 0)
      {
        updates.tags = tags.map(tag => tag.id);
      }

      if (Object.keys(updates).length === 0) {
        setIsLoading(false);
        onClose();
        return;
      }

      console.log('Updating project with:', updates);
      
      const response = await updateProject(project.id, updates);
      
      if (!response.success) {
        throw new Error(response.message);
      }
      
      onSuccess();
    } catch (err) {
      setError('Failed to update project. Please try again.');
      console.error('Error updating project:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleClose = () => {
    if (!isLoading) {
      onClose();
    }
  };

  const isFolder = project?.projectType !== 'root';
  const itemType = isFolder ? 'folder' : 'project';

return (
    <Dialog open={isOpen} onOpenChange={handleClose}>
        <DialogContent className="sm:max-w-[500px]">
            <DialogHeader>
                <DialogTitle>
                    Edit {isFolder ? 'Folder' : 'Project'}
                </DialogTitle>
                <DialogDescription>
                  Edit details of the selected project. Only creators of the project are allowed to edit these details.
                </DialogDescription>
            </DialogHeader>
            
              <div className="grid gap-4 py-4">
                {/* Title input (always shown) */}
                <div className="grid grid-cols-4 items-center gap-4">
                  <Label htmlFor="title" className="text-right">
                    {isFolder ? 'Folder' : 'Project'} Name
                  </Label>
                  <Input
                    id="title"
                    data-testid="change-project-title"
                    value={title}
                    onChange={(e) => setTitle(e.target.value)}
                    placeholder={`Enter ${itemType} name`}
                    disabled={isLoading}
                    autoFocus
                    className="col-span-3"
                    onKeyDown={(e) => {
                      if (e.key === 'Enter' && !e.shiftKey) {
                        e.preventDefault();
                        handleSubmit(e as any);
                      }
                    }}
                  />
                </div>

                {/* Description input (only if not a folder) */}
                {!isFolder && (
                  <div className="grid grid-cols-4 items-center gap-4">
                  <Label htmlFor="description" className="text-right">
                    Description
                  </Label>
                  <Textarea
                    id="description"
                    value={description}
                    onChange={(e) => setDescription(e.target.value)}
                    placeholder={`Enter ${itemType} description`}
                    disabled={isLoading}
                    className="col-span-3"
                    rows={3}
                  />
                  </div>
                )}

                {/* Creators input (only if not a folder) */}
                {!isFolder && (
                  <div className="grid grid-cols-4 items-center gap-4">
                  <Label htmlFor="creators" className="text-right">
                    Add Creators
                  </Label>
                  <AddUserDropdown
                    className="col-span-3"
                    selectMultiple={true}
                    onChangeAction={setCreators}
                    currentCreators={currentCreators} // exclude current creators when updating
                  />
                  </div>
                )}

                {/* Tags input (only if not a folder) */}
                {!isFolder && (
                  <div className="grid grid-cols-4 items-center gap-4">
                  <Label htmlFor="tags" className="text-right">
                    New Tags
                  </Label>
                  <SelectTagDropdown 
                    className="col-span-3"
                    selectMultiple={true}
                    onChangeAction={setTags}
                  />
                  </div>
                )}
                {error && (
                    <div className="text-sm text-red-600 bg-red-50 p-2 rounded">
                        {error}
                    </div>
                )}
                
                <DialogFooter>
                    <Button
                        type="button"
                        variant="outline"
                        onClick={handleClose}
                        disabled={isLoading}
                    >
                        Cancel
                    </Button>
                    <Button
                        data-testid="project-submit-update"
                        onClick={handleSubmit}
                        disabled={isLoading || !title.trim()}
                    >
                        {isLoading ? 'Updating...' : 'Update'}
                    </Button>
                </DialogFooter>
            </div>
        </DialogContent>
    </Dialog>
  );
}