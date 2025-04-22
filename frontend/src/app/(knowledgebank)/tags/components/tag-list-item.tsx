"use client";

import { Tag } from "@/types/tag.type";
import ApprovedTag from "@/icons/tag-icons/aproved-tag";
import AdminIcon from "@/icons/tag-icons/admin-tag";

export default function TagListItem({ tag }: { tag: Tag}) {
  return (
    <div className="flex w-full items-center justify-between gap-2">
      <div className="relative w-full shadow rounded-md px-3 py-1">
        <div className="flex">
          <p>{tag.name}</p>
          {tag.isStandardized ? (<AdminIcon className="h-5 w-5 ml-2" />) : tag.isApproved ? (<ApprovedTag className="h-5 w-5 ml-2" />) : "" }
        </div>
      </div>
    </div>
  );
  }
  

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


