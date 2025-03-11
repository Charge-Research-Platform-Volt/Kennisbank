"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tag } from "@/types/tag.type";
import DeleteTagButton from "./delete-tag-button";
import { toast } from "sonner";
import { useActionState, useEffect, useState } from "react";
import { FormResponse } from "@/types/return.type";
import { SaveStandardizedTag } from "@/actions/standardizedTagActions";

const initialState: FormResponse<Tag> = {
  success: false,
  message: "",
};

export default function TagListItem({tag}: {tag: Tag}) {
  const [state, action, isPending] = useActionState(SaveStandardizedTag, initialState);
  const [tagName, setTagName] = useState(tag.name);

  console.log("CreateStandardizedTag state:");
  console.log(state.message);

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state]);
  
  return (
    <div className="mb-4 flex w-full items-center justify-between gap-2">
      <form className="flex flex-grow items-center gap-2" action={action}>
        <Input
              type="text"
              name="name"
              placeholder="Tag name"
              value={tagName}
              onChange={(e) => setTagName(e.target.value)}
        />
        <Input
              type="hidden"
              name="id"
              value={tag.id}
        />
        <Button
          className="w-24 cursor-pointer"
          variant="default"
          type="submit"
          disabled={isPending}
        >
          {isPending ? "Saving..." : "Save"}
        </Button>
      </form>
      <DeleteTagButton tag={tag} />
    </div>
  );
  }
  