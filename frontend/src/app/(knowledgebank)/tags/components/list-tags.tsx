import React from "react";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema } from "@/types/tag.type";
import TagListItem from "./tag-list-item";
import TagListItemAdmin from "./tag-list-item-admin";
import { getCurrentUserRole } from "@/lib/auth-server";

export default async function ListTags() {
    const userRole = await getCurrentUserRole(); 
    const isAdmin = userRole.role === "admin";

    const tagsResult = await FetchWithValidation(
        TagsArraySchema,
        "http://backend:8080/tag/all-tags",
    );

    if (!tagsResult.success) {
        return (
            <div className="w-full">
                <div className="flex justify-center items-center w-full">
                    <p className="text-muted-foreground">Failed to load tags.</p>
                </div>
            </div>
        );
    }

    
    const tags = TagsArraySchema.parse(tagsResult.data)
      
    if (tags.length === 0) {
        return (
            <div className="w-full">
                <div className="flex justify-center items-center w-full">
                    <p className="text-muted-foreground">No tags found.</p>
                </div>
            </div>
        );
    }

    return (
        <div className="w-full">
            {tags.map((tag) => (
                <div key={tag.id} className="mb-2 flex justify-between items-center w-full">
                    {
                        isAdmin ?
                        ( <TagListItemAdmin tag={tag} /> ) :
                        ( <TagListItem tag={tag} />)
                    }
                </div>
            ))}
        </div>
    );
}