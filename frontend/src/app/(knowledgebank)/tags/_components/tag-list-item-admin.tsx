"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tag, TagRenameDto } from "@/types/tag.type";
import { toast } from "sonner";
import { useActionState, useEffect, useState } from "react";
import { FormResponse } from "@/types/return.type";
import { RenameTag } from "@/actions/tagActions";
import SaveIcon from "@/icons/save-icon";
import ApprovedTagIcon from "@/icons/tag-icons/aproved-tag";
import AdminIcon from "@/icons/tag-icons/admin-tag";
import { TagActionButtons } from "./tag-action-buttons";

const initialStateTag: FormResponse<TagRenameDto> = {
  success: false,
  message: "",
};

/**
 * TagListItemAdmin - Component for displaying and managing tags in the admin interface
 * 
 * This component handles both standardized tags and user tags, providing:
 * - Tag display with appropriate icons
 * - Inline editing functionality
 * - Action buttons based on tag type and state
 * 
 * @param {Object} props - Component props
 * @param {Tag} [props.tag] - Tag object
 * @returns {ReactElement} The rendered tag list item
 */
export default function TagListItemAdmin({ tag }: { tag: Tag }) {
  const [saveTagState, saveTagAction, savingTagIsPending] = useActionState(RenameTag, initialStateTag);

  const [tagName, setTagName] = useState(tag.name);
  const [originalTagName, setOriginalTagName] = useState(tag.name);
  const [editing, setEditing] = useState(false);

  // Handle form action responses
  useEffect(() => {
    if (saveTagState.success) {
      toast.success(saveTagState.message);
      setEditing(false);
      setTagName(tagName);
      setOriginalTagName(tagName);
    } else if (saveTagState.message) {
      toast.error(saveTagState.message);
    }
  }, [saveTagState]);

  // Handle edit button click
  const handleEditClick = (e: React.MouseEvent) => {
    e.preventDefault();
    setEditing(true);
  };
  
  // Determine which form action to use
  const formAction = saveTagAction;
  const isSaving : boolean = savingTagIsPending;
  const tagId : string = tag.id;
  
  return (
    <div className="flex w-full items-center justify-between gap-2">
      {editing ? (
        <form className="relative w-full" action={formAction}>
          <Input
            type="text"
            name="name"
            placeholder="Tag name"
            value={tagName}
            onChange={(e) => setTagName(e.target.value)}
          />
          <div className="absolute inset-y-0 right-2 flex items-center justify-center">
            <Button
              className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
              variant="default"
              type="submit"
              title="Save tag"
              disabled={isSaving}
            >
              <SaveIcon className="h-5 w-5" fill="#737373"/>
            </Button>
          </div>
          <Input
            type="hidden"
            name="id"
            value={tagId}
          />
          <Input
            type="hidden"
            name="originalTagName"
            value={originalTagName}
          />
        </form>
      ) : (
        <form className="relative w-full shadow rounded-md px-3 py-1" action={formAction}>
          <div className="flex">
            <p>{tagName}</p>

            {
              tag.isStandardized ?
              ( <AdminIcon className="h-5 w-5 ml-2" />) :
              tag.isApproved ?
              ( <ApprovedTagIcon className="h-5 w-5 ml-2" />) :
              ""
            }
          </div>
          <div className="absolute inset-y-0 right-2 flex items-center justify-center">
            <TagActionButtons 
              tag={tag}
              onEditClick={handleEditClick} 
            />
          </div>
          <Input
            type="hidden"
            name="id"
            value={tagId}
          />
        </form>
      )}
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


