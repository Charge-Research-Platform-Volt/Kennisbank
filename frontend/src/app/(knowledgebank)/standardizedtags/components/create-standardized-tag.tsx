"use client";

import React, { useActionState, useEffect } from "react";
import type { FormResponse } from "@/types/return.type";
import { toast } from "sonner";
import { TagBase } from "@/types/tag.type";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { AddStandardizedTag } from "@/actions/standardizedTagActions";

const initialState: FormResponse<TagBase> = {
  success: false,
  message: "",
};

export default function CreateStandardizedTag() {
  const [state, action, isPending] = useActionState(AddStandardizedTag, initialState);
  console.log("CreateStandardizedTag state:");
  console.log(state.message);

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state]);

  return (
    <form className="mb-4 flex max-w-xl space-x-2" action={action}>
      <Input
        type="text"
        name="name"
        placeholder="Tag name"
        disabled={isPending}
      />
      <Button
        className="w-24 cursor-pointer"
        variant="default"
        type="submit"
        disabled={isPending}
      >
        {isPending ? "Creating..." : "Create"}
      </Button>
    </form>
  );
}
