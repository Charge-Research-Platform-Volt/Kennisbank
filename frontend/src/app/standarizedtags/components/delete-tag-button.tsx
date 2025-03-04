"use client";

import { DeleteStandarizedTag } from "@/actions/standarizedTagActions";
import { Button } from "@/components/ui/button";
import { useTransition } from "react";

export default function DeleteTagButton({id} : {id: string}) {
    // const [tag, setTag] = useTransition(DeleteStandarizedTag);
    // console.log("CreateStandarizedTag state:");
    // console.log(state.message);
    const [isPending, startTransition] = useTransition();

    function handleDelete() {
        startTransition(() => {
            DeleteStandarizedTag(id);
        });
    }
  
    // useEffect(() => {
    //   if (state.success) {
    //     toast.success(state.message);
    //   } else if (state.message) {
    //     toast.error(state.message);
    //   }
    // }, [state]);
  
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
  