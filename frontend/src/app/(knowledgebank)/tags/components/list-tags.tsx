import React from "react";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema, UserTagsArraySchema } from "@/types/tag.type";
import TagListItem from "./tag-list-item";
import TagListItemAdmin from "./tag-list-item-admin";
import { getCurrentUserRole } from "@/lib/auth-server";

export default async function ListTags() {
    let failed = false;

    const userRole = await getCurrentUserRole(); 
    const isAdmin = userRole.role === "admin"; 

    const standardizedTagsResult = await FetchWithValidation(
        TagsArraySchema,
        "http://backend:8080/Tag/all-tags",
      );
    
      if(!standardizedTagsResult.success) {
        failed = true;
      }
    const userTagsResult = await FetchWithValidation(
        UserTagsArraySchema,
        "http://backend:8080/UserTag/all-tags",
      );
    
      if(!userTagsResult.success) {
        failed = true;
      }
      
     return (
        <div className="w-full">    
        { failed ? (
            <div className="flex justify-center items-center w-full">
                <p className="text-muted-foreground">Failed to load tags.</p>
            </div>
        ) : !standardizedTagsResult.data || !userTagsResult.data || standardizedTagsResult.data?.length + userTagsResult.data?.length === 0 ? (
            <div className="flex justify-center items-center w-full">
                <p className="text-muted-foreground">No tags found.</p>
            </div>

            ) : (
                <>
                    {standardizedTagsResult.data.map((tag) => (
                        <div key={tag.id} className="mb-2 flex justify-between items-center w-full">
                            {isAdmin ? (<TagListItemAdmin tag={tag} />) : (<TagListItem tag={tag} />)}
                        </div>
                    ))}
                    {userTagsResult.data.sort((a, b) => {
                        if (a.isApproved === b.isApproved) {
                        return a.name.localeCompare(b.name); 
                        }
                        return a.isApproved ? -1 : 1;
                    }).map((tag) => (
                        <div key={tag.id} className="mb-2 flex justify-between items-center w-full">
                            {isAdmin ? (<TagListItemAdmin tag={tag} userTag={true} aprovedTag={tag.isApproved} />) : (<TagListItem tag={tag} userTag={true} aprovedTag={tag.isApproved} />)}
                        </div>
                    ))}
                </>
        )}
      </div>
     );
}