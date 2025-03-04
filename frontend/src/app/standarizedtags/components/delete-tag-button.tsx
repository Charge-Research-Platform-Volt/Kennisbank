"use client";

import { DeleteStandarizedTag } from "@/actions/standarizedTagActions";
import { Button } from "@/components/ui/button";
import { Tag } from "@/types/tag.type";
import { useTransition } from "react";

export default function DeleteTagButton({tag} : {tag: Tag}) {
    const [isPending, startTransition] = useTransition();

    function handleDelete() {
        startTransition(() => {
            DeleteStandarizedTag(tag);
        });
    }
  
    return (
        <Button
          className="w-24"
          variant="default"
          type="submit"
          onClick={handleDelete}
          disabled={isPending}
        >
          {isPending ? "Deleting..." : "Delete"}
        </Button>
    );
  }
  