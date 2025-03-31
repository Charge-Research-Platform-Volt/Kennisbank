"use client";

import { ConvertUserTag } from "@/actions/userTagActions";
import { Button } from "@/components/ui/button";
import { UserTag } from "@/types/tag.type";
import { useTransition } from "react";
import { toast } from "sonner";
import AdminIcon from "@/icons/tag-icons/admin-tag";

export default function ConvertTagButton({tag} : {tag: UserTag}) {
    const [isPending, startTransition] = useTransition();

    function handleApprove() {
      console.log("Convert user tag:", tag.name);
      startTransition(async () => {
        const result = await ConvertUserTag(tag.id);
        if(result && result.success){
          toast.success("Tag converted");
        }
        else if(result && result.message){
          toast.error(result.message);
        }
        else{
          toast.error("Error converting tag");
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
          title="Convert to standardized tag"
        >
          <AdminIcon className="h-5 w-5" fill="#737373" />
        </Button>
    );
  }
  