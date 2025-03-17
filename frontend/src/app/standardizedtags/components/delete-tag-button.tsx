"use client";

import { DeleteStandardizedTag } from "@/actions/standardizedTagActions";
import { Button } from "@/components/ui/button";
import { Tag } from "@/types/tag.type";
import { useTransition } from "react";

export default function DeleteTagButton({tag} : {tag: Tag}) {
    const [isPending, startTransition] = useTransition();

    function handleDelete() {
        startTransition(() => {
            DeleteStandardizedTag(tag);
        });
    }
  
    return (
        <Button
          className="w-24 cursor-pointer"
          variant="default"
          type="submit"
          onClick={handleDelete}
          disabled={isPending}
        >
          {isPending ? "Deleting..." : "Delete"}
        </Button>
    );
  }
  