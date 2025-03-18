import React from "react";
// import { FetchWithValidation } from "@/lib/fetchWithValidation";
// import { UserTagsArraySchema } from "@/types/tag.type";
// import UserTagListItem from "./tag-list-item";

export default async function ListUserTags() {
    return (<div></div>);
    // const result = await FetchWithValidation(
    //   UserTagsArraySchema,
    //   "http://backend:8080/UserTag/all-tags",
    // )

    // if(!result.success) {
    //   throw new Error("Data validation failed");
    // }

    // return (
    //     <div>    
    //     {result.data.map((tag) => (
    //       <div key={tag.id} className="mb-4 flex max-w-xl justify-between items-center">
    //         <UserTagListItem tag={tag} />
    //       </div>
    //     ))}
    //   </div>
    // );
}