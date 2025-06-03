"use client";

import React, { useEffect, useRef, useState } from "react";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import AvatarEditorDialog from "@/components/ui/avatar-editor-dialog";
import { toast } from "sonner";

// intialUrl can't actually ever be 0 because zod validates it, but its impossible to tell typescrip this
export default function EditableAvatar({ initialUrl, onNewAvatar }: { initialUrl: string | null ; onNewAvatar: (blob: Blob | null) => void }) {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [avatarUrl, setAvatarUrl] = useState<string | null>(initialUrl);
  const [inputFile, setInputFile] = useState<Blob | null>(null);
  const [dialogOpen, setDialogOpen] = useState(false);
  const [animating, setAnimating] = useState(false);

  const handleFile = (file: File) => {
    setInputFile(file);
    setDialogOpen(true);
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
    e.target.value = ""; // Without this line, duplicate file wont trigger on change
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const file = e.dataTransfer.files?.[0];
    if (file) {
      if (file.type.startsWith("image/")) handleFile(file);
      else toast.error("Only image type files are supported.");
    }
  };

  const handleOpenChange = (opening: boolean) => {
    if (!opening) setDialogOpen(false);
  };

  const handleConfirm = (blob: Blob) => {
    setAvatarUrl(URL.createObjectURL(blob));
    if (avatarUrl) URL.revokeObjectURL(avatarUrl)
    onNewAvatar(blob);
  }

  const handleDefault = () => {
    if (avatarUrl) URL.revokeObjectURL(avatarUrl)
    setAvatarUrl(null);
    onNewAvatar(null);
  }

  return (
    <>
      <div
        className="relative mx-auto aspect-square w-32 cursor-pointer overflow-hidden rounded-full border border-gray-300"
        onClick={() => fileInputRef.current?.click()}
        onDrop={handleDrop}
        onDragOver={(e) => e.preventDefault()}
        role="button"
      >
        <img src={avatarUrl || "/img/default-profile-picture.svg"} alt="Profile avatar" className="absolute inset-0 h-full w-full object-cover" />
      </div>

      <input type="file" accept="image/*" className="hidden" ref={fileInputRef} onChange={handleFileInput} />

      <Dialog open={dialogOpen} onOpenChange={handleOpenChange}>
        <DialogContent className="max-w-lg" onAnimationStartCapture={() => { setAnimating(true)}} onAnimationEndCapture={() => { setAnimating(false)}}>
          <AvatarEditorDialog inputFile={inputFile!} onConfirm={handleConfirm} onDefault={handleDefault} animating={animating} />
        </DialogContent>
      </Dialog>
    </>
  );
}
