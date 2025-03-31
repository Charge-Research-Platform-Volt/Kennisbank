"use client";

import { DeleteStandardizedTag } from "@/actions/standardizedTagActions";
import { DeleteUserTag } from "@/actions/userTagActions";
import { Button } from "@/components/ui/button";
import DeleteIcon from "@/icons/delete-icon";
import { Tag, UserTag } from "@/types/tag.type";
import { useTransition } from "react";
import { toast } from "sonner";

export default function DeleteTagButton({tag = undefined, userTag = undefined} : {tag?: Tag, userTag?: UserTag}) {
    const [isPending, startTransition] = useTransition();

    function handleDelete() {
      if (userTag) {
        console.log("Deleting user tag:", userTag.name);
        startTransition(async () => {
          const result = await DeleteUserTag(userTag);
          if(result && result.success){
            toast.success("Tag deleted");
          }
          else if(result && result.message) {
            toast.error(result.message);
          }
          else{
            toast.error("Error deleting tag");
          }
        });
      }
      else if(tag){
        startTransition(async () => {
          const result = await DeleteStandardizedTag(tag);
          if(result && result.success){
            toast.success("Tag deleted");
          }
          else if(result && result.message) {
            toast.error(result.message);
          }
          else{
            toast.error("Error deleting tag");
          }
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
  