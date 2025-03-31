"use client";

import { DeleteStandardizedTag } from "@/actions/standardizedTagActions";
import { Button } from "@/components/ui/button";
import DeleteIcon from "@/icons/delete-icon";
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
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="submit"
          onClick={handleDelete}
          disabled={isPending}
        >
          <DeleteIcon className="h-5 w-5" fill="#737373" />
        </Button>
    );
  }
  