"use client";

import React, { useActionState, useEffect } from "react";
import type { FormResponse } from "@/types/return.type";
import { toast } from "sonner";
import { TagBase } from "@/types/tag.type";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { AddStandarizedTag } from "@/actions/standarizedTagActions";

const initialState: FormResponse<TagBase> = {
  success: false,
  message: "",
};

export default function CreateStandarizedTag() {
  const [state, action, isPending] = useActionState(AddStandarizedTag, initialState);
  console.log("CreateStandarizedTag state:");
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
        className="w-24"
        variant="default"
        type="submit"
        disabled={isPending}
      >
        {isPending ? "Creating..." : "Create"}
      </Button>
    </form>
  );
}
