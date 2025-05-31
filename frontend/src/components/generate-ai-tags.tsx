"use client";

import React, { useTransition } from "react";
import { Button } from "./ui/button";
import { GenerateAiTagsActions } from "@/actions/aiTagsActions";
import { toast } from "sonner";
import { Badge } from "./ui/badge";

export default function GenerateAiTags({ documentId }: { documentId: string }) {
  const [isPending, startTransition] = useTransition();
  const [tags, setTags] = React.useState<string[]>([]);

  const handleGenerateTags = async () => {
    startTransition(async () => {
      const response = await GenerateAiTagsActions(documentId);

      if (response.success) {
        setTags(response.tags);
        toast.success(response.message);
      } else {
        toast.error(response.message);
      }
    });
  };

  return (
    <div>
      <Button variant={"outline"} onClick={handleGenerateTags} disabled={isPending}>
        {isPending ? "Generating..." : "Generate Tags"}
      </Button>

      {tags.length > 0 && (
        <div className="mt-2">
          {tags.map((tag, index) => (
            <Badge variant={"outline"} key={index} className="mt-2 mr-2">
              {tag}
            </Badge>
          ))}
        </div>
      )}
    </div>
  );
}
