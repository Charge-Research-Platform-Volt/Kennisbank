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
import { Label } from "@/components/ui/label";
import React, { useState } from "react";
import { string } from "zod";

interface GeneratePopupProps {
    title: string;
    description: string;
    content: string[];
    open: boolean; // Prop to control if the dialog is open
    onClose: () => void; // Prop to notify parent of open/close changes
  }

export default function GeneratePopup({title, description, content, open, onClose} : GeneratePopupProps)
{ 
    return(
    <Dialog open={open} onOpenChange={open => !open && onClose()}>
      <DialogContent className="sm:max-w-[425px]">
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            <DialogDescription>
              {description}
            </DialogDescription>
          </DialogHeader>
          <div>
          {content.map(t=>
                <Label key={t}>{t}</Label>
            )}
          </div>
      </DialogContent>
    </Dialog>)
}