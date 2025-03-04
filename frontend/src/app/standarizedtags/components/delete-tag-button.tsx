"use client";

import { DeleteStandarizedTag } from "@/actions/standarizedTagActions";
import { Button } from "@/components/ui/button";
import { useTransition } from "react";

export default function DeleteTagButton({id} : {id: string}) {
    const [isPending, startTransition] = useTransition();

    function handleDelete() {
        startTransition(() => {
            DeleteStandarizedTag(id);
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
  