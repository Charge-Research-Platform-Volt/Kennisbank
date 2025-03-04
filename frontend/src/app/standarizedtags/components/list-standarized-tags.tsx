import React from "react";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema } from "@/types/standarized-tag.type";
import { Button } from "@/components/ui/button";
import DeleteTagButton from "./delete-tag-button";

export default async function ListStandarizedTags() {
    const result = await FetchWithValidation(
        TagsArraySchema,
        "http://backend:8080/Tag/all-tags",
      );
    
      if(!result.success) {
        throw new Error("Data validation failed");
      }
    
    return (
        <div>    
        {result.data.map((tag) => (
          <div key={tag.name} className="mb-4 flex max-w-xl justify-between items-center">
            <h2>{tag.name}</h2>
            <DeleteTagButton name={tag.name} />
          </div>
        ))}
      </div>
    );
}