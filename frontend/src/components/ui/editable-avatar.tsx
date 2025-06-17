"use client";

import React, { useEffect, useRef, useState } from "react";
import { Dialog, DialogContent } from "@/components/ui/dialog";
import AvatarEditorDialog from "@/components/ui/avatar-editor-dialog";
import { toast } from "sonner";
import { Settings, Upload, Ban } from "lucide-react";

export type ImageSet = { url: string, blob: Blob }

export default function EditableAvatar({ url, onConfirm, onDefault }: { url: string; onConfirm: (blob: Blob) => void; onDefault: () => void }) {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [inputFileUrl, setInputFileUrl] = useState("");
  const [dialogOpen, setDialogOpen] = useState(false);
  const [animating, setAnimating] = useState(false);
  const [isDraggedFileValid, setIsDraggedFileValid] = useState<Boolean | null>(null);
  const [avatarMenuPosition, setAvatarMenuPosition] = useState<{ mouseX: number; mouseY: number } | null>(null);

  useEffect(() => {
    return () => { if (inputFileUrl) URL.revokeObjectURL(inputFileUrl) }
  }, [])

  const handleFile = (file: File) => {
    setInputFileUrl(URL.createObjectURL(file));
    if (inputFileUrl) URL.revokeObjectURL(inputFileUrl)
    setDialogOpen(true);
  };

  const handleFileInput = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
    e.target.value = ""; // Without this line, duplicate file wont trigger on change
  };

  const handleDragEnter = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (e.dataTransfer.items.length > 0) {
      const item = e.dataTransfer.items[0];
      setIsDraggedFileValid(item.type.startsWith("image"));
    }
  };

  const handleDrop = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    const file = e.dataTransfer.files?.[0];
    if (file) {
      if (file.type.startsWith("image/")) handleFile(file);
      else toast.error("Only image type files are supported.");
    }
    setIsDraggedFileValid(null);
  };

  const handleDragLeave = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDraggedFileValid(null);
  };

  const handleDragOver = (e: React.DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    if (isDraggedFileValid === false) {
      e.dataTransfer.dropEffect = "none";
    } else {
      e.dataTransfer.dropEffect = "copy";
    }
  };

  const handleOpenChange = (opening: boolean) => {
    if (!opening) setDialogOpen(false);
  };

  return (
    <>
      <div className="mx-auto aspect-square w-32 rounded-full" onMouseLeave={() => setAvatarMenuPosition(null)}>
        {avatarMenuPosition && (
          <div
            className="absolute z-50 overflow-hidden rounded-md border bg-white shadow-md"
            style={{ top: avatarMenuPosition.mouseY, left: avatarMenuPosition.mouseX }}
            onClick={() => setAvatarMenuPosition(null)}
          >
            <ul>
              {
                <li onClick={() => fileInputRef.current?.click()} className="cursor-pointer px-4 hover:bg-gray-200">
                  Select image
                </li>
              }
              {
                <li onClick={onDefault} className="cursor-pointer px-4 hover:bg-gray-200">
                  Default avatar
                </li>
              }
            </ul>
          </div>
        )}
        <div
          className="group relative mx-auto aspect-square w-32 overflow-hidden rounded-full border border-gray-300"
          onClick={(e) => {
            setAvatarMenuPosition({
              mouseX: e.clientX,
              mouseY: e.clientY,
            });
          }}
          onDrop={handleDrop}
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDragEnter={handleDragEnter}
          role="button"
        >
          <img
            src={url}
            alt="Profile avatar"
            className={`cursor-inherit pointer-events-none absolute inset-0 h-full w-full object-cover transition duration-300 ${
              isDraggedFileValid === null ? "group-hover:brightness-75" : "brightness-75"
            }`}
          />

          {/* Overlay Symbol, if hovering: Search, if dragging image: Drop box, if dragging else: X */}
          <div
            className={`cursor-inherit pointer-events-none absolute inset-0 flex items-center justify-center opacity-0 transition duration-300 ${
              isDraggedFileValid === null ? "group-hover:bg-black/60 group-hover:opacity-100" : "bg-black/60 opacity-100"
            }`}
          >
            {isDraggedFileValid === null ? <Settings className="h-6 w-6 text-white" /> : isDraggedFileValid ? <Upload className="h-6 w-6 text-white" /> : <Ban className="h-6 w-6 text-white" />}
          </div>
        </div>
      </div>

      <input type="file" accept="image/*" className="hidden" ref={fileInputRef} onChange={handleFileInput} />
      <Dialog open={dialogOpen} onOpenChange={handleOpenChange}>
        <DialogContent
          className="max-w-lg"
          onAnimationStartCapture={() => {
            setAnimating(true);
          }}
          onAnimationEndCapture={() => {
            setAnimating(false);
          }}
        >
          <AvatarEditorDialog url={inputFileUrl!} onConfirm={onConfirm} onDefault={onDefault} animating={animating} />
        </DialogContent>
      </Dialog>
    </>
  );
}
