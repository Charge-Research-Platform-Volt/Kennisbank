import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagArraySchema } from "@/types/tag.type";

/**
 * 
 * @returns All tags from the backend
 */
export default async function ListTags() {
    const result = await FetchWithValidation(
        TagArraySchema,
        "http://backend:8080/Tag/all-tags",
      );
    
      if(!result.success) {
        throw new Error("Data validation failed");
      }
    
    return result
}