"use client";

import React, { useActionState, useEffect } from "react";
import type { FormResponse } from "@/types/return.type";
import { toast } from "sonner";
import { Tag } from "@/types/tag.type";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { AddUserTag } from "@/actions/tagActions";
import { MAX_TAG_LENGTH } from "@/../constants";

const initialState: FormResponse<Tag> = {
  success: false,
  message: "",
};

export default function CreateUserTag() {
  const [state, action, isPending] = useActionState(AddUserTag, initialState);

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
      window.dispatchEvent(new Event("tagListUpdated"));
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state]);

  return (
    <form className="flex space-x-2 w-full" action={action}>
      <div className="relative w-full">
        <Input
          data-testid="input"
          type="text"
          name="name"
          placeholder="New tag"
          disabled={isPending}
          maxLength={MAX_TAG_LENGTH}
        />
        <Button
          data-testid="button"
          className="absolute inset-y-0 right-2 flex items-center justify-center bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="submit"
          disabled={isPending}
        >
            <span className="text-xl">{isPending ? "..." : "+"}</span>
        </Button>
      </div>
    </form>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


