import React from "react";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema } from "@/types/tag.type";
import TagListItem from "./tag-list-item";

export default async function ListStandarizedTags() {
    const result = await FetchWithValidation(
        TagsArraySchema,
        "http://backend:8080/Tag/all-tags",
      );
    
      if(!result.success) {
        throw new Error("Data validation failed");
      }
    
     return (
        <div className="w-full">    
        {
            result.data?.length > 0 ? result.data?.map((tag) => (
            <div key={tag.id} className="mb-4 flex justify-between items-center w-full">
                <TagListItem tag={tag} />
            </div>
            )) : (
                <div className="flex justify-center items-center w-full">
                    <p className="text-muted-foreground">No tags found.</p>
                </div>
            )
        }
      </div>
     );
}