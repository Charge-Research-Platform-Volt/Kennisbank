"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tag } from "@/types/tag.type";
import DeleteTagButton from "./delete-tag-button";
import { toast } from "sonner";
import { useActionState, useEffect, useState } from "react";
import { FormResponse } from "@/types/return.type";
import { SaveStandardizedTag } from "@/actions/standardizedTagActions";
import EditIcon from "@/icons/edit-icon";
import SaveIcon from "@/icons/save-icon";
import ApprovedTag from "@/icons/tag-icons/aproved-tag";
import AdminIcon from "@/icons/tag-icons/admin-tag";

const initialState: FormResponse<Tag> = {
  success: false,
  message: "",
};

export default function TagListItemAdmin({tag, userTag = false, aprovedTag = false}: {tag: Tag, userTag?: boolean , aprovedTag?: boolean}) {
  const [state, action, isPending] = useActionState(SaveStandardizedTag, initialState);
  const [tagName, setTagName] = useState(tag.name);

  const [editting, setEditting] = useState(false);
  

  console.log("CreateStandardizedTag state:");
  console.log(state.message);

  useEffect(() => {
    if (state.success) {
      toast.success(state.message);
      setEditting(false);
    } else if (state.message) {
      toast.error(state.message);
    }
  }, [state]);
  
  return (
    <div className="flex w-full items-center justify-between gap-2">
      { editting ? 
        (
          <form className="relative w-full" action={action}>
            <Input
                  type="text"
                  name="name"
                  placeholder="Tag name"
                  value={tagName}
                  onChange={(e) => setTagName(e.target.value)}
            />
            <div className="absolute inset-y-0 right-2 flex items-center justify-center">
              <Button
                className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
                variant="default"
                type="submit"
                disabled={isPending}
              >
                <SaveIcon className="h-5 w-5" fill="#737373"/>
              </Button>
            </div>
            <Input
              type="hidden"
              name="id"
              value={tag.id}
            />
          </form>
        ) : (
          <form className="relative w-full shadow rounded-md px-3 py-1" action={action}>
            <div className="flex">
              <p>{tagName}</p>
              {aprovedTag ? (<ApprovedTag className="h-5 w-5 ml-2" />) : userTag ? "" : (<AdminIcon className="h-5 w-5 ml-2" />)}
            </div>
            <div className="absolute inset-y-0 right-2 flex items-center justify-center">
              <Button
                className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
                variant="default"
                type="submit"
                onClick={(e) => {
                  e.preventDefault();
                  setEditting(true);
                }}
              >
                <EditIcon className="h-5 w-5" fill="#737373"/>
              </Button>
              <DeleteTagButton tag={tag} />
            </div>
            <Input
              type="hidden"
              name="id"
              value={tag.id}
            />
        </form>
        )}
    </div>
  );
  }
  