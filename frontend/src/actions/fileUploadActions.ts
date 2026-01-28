// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

import { ApiResponse } from "@/types/apiResponse.type";

// DTO types matching the backend
export interface FileUploadInitDto {
  fileName: string;
  fileSize: number;
}

export interface FileUploadInitResponse {
  objectName: string;
  uploadId: string;
  fileName: string;
  fileSize: number;
  extension: string;
}

export interface FileUploadFinalizeDto {
  objectName: string;
  uploadId: string;
  partETags: Record<string, string>;
}

export interface FileUploadFinalizeResponse {
  objectName: string;
}

/**
 * Handles API responses and throws standardized errors
 */
const handleApiResponse = async <T = unknown>(response: Response): Promise<T> => {
  const result: ApiResponse = await response.json();

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${result.message}`);
  }

  if (!result.success) {
    throw new Error(`Request failed: ${result.message}`);
  }

  return result.body as T;
};

/**
 * Initializes a file upload session
 * @param dto File initialization information
 * @returns Upload session information including GUID
 */
export async function uploadInit(dto: FileUploadInitDto): Promise<FileUploadInitResponse> {
  const response = await fetch("/api/files/upload/init", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(dto),
  });

  return handleApiResponse<FileUploadInitResponse>(response);
}

/**
 * Uploads a single chunk of a file
 * @param guid Upload session GUID from initialization
 * @param blockId Unique block ID (hex encoded)
 * @param chunkData The chunk data to upload
 */
export async function uploadChunk(
  objectName: string,
  uploadId: string,
  partNumber: number,
  chunkData: Blob
): Promise<string> {
  const response = await fetch(`/api/files/upload/part/${objectName}/${uploadId}/${partNumber}`, {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": chunkData.type || "application/octet-stream",
    },
    body: chunkData,
  });

  const result: ApiResponse = await response.json();

  if (!response.ok || !result.success) {
    throw new Error(`Chunk upload failed: ${result.message}`);
  }
  
  return result.body.eTag;
}

/**
 * Finalizes a file upload by committing all uploaded chunks
 * @param dto Finalization information including block IDs
 * @returns Upload completion information
 */
export async function uploadFinalize(
  dto: FileUploadFinalizeDto
): Promise<FileUploadFinalizeResponse> {
  const response = await fetch("/api/files/upload/finalize", {
    method: "POST",
    credentials: "include",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify(dto),
  });

  return handleApiResponse<FileUploadFinalizeResponse>(response);
}

/**
 * Cancels an upload session by deleting all staged chunks
 * @param guid Upload session GUID to cancel
 */
export async function uploadCancel(objectName: string, uploadId: string): Promise<void> {
  const response = await fetch(`/api/files/upload/cancel/${objectName}/${uploadId}`, {
    method: "DELETE",
    credentials: "include",
  });

  await handleApiResponse(response);
}

/**
 * Downloads a file by its GUID
 * @param id The GUID of the file (resource ID)
 * @returns Blob of the downloaded file
 */
export async function downloadFile(id: string): Promise<Blob> {
  const response = await fetch(`/api/files/download/${id}`, {
    method: "GET",
    credentials: "include",
  });

  if (!response.ok) {
    // Try to parse as JSON for error message
    try {
      const result: ApiResponse = await response.json();
      throw new Error(`Download failed: ${result.message}`);
    } catch {
      throw new Error(`Download failed: ${response.status} ${response.statusText}`);
    }
  }

  return response.blob();
}

/**
 * Downloads a file and triggers browser download
 * @param id The GUID of the file (resource ID)
 * @param filename Optional filename override
 */
export async function downloadFileToDevice(id: string, filename?: string): Promise<void> {
  const blob = await downloadFile(id);

  // Create a temporary URL for the blob
  const url = window.URL.createObjectURL(blob);

  // Create a temporary anchor element and trigger download
  const a = document.createElement("a");
  a.href = url;

  if (filename) {
    a.download = filename;
  } else {
    // Extract filename from content-disposition if not provided
    a.download = "download";
  }

  document.body.appendChild(a);
  a.click();

  // Cleanup
  window.URL.revokeObjectURL(url);
  document.body.removeChild(a);
}

/**
 * Uploads a file using chunked upload strategy
 * @param file The file to upload
 * @param maxPartSize Maximum size of each chunk in bytes (default: 4MB)
 * @param onProgress Optional callback for progress updates (0-100)
 * @returns The GUID of the uploaded file
 */
export async function uploadFileChunked(
  file: File,
  maxPartSize: number = 5 * 1024 * 1024, // 5MB default
  onProgress?: (progress: number) => void
): Promise<string> {
  // Initialize upload
  const initResponse = await uploadInit({
    fileName: file.name,
    fileSize: file.size,
  });

  const { objectName, uploadId } = initResponse;
  const numberOfParts = Math.ceil(file.size / maxPartSize);
  const partETags: Record<string, string> = {};

  try
  {
    // Upload chunks
    for (let i = 1; i <= numberOfParts; i++)
    {
      const start = (i - 1) * maxPartSize;
      const end = Math.min(start + maxPartSize, file.size);
      const chunk = file.slice(start, end);

      try
      {
        const eTag: string = await uploadChunk(objectName, uploadId, i, chunk);
        partETags[i] = eTag;

        // Report progress
        if (onProgress)
        {
          const progress = Math.round(((i + 1) / numberOfParts) * 100);
          onProgress(progress);
        }
      } catch (error)
      {
        throw new Error(`Failed to upload chunk ${i + 1}/${numberOfParts}: ${error}`);
      }
    }

    // Finalize upload
    const uploadDto: FileUploadFinalizeDto = 
    {
        objectName,
        uploadId,
        partETags
    }
    
    await uploadFinalize(uploadDto);

    return objectName;
  } catch (error) {
    // Clean up on error
    try {
      await uploadCancel(objectName, uploadId);
    } catch (cleanupError) {
      console.error("Failed to clean up after error:", cleanupError);
    }
    throw error;
  }
}