"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Tag, UserTag } from "@/types/tag.type";
import DeleteTagButton from "./delete-tag-button";
import { toast } from "sonner";
import { useActionState, useEffect, useState } from "react";
import { FormResponse } from "@/types/return.type";
import { SaveStandardizedTag } from "@/actions/standardizedTagActions";
import EditIcon from "@/icons/edit-icon";
import SaveIcon from "@/icons/save-icon";
import ApprovedTag from "@/icons/tag-icons/aproved-tag";
import AdminIcon from "@/icons/tag-icons/admin-tag";
import { SaveUserTag } from "@/actions/userTagActions";
import ApproveTagButton from "./approve-tag-button";

const initialStateStandardizedTag: FormResponse<Tag> = {
  success: false,
  message: "",
};

const initialStateUserTag: FormResponse<UserTag> = {
  success: false,
  message: "",
};

export default function TagListItemAdmin({tag = undefined, userTag = undefined}: {tag?: Tag, userTag?: UserTag}) {
  const [saveStandardizedTagState, saveStandardizedTagAction, savingStandardizedTagIsPending] = useActionState(SaveStandardizedTag, initialStateStandardizedTag);
  const [saveUserTagState, saveUserTagAction, savingUserTagIsPending]  = useActionState(SaveUserTag, initialStateUserTag);

  const [tagName, setTagName] = useState(userTag ? userTag.name : tag?.name);

  const [editting, setEditting] = useState(false);
  

  console.log("SaveStandardizedTag state:");
  console.log(saveStandardizedTagState.message);
  console.log("SaveUserTag state:");
  console.log(saveUserTagState.message);

  useEffect(() => {
    if (saveStandardizedTagState.success) {
      toast.success(saveStandardizedTagState.message);
      setEditting(false);
    } else if (saveStandardizedTagState.message) {
      toast.error(saveStandardizedTagState.message);
    }
  }, [saveStandardizedTagState]);

  useEffect(() => {
    if (saveUserTagState.success) {
      toast.success(saveUserTagState.message);
      setEditting(false);
    } else if (saveUserTagState.message) {
      toast.error(saveUserTagState.message);
    }
  }, [saveUserTagState]);
  
  return (
    <div className="flex w-full items-center justify-between gap-2">
      { editting ? 
        (
          <form className="relative w-full" action={userTag ? saveUserTagAction : saveStandardizedTagAction}>
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
                disabled={savingUserTagIsPending || savingStandardizedTagIsPending}
              >
                <SaveIcon className="h-5 w-5" fill="#737373"/>
              </Button>
            </div>
            <Input
              type="hidden"
              name="id"
              value={userTag ? userTag.id : tag?.id}
            />
          </form>
        ) : (
          <form className="relative w-full shadow rounded-md px-3 py-1" action={userTag ? saveUserTagAction : saveStandardizedTagAction}>
            <div className="flex">
              <p>{tagName}</p>
              {userTag && userTag.isApproved ? (<ApprovedTag className="h-5 w-5 ml-2" />) : userTag ? "" : (<AdminIcon className="h-5 w-5 ml-2" />)}
            </div>
            <div className="absolute inset-y-0 right-2 flex items-center justify-center">
              {userTag && !userTag.isApproved ? (
              <div>
                <ApproveTagButton tag={userTag} />
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
              </div>) : ""}
              <DeleteTagButton tag={tag} userTag={userTag} />
            </div>
            <Input
              type="hidden"
              name="id"
              value={userTag ? userTag.id : tag?.id}
            />
        </form>
        )}
    </div>
  );
  }
  