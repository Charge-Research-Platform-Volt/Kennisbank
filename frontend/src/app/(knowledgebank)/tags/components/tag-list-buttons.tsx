"use client";

import { ApproveUserTag } from "@/actions/userTagActions";
import ActionButton from "@/components/ui/icon-action-button";
import ApproveTagIcon from "@/icons/tag-icons/approve-tag-icon";
import { Tag, UserTag } from "@/types/tag.type";
import { DeleteStandardizedTag } from "@/actions/standardizedTagActions";
import { ConvertUserTag } from "@/actions/userTagActions";
import AdminIcon from "@/icons/tag-icons/admin-tag";
import { DeleteUserTag } from "@/actions/userTagActions";
import DeleteIcon from "@/icons/delete-icon";

export function ApproveTagButton({ tag }: { tag: UserTag }) {
  return <ActionButton<string> action={ApproveUserTag} actionName="Approving user tag" actionArg={tag.id} successMessage="Tag approved" icon={ApproveTagIcon} title="Approve tag" />;
}

export function ConvertTagButton({ tag }: { tag: UserTag }) {
  return <ActionButton<string> action={ConvertUserTag} actionName="Convert user tag" actionArg={tag.id} successMessage="Tag converted" icon={AdminIcon} title="Convert to standardized tag" />;
}

interface DeleteTagButtonProps {
  tag?: Tag;
  userTag?: UserTag;
}

export function DeleteTagButton({ tag, userTag }: DeleteTagButtonProps) {
  if (userTag) {
    return <ActionButton<UserTag> action={DeleteUserTag} actionName="Deleting user tag" actionArg={userTag} successMessage="Tag deleted" icon={DeleteIcon} title="Delete tag" />;
  }

  if (tag) {
    return <ActionButton<Tag> action={DeleteStandardizedTag} actionName="Deleting standardized tag" actionArg={tag} successMessage="Tag deleted" icon={DeleteIcon} title="Delete tag" />;
  }

  // Fallback empty button for type safety
  return null;
}
