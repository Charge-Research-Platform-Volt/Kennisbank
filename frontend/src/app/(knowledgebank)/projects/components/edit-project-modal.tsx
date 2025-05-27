"use client";

import React, { useState, useEffect } from 'react';
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogFooter } from '@/components/ui/dialog';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Label } from '@/components/ui/label';
import { Project } from '@/types/project.type';
import { updateProject } from '@/actions/projectActions'; 

interface EditProjectModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  project: Project | null;
}

export default function EditProjectModal({
  isOpen,
  onClose,
  onSuccess,
  project
}: EditProjectModalProps) {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [isLoading, setIsLoading] = useState(false);
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
      const updates: Record<string, unknown> = {};
      if (title.trim() && title.trim() !== project.title) {
        updates.title = title.trim();
      }
      if (typeof description === 'string' && description.trim() !== (project.description || '')) {
        updates.description = description.trim() || null;
      }
        // Only send updates if there are changes
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
            </DialogHeader>
            
            <div className="space-y-4">
                <div className="space-y-2">
                    <Label htmlFor="title">
                        {isFolder ? 'Folder' : 'Project'} Name
                    </Label>
                    <Input
                        id="title"
                        value={title}
                        onChange={(e) => setTitle(e.target.value)}
                        placeholder={`Enter ${itemType} name`}
                        disabled={isLoading}
                        autoFocus
                        onKeyDown={(e) => {
                            if (e.key === 'Enter' && !e.shiftKey) {
                                e.preventDefault();
                                handleSubmit(e as any);
                            }
                        }}
                    />
                </div>

                {!isFolder && (
                    <div className="space-y-2">
                        <Label htmlFor="description">
                            Description (optional)
                        </Label>
                        <Textarea
                            id="description"
                            value={description}
                            onChange={(e) => setDescription(e.target.value)}
                            placeholder={`Enter ${itemType} description`}
                            disabled={isLoading}
                            rows={3}
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