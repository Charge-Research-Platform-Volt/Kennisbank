"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tag, UserTag } from "@/types/tag.type";
import { toast } from "sonner";
import { useActionState, useEffect, useState } from "react";
import { FormResponse } from "@/types/return.type";
import { SaveStandardizedTag } from "@/actions/standardizedTagActions";
import SaveIcon from "@/icons/save-icon";
import ApprovedTag from "@/icons/tag-icons/aproved-tag";
import AdminIcon from "@/icons/tag-icons/admin-tag";
import { SaveUserTag } from "@/actions/userTagActions";
import { TagActionButtons } from "./tag-action-buttons";

const initialStateStandardizedTag: FormResponse<Tag> = {
  success: false,
  message: "",
};

const initialStateUserTag: FormResponse<UserTag> = {
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
 * @param {Tag} [props.tag] - Standardized tag object (optional)
 * @param {UserTag} [props.userTag] - User tag object (optional)
 * @returns {ReactElement} The rendered tag list item
 */
export default function TagListItemAdmin({tag = undefined, userTag = undefined}: {tag?: Tag, userTag?: UserTag}) {
  const [saveStandardizedTagState, saveStandardizedTagAction, savingStandardizedTagIsPending] = useActionState(SaveStandardizedTag, initialStateStandardizedTag);
  const [saveUserTagState, saveUserTagAction, savingUserTagIsPending]  = useActionState(SaveUserTag, initialStateUserTag);

  const [tagName, setTagName] = useState(userTag ? userTag.name : tag?.name);
  const [editting, setEditting] = useState(false);
  
  // Handle form action responses
  useEffect(() => {
    if (saveStandardizedTagState.success) {
      toast.success(saveStandardizedTagState.message);
      setEditting(false);
    } else if (saveStandardizedTagState.message) {
      toast.error(saveStandardizedTagState.message);
    }
  }, [saveStandardizedTagState]);

  useEffect(() => {
    if (saveUserTagState.success) {
      toast.success(saveUserTagState.message);
      setEditting(false);
    } else if (saveUserTagState.message) {
      toast.error(saveUserTagState.message);
    }
  }, [saveUserTagState]);

  // Handle edit button click
  const handleEditClick = (e: React.MouseEvent) => {
    e.preventDefault();
    setEditting(true);
  };
  
  // Determine which form action to use
  const formAction = userTag ? saveUserTagAction : saveStandardizedTagAction;
  const isSaving = savingUserTagIsPending || savingStandardizedTagIsPending;
  const tagId = userTag ? userTag.id : tag?.id;
  
  return (
    <div className="flex w-full items-center justify-between gap-2">
      {editting ? (
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
        </form>
      ) : (
        <form className="relative w-full shadow rounded-md px-3 py-1" action={formAction}>
          <div className="flex">
            <p>{tagName}</p>
            {userTag && userTag.isApproved ? (
              <ApprovedTag className="h-5 w-5 ml-2" />
            ) : userTag ? (
              ""
            ) : (
              <AdminIcon className="h-5 w-5 ml-2" />
            )}
          </div>
          <div className="absolute inset-y-0 right-2 flex items-center justify-center">
            <TagActionButtons 
              tag={tag} 
              userTag={userTag} 
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