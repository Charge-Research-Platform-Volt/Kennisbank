import { ApiResponse } from "@/types/apiResponse.type";

/**
 * @summary Uploads the given DTO to the given endpoint
 * @param endPoint The endpoint in the backend
 * @param dto The DTO to upload
 * @returns The ID of the newly created entity
 */
export async function UploadWithDto(endPoint: string, dto: unknown): Promise<string> {
  const response = await fetch(endPoint, {
    method: "PUT",
    credentials: "include",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(dto),
  });

  const result: ApiResponse = await response.json();

  if (!response.ok) {
    throw new Error(`Upload failed: ${response.status} ${result.message}`);
  }

  if (!result.success) {
    throw new Error(`Upload failed: ${result.message}`);
  }

  return result.body;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


