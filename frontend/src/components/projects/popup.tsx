import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Label } from "@/components/ui/label";
import { Tag, User } from "lucide-react";
import React from "react";

interface GeneratePopupProps {
    title: string;
    description: string;
    content: string[];
    open: boolean; // Prop to control if the dialog is open
    onClose: () => void; // Prop to notify parent of open/close changes
  }
/**
 * Function to generate a popup
 * 
 * @author Justin Liem
 * @param {string} title - Title of the popup
 * @param {string} description - Description header of the popup
 * @param {string[]} content - The string content to display
 * @param {boolean} open = Whether or not the popup should be open or not
 * @param {() => void} onClose - Function that closes the popup
 * @returns A popup displaying the content as given, for tags and users it will put an icon before each entry
 */
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
          <div className="flex flex-col space-y-2 max-h-full overflow-y-auto overflow-x-auto p-2 border rounded-md bg-gray-50">
          {content.map((t, i)=>
                <div key={i} className="w-fit inline-flex items-center px-2 py-1 whitespace-nowrap gap-2 border rounded-md border-grey-300 hover:bg-purple-700">
                  {title === "Tags" ? <Tag size={16}/> : (title ==="Creators" ? <User size={16}/> : <></>)}
                  <Label key={t}>{t}</Label>
                </div>
            )}
          </div>
      </DialogContent>
    </Dialog>)
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)