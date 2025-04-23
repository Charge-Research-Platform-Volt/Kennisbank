import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagArraySchema } from "@/types/tag.type";

/**
 * 
 * @returns All tags from the backend
 */
export default async function ListTags() {
    const result = await FetchWithValidation(
        TagArraySchema,
        `${process.env.API_URL}/Tag/all-tags`,
      );
    
      if(!result.success) {
        throw new Error("Data validation failed");
      }
    
    return result
}