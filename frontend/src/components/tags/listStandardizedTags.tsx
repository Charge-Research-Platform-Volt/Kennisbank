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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


