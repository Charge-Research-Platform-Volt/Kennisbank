"use client";

import { DeleteUserTag } from "@/actions/userTagActions";
import { Button } from "@/components/ui/button";
import { Tag, UserTag } from "@/types/tag.type";
import { useTransition } from "react";

export default function DeleteUserTagButton({tag} : {tag: UserTag}) {
    const [isPending, startTransition] = useTransition();

    function handleDelete() {
        startTransition(() => {
            DeleteUserTag(tag);
        });
    }

    const isDisabled = isPending || (tag.user != "TestUser1")
  
    return (
        <Button
        className={`w-24 ${isDisabled ? "bg-gray-500 cursor-not-allowed" : ""}`}
        variant="default"
          type="submit"
          onClick={handleDelete}
          disabled={isDisabled}
        >
          {isPending ? "Deleting..." : "Delete"}
        </Button>
    );
  }
  