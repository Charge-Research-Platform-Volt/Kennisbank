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
import { TagMergeButton } from "./merge-tags-popup";


const initialStateTag: FormResponse<TagRenameDto> = {
  success: false,
  message: "",
};

/**
 * TagListItem - Component for displaying and managing tags in the interface
 * 
 * This component handles both standardized tags and user tags, providing:
 * - Tag display with appropriate icons
 * - Inline editing functionality
 * - Action buttons based on tag type and state
 * 
 * @param {Tag} [props.tag] - Tag object
 * @param {string} [userRole] - Role that conditionally renders items available for the user, Admins can manage tags for example
 * @returns {ReactElement} The rendered tag list item
 */
export default function TagListItem({ tag, userRole }: { tag: Tag, userRole: string}) {
  const [saveTagState, saveTagAction, savingTagIsPending] = useActionState(RenameTag, initialStateTag);

  const [tagName, setTagName] = useState(tag.name);
  const [originalTagName, setOriginalTagName] = useState(tag.name);
  const [editing, setEditing] = useState(false);

  // Handle form action responses
  useEffect(() => {
    // Only display the save tag message if you actually edit the tag
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
        <div className="relative w-full shadow rounded-md px-3 py-1">
          <div className="flex justify-between items-center">
            <div className="flex items-center">
              <p>{tagName}</p>
              <div className="ml-2">
                {tag.isStandardized ? (
                  <AdminIcon className="h-5 w-5 ml-2" />
                ) : tag.isApproved ? (
                  <ApprovedTagIcon className="h-5 w-5 ml-2" />
                ) : ""}
              </div>
            </div>

            <div className="flex items-center">
              {userRole == "admin" ? <TagMergeButton tag={tag}></TagMergeButton> : null}
              <TagActionButtons
                tag={tag}
                onEditClick={handleEditClick}
                userRole={userRole}
              />
            </div>
          </div>
          <Input
            type="hidden"
            name="id"
            value={tagId}
          />
        </div>
      )}
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


