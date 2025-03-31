"use client";

import { ApproveUserTag } from "@/actions/userTagActions";
import { Button } from "@/components/ui/button";
import ApproveTagIcon from "@/icons/tag-icons/approve-tag-icon";
import { UserTag } from "@/types/tag.type";
import { useTransition } from "react";
import { toast } from "sonner";

export default function ApproveTagButton({tag} : {tag: UserTag}) {
    const [isPending, startTransition] = useTransition();

    function handleApprove() {
      console.log("Aproving user tag:", tag.name);
      startTransition(async () => {
        const result = await ApproveUserTag(tag.id);
        if(result && result.success){
          toast.success("Tag approved");
        }
        else if(result && result.message){
          toast.error(result.message);
        }
        else{
          toast.error("Error approving tag");
        }
      });
    }
  
    return (
        <Button
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="button"
          onClick={handleApprove}
          disabled={isPending}
        >
          <ApproveTagIcon className="h-5 w-5" fill="#737373" />
        </Button>
    );
  }
  