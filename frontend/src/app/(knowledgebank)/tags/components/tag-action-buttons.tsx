"use client";

import { Button } from "@/components/ui/button";
import { Tag } from "@/types/tag.type";
import EditIcon from "@/icons/edit-icon";
import { ApproveTagButton, DeleteTagButton, ConvertTagButton } from "./tag-list-buttons";

interface TagActionButtonsProps {
  tag: Tag;
  onEditClick: (e: React.MouseEvent) => void;
}

/**
 * TagActionButtons - A component that renders the appropriate action buttons for tags
 * 
 * This component displays different combinations of buttons based on the type of tag:
 * - For unapproved user tags: Approve, Edit, Delete
 * - For approved user tags: Convert, Edit, Delete
 * - For standardized tags: Edit, Delete
 * 
 * @param {TagActionButtonsProps} props - The component props
 * @returns {ReactElement} - The rendered action buttons
 */
export function TagActionButtons({ tag, onEditClick }: TagActionButtonsProps) {
  // Common Edit Button that appears for all tag types
  const EditButton = () => (
    <Button
      className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
      variant="default"
      type="button"
      title="Edit tag"
      onClick={onEditClick}
    >
      <EditIcon className="h-5 w-5" fill="#737373" />
    </Button>
  );

  // If unapproved user tag 
  if (!tag.isStandardized && !tag.isApproved) {
    return (
      <div className="flex">
        <ApproveTagButton tag={tag} />
        <EditButton />
        <DeleteTagButton tag={tag} />
      </div>
    );
  }
  
  // If approved user tag
  if (!tag.isStandardized && tag.isApproved) {
    return (
      <div className="flex">
        <ConvertTagButton tag={tag} />
        <EditButton />
        <DeleteTagButton tag={tag} />
      </div>
    );
  }
  
  // Ifstandardized tag
  if (tag) {
    return (
      <div className="flex">
        <EditButton />
        <DeleteTagButton tag={tag} />
      </div>
    );
  }
  
  // Fallback for type safety
  return null;
}