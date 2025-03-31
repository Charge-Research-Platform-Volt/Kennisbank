"use client";

import { DeleteStandardizedTag } from "@/actions/standardizedTagActions";
import { DeleteUserTag } from "@/actions/userTagActions";
import { Button } from "@/components/ui/button";
import DeleteIcon from "@/icons/delete-icon";
import { Tag, UserTag } from "@/types/tag.type";
import { useTransition } from "react";

export default function DeleteTagButton({tag = undefined, userTag = undefined} : {tag?: Tag, userTag?: UserTag}) {
    const [isPending, startTransition] = useTransition();

    function handleDelete() {
      if (userTag) {
        console.log("Deleting user tag:", userTag.name);
        startTransition(() => {
          DeleteUserTag(userTag);
        });
      }
      else if(tag){
        startTransition(() => {
          DeleteStandardizedTag(tag);
        });
      }
       
    }
  
    return (
        <Button
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="button"
          onClick={handleDelete}
          disabled={isPending}
        >
          <DeleteIcon className="h-5 w-5" fill="#737373" />
        </Button>
    );
  }
  