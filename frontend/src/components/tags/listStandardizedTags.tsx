import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagArraySchema } from "@/types/tag.type";

export default async function ListStandardizedTags() {
    const result = await FetchWithValidation(
        TagArraySchema,
        "http://backend:8080/Tag/all-tags",
      );
    
      if(!result.success) {
        throw new Error("Data validation failed");
      }
    
    return result
}