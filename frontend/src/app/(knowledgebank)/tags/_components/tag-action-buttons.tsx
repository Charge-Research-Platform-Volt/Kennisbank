"use client";

import { Button } from "@/components/ui/button";
import type { Tag } from "@/types/tag.type";
import EditIcon from "@/icons/edit-icon";
import { ApproveTagButton, DeleteTagButton, ConvertTagButton } from "./tag-list-buttons";
import { useUserRole } from "@/context/user-role-context";
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
  const { userRole } = useUserRole();

  // Common Edit Button that appears for all tag types
  const EditButton = () =>
    tag.canEditAndDelete || userRole == "admin" ? (
      <Button className="text-muted-foreground bg-transparent shadow-none hover:bg-gray-200" variant="default" type="button" title="Edit tag" onClick={onEditClick}>
        <EditIcon className="h-5 w-5" fill="#737373" />
      </Button>
    ) : null;

  // Usage Count styled as an icon-like element
  const UsageCount = () => (
    <Button className="text-muted-foreground cursor-default bg-transparent shadow-none" variant="default" type="button" title={`Usage count: ${tag.usageCount || 0}`} disabled>
      <span className="flex h-5 w-5 items-center justify-center font-bold">{tag.usageCount || 0}</span>
    </Button>
  );

  // If the current user is not admin
  if (userRole != "admin") {
    return (
      <div className="flex">
        <UsageCount />
        <EditButton />
        <DeleteTagButton tag={tag} />
      </div>
    );
  }

  // If unapproved user tag
  if (!tag.isStandardized && !tag.isApproved) {
    return (
      <div>
        {userRole == "admin" && (
          <div className="flex">
            <UsageCount />
            <ApproveTagButton tag={tag} />
            <EditButton />
            <DeleteTagButton tag={tag} />
          </div>
        )}
      </div>
    );
  }

  // If unapproved user tag
  if (!tag.isStandardized && !tag.isApproved) {
    return (
      <div>
        {userRole == "admin" && (
          <div className="flex">
            <UsageCount />
            <ApproveTagButton tag={tag} />
            <EditButton />
            <DeleteTagButton tag={tag} />
          </div>
        )}
      </div>
    );
  }

  // If approved user tag
  if (!tag.isStandardized && tag.isApproved) {
    return (
      <div>
        {userRole == "admin" && (
          <div className="flex">
            <UsageCount />
            <ConvertTagButton tag={tag} />
            <EditButton />
            <DeleteTagButton tag={tag} />
          </div>
        )}
      </div>
    );
  }

  // If standardized tag
  if (tag) {
    return (
      <div>
        {userRole == "admin" && (
          <div className="flex">
            <UsageCount />
            <EditButton />
            <DeleteTagButton tag={tag} />
          </div>
        )}
      </div>
    );
  }

  // Fallback for type safety
  return null;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
