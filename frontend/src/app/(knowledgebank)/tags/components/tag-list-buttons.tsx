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
      icon={ApproveTagIcon}
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
      icon={AdminIcon}
      title="Convert to standardized tag"
    />
  );
}

export function DeleteTagButton({ tag }: { tag: Tag }) {
  if (tag) {
    return (
      <ActionButton<string>
        action={DeleteTag}
        actionName="Deleting tag"
        actionArg={tag.id}
        successMessage="Tag deleted"
        icon={DeleteIcon}
        title="Delete tag"
      />
    );
  }

  // Fallback empty button for type safety
  return null;
}
