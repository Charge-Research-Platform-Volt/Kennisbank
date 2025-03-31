"use client";

import { Tag, UserTag } from "@/types/tag.type";
import ApprovedTag from "@/icons/tag-icons/aproved-tag";
import AdminIcon from "@/icons/tag-icons/admin-tag";

export default function TagListItem({tag = undefined, userTag = undefined}: {tag?: Tag, userTag?: UserTag}) {
  console.log("TagListItem state:");
  console.log(userTag ? userTag.name : tag?.name);
  return (
    <div className="flex w-full items-center justify-between gap-2">
      <div className="relative w-full shadow rounded-md px-3 py-1">
        <div className="flex">
          <p>{userTag ? userTag.name : tag?.name}</p>
          {userTag && userTag.isApproved ? (<ApprovedTag className="h-5 w-5 ml-2" />) : userTag ? "" : (<AdminIcon className="h-5 w-5 ml-2" />)}
        </div>
      </div>
    </div>
  );
  }
  