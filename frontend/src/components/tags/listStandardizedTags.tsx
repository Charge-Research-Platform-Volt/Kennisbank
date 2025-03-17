import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema } from "@/types/tag.type";

export default async function ListStandardizedTags() {
    const result = await FetchWithValidation(
        TagsArraySchema,
        "http://backend:8080/Tag/all-tags",
      );
    
      if(!result.success) {
        throw new Error("Data validation failed");
      }
    
    return result
}