import React from "react";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagArraySchema } from "@/types/tag.type";
import TagListItem from "./tag-list-item";
import TagListItemAdmin from "./tag-list-item-admin";
import { getCurrentUserRole } from "@/lib/auth-server";

export default async function ListTags() {
    const userRole = await getCurrentUserRole(); 
    const isAdmin = userRole.role === "admin";
 
    const tagsResult = await FetchWithValidation(
        TagArraySchema,
        `${process.env.API_URL}/tags/all-tags`,
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

    
    const tags = TagArraySchema.parse(tagsResult.data)
      
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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


