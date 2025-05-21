"use client";

import React, { useState, useEffect } from 'react';
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
import { createFolder } from '@/actions/projectActions';
import { ApiResponse } from '@/types/apiResponse.type';

interface CreateFolderModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSuccess: () => void;
  parentProjectId: string | null;
}

export default function CreateFolderModal({ isOpen, onClose, onSuccess, parentProjectId }: CreateFolderModalProps) {
    const [title, setTitle] = useState('');
    const [isLoading, setIsLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    
    const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!title.trim()) {
      setError("Folder name is required.");
      return;
    }

    if (!parentProjectId) {
      setError("Cannot create folder: No parent project selected.");
      return;
    }

    setIsLoading(true);

    try {
        // Call the folder creation API
        const response: ApiResponse = await createFolder(title.trim(), parentProjectId);
        
        if (response.success) {
        setTitle('');
        onSuccess(); // Trigger refresh of the project content
        } else {
        setError(response.message || "Failed to create folder.");
        }
    } catch (err) {
        console.error("Create folder error:", err);
        setError(err instanceof Error ? err.message : "An unexpected error occurred.");
    } finally {
        setIsLoading(false);
    }
    };


  // Clean up function to handle dialog closing
  const handleCloseDialog = () => {
    if (!isLoading) {
      onClose();
      setTitle(''); 
      setError(null); 
    }
  };

  return (
    <Dialog open={isOpen} onOpenChange={(open) => !open && handleCloseDialog()}>
      <DialogContent className="sm:max-w-[425px]">
        <form onSubmit={handleSubmit}>
          <DialogHeader>
            <DialogTitle>Create New Folder</DialogTitle>
            <DialogDescription>
              Enter a name for your new folder.
            </DialogDescription>
          </DialogHeader>
          <div className="grid gap-4 py-4">
            <div className="grid grid-cols-4 items-center gap-4">
              <Label htmlFor="folderName" className="text-right">
                Name
              </Label>
              <Input
                id="folderName"
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                className="col-span-3"
                required
                autoFocus
              />
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
            <Button type="submit" disabled={isLoading}>
              {isLoading ? 'Creating...' : 'Create'}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  );
}