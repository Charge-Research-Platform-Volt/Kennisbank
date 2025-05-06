import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { ApiResponseSchema } from "@/types/apiResponse.type";

/**
 * 
 * @returns All tags from the backend
 */
export default async function ListTags() {
    const result = await FetchWithValidation(
        ApiResponseSchema,
        `${process.env.API_URL}/Tag/all-tags`,
      );
    
      if(!result.success) {
        throw new Error("Data validation failed");
      }
    
    return result.data.body
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


