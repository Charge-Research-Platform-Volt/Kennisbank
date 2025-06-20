"use client";

import { Button } from "@/components/ui/button";
import { Tag } from "@/types/tag.type";
import EditIcon from "@/icons/edit-icon";
import { ApproveTagButton, DeleteTagButton, ConvertTagButton } from "./tag-list-buttons";
import { useUserRole } from "@/context/user-role-context";
import { TagMergeButton } from "./merge-tags-popup";
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
  const EditButton = () => (
    tag.canEditAndDelete || userRole == "admin" ? (
      <Button
        className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
        variant="default"
        type="button"
        title="Edit Tag"
        onClick={onEditClick}
      >
        <EditIcon className="h-5 w-5" fill="#737373" />
      </Button>
    ) : null
  );

  // Usage Count styled as an icon-like element
  const UsageCount = () => (
    <div title={`Usage Count: ${tag.usageCount || 0}`}>
      <Button
        className="bg-transparent cursor-default shadow-none text-muted-foreground"
        variant="default"
        type="button"
        disabled
      >
        <span className="h-5 w-5 flex items-center justify-center font-bold">
          {tag.usageCount || 0}
        </span>
      </Button>
    </div>
  );

  // If the current user is not admin
  if (userRole != "admin") {
    return (
        <div className="flex">
          <UsageCount />
          <EditButton />
          <DeleteTagButton tag={tag} />
        </div>
    )
  }

  // If unapproved user tag 
  if (!tag.isStandardized && !tag.isApproved) {
    return (
      <div>
      {
        userRole == "admin" &&
        <div className="flex">
          <UsageCount />
          <TagMergeButton tag={tag}/>
          <ApproveTagButton tag={tag} />
          <EditButton />
          <DeleteTagButton tag={tag} />
        </div>
      }
      </div>
    );
  }
  
  // If approved user tag
  if (!tag.isStandardized && tag.isApproved) {
    return (
      <div>
        {
          userRole == "admin" && 
          <div className="flex">
            <UsageCount />
            <TagMergeButton tag={tag}/>
            <ConvertTagButton tag={tag} />
            <EditButton />
            <DeleteTagButton tag={tag} />
          </div> 
        }
      </div>
    );
  }
  
  // If standardized tag
  if (tag) {
    return (
      <div>
        {
          userRole == "admin" &&
          <div className="flex">
            <UsageCount />
            <TagMergeButton tag={tag}/>
            <EditButton />
            <DeleteTagButton tag={tag} />
          </div>
        }
      </div>
    );
  }
  
  // Fallback for type safety
  return null;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


