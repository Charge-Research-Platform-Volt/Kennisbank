"use client";

import ActionButton from "@/components/ui/icon-action-button";
import ApproveTagIcon from "@/icons/tag-icons/approve-tag-icon";
import { Tag } from "@/types/tag.type";
import { DeleteTag, ApproveTag, MakeStandardized } from "@/actions/tagActions";
import AdminIcon from "@/icons/tag-icons/admin-tag";
import DeleteIcon from "@/icons/delete-icon";

export function ApproveTagButton({ tag }: { tag: Tag }) {
  return (
    <ActionButton<string>
      action={ApproveTag}
      actionName="Approving tag"
      actionArg={tag.id}
      successMessage="Tag approved"
      onSuccessAction={() => window.dispatchEvent(new Event("tagListUpdated"))}
      icon={<ApproveTagIcon className= "h-5 w-5" fill= "#737373" />}
      title="Approve tag"
    />
  );
}

export function ConvertTagButton({ tag }: { tag: Tag }) {
  return (
    <ActionButton<string>
      action={MakeStandardized}
      actionName="Convert user tag"
      actionArg={tag.id}
      successMessage="Tag converted"
      onSuccessAction={() => window.dispatchEvent(new Event("tagListUpdated"))}
      icon={<AdminIcon className= "h-5 w-5" fill= "#737373"/>}
      icon={<AdminIcon className= "h-5 w-5" fill= "#737373"/>}
      title="Convert to standardized tag"
    />
  );
}

export function DeleteTagButton({ tag, userRole }: { tag: Tag, userRole: string }) {
  if (tag && tag.canEditAndDelete || userRole == "admin") {
    return (
      <ActionButton<string>
        action={DeleteTag}
        actionName="Deleting tag"
        actionArg={tag.id}
        successMessage="Tag deleted"
        onSuccessAction={() => window.dispatchEvent(new Event("tagListUpdated"))}
        icon={<DeleteIcon className= "h-5 w-5" fill= "#737373"/>}
        icon={<DeleteIcon className= "h-5 w-5" fill= "#737373"/>}
        title="Delete tag"
      />
    );
  }

  // Fallback empty button for type safety
  return null;
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


//     <Button
// className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
// variant="default"
// type="button"
// onClick={handleAction}
// disabled={isPending}
// title={title}
// >
// {Icon}
// </Button>
// <ActionButton<string>
//   action={TagMergePopup}
//   actionName="Merge tags"
//   actionArg={tag.id}
//   successMessage="Tag merge"
//   onSuccessAction={() => window.dispatchEvent(new Event("tagListUpdated"))}
//   icon={<Merge/>}
//   title="Merge tag into another tag"
// />