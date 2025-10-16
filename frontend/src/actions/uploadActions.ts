import { z } from "zod"
import { ResourceCreateDto, WebsiteCreateDto, DocumentCreateDto, VideoCreateDto, AudioCreateDto, FileResourceCreateDto, PersonCreateDto, OrganisationCreateDto, LargeFileFinalizeDto } from "@/types/uploadTypes"
import { resourceCreateFormSchema } from "@/components/new/NewResource"
import { ApiResponse } from "@/types/apiResponse.type";
import { convertToUTCDate } from "@/lib/dateUtils";

/**
 * Creates a base resource DTO from form data
 */
const createBaseResourceDto = (form: z.infer<typeof resourceCreateFormSchema>): ResourceCreateDto => {
  return {
    Title: form.title,
    Description: form.description,
    TypeId: form.typeId,
    LanguageCode: form.languageCode,
    PublicationCode: form.publicationCode,
    PublicationDate: convertToUTCDate(form.publicationDate),
    License: form.license,
    Sources: form.sources,
    Note: form.note,
    Tags: form.tags,
    Authors: form.authors,
    Organisations: form.organisations,
    Regions: form.regions,
    RelatedOrganisations: form.relatedOrganisations,
    RelatedPersons: form.relatedPersons,
  };
};

/**
 * Enhances the base DTO with type-specific fields
 */
const createTypeSpecificDto = (form: z.infer<typeof resourceCreateFormSchema>, baseDto: ResourceCreateDto) => {
  let dto: any = { ...baseDto };
  
  if (form.uploadType === "website") {
    dto = {
      ...dto,
      Url: form.url,
      AccessedOn: form.accessedOn ? convertToUTCDate(form.accessedOn) : null,
    } as WebsiteCreateDto;
  } else {
    dto = {
      ...dto,
      Hash: form.hash
    } as FileResourceCreateDto;
  }
  
  switch (form.uploadType) {
    case "document":
      dto = {
        ...dto,
        Abstract: form.abstract,
      } as DocumentCreateDto;
      break;
      
    case "video":
    case "audio":
      dto = {
        ...dto,
        Length: form.length,
      } as VideoCreateDto | AudioCreateDto;
      break;
  }
  
  return dto;
};

/**
 * Handles API responses and throws standardized errors
 */
const handleApiResponse = async (response: Response): Promise<string> => {
  const result: ApiResponse = await response.json();
  
  if (!response.ok) {
    throw new Error(`Upload failed: ${response.status} ${result.message}`);
  }
  
  if (!result.success) {
    throw new Error(`Upload failed: ${result.message}`);
  }
  
  return result.body;
};

/**
 * @summary Uploads a new resource to the databse
 * @param form The form of the NewResource page
 * @returns The ID of the new resource
 */
export async function UploadNewResource(form: z.infer<typeof resourceCreateFormSchema>): Promise<string> {
  const baseResourceDto = createBaseResourceDto(form);
  const dto = createTypeSpecificDto(form, baseResourceDto);
  
  // Create formdata
  const formData = new FormData();
  
  if (form.file && form.uploadType !== "website") {
    formData.append("file", form.file);
  }
  
  formData.append("uploadType", form.uploadType);
  formData.append("dto", JSON.stringify(dto));
  
  // Send the request
  const response = await fetch("/api/resources/new", {
    method: "PUT",
    credentials: "include",
    body: formData,
  });
  
  return handleApiResponse(response);
}

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
  
  return handleApiResponse(response);
}

/**
 * Initializes a large resource upload
 */
async function InitLargeResourceUpload(form: z.infer<typeof resourceCreateFormSchema>): Promise<string> {
  const baseResourceDto = createBaseResourceDto(form);
  const dto = createTypeSpecificDto(form, baseResourceDto);
  
  // Create formdata
  const formData = new FormData();

  if (form.file && form.uploadType !== "website") {
    const emptyFile = new File([""], form.file.name, { type: form.file.type });
    formData.append("file", emptyFile);
  }
  
  formData.append("uploadType", form.uploadType);
  formData.append("dto", JSON.stringify(dto));
  
  // Send the request
  const response = await fetch("/api/resources/large/init", {
    method: "PUT",
    credentials: "include",
    body: formData,
  });
  
  return handleApiResponse(response);
}

/**
 * Uploads a chunk of a file to the server
 * @param resourceId The ID of the resource
 * @param fileType The file type of the resource
 * @param blockId The ID of the current block/chunk
 * @param chunkData A slice of the original file
 * @returns Promise resolving when chunk has been uploaded
 */
async function UploadChunk(resourceId: string, fileType: string, blockId: string, chunkData: Blob): Promise<void> {
  const response = await fetch(`/api/resources/large/chunk/${resourceId}/${fileType}/${blockId}`, {
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
}

/**
 * Finalizes a large file upload
 * @param finalizeDto The DTO to finalize the large file upload
 * @returns The ID of the new resource
 */
async function finalizeLargeUpload(finalizeDto: LargeFileFinalizeDto): Promise<string> {
  const formData = new FormData();
  
  formData.append("ResourceId", finalizeDto.ResourceId);
  formData.append("FileType", finalizeDto.FileType);
  formData.append("FileName", finalizeDto.FileName);
  
  if (finalizeDto.BlockIds) {
    finalizeDto.BlockIds.forEach(blockId => {
      formData.append("BlockIds", blockId);
    });
  }

  const response = await fetch("/api/resources/large/finalize", {
    method: "PUT",
    credentials: "include",
    body: formData,
  });

  return handleApiResponse(response);
}

/**
 * Reverts a chunk upload operation by deleting the uploaded chunks and resource
 * @param resourceId The ID of the resource to revert
 * @param fileType The type of file being uploaded
 * @returns Promise resolving when cleanup is complete
 */
async function RevertChunkUploads(resourceId: string, fileType: string): Promise<void> {
  try {
    // Send request to cleanup endpoint
    const response = await fetch(`/api/resources/large/cleanup/${resourceId}`, {
      method: "DELETE",
      credentials: "include",
    });

    const result: ApiResponse = await response.json();
    
    if (!response.ok || !result.success) {
      console.error(`Failed to clean up failed upload: ${result.message}`);
    } else {
      console.log(`Successfully cleaned up failed upload for resource ${resourceId}`);
    }
  } catch (error) {
    console.error(`Error cleaning up failed upload: ${error}`);
  }
}

/**
 * @summary Uploads a large resource by splitting it into chunks
 * @param form The form of the NewResource page
 * @returns The ID of the new resource
 */
export async function UploadNewLargeResource(form: z.infer<typeof resourceCreateFormSchema>, MAX_CHUNK_SIZE: number): Promise<string> {
  const file = form.file;
  const fileName = file.name;
  const fileType = form.uploadType;
  let resourceId: string;
  const numberOfChunks = Math.ceil(file.size / MAX_CHUNK_SIZE);

  try {
    resourceId = await InitLargeResourceUpload(form);
  } catch (error) {
    throw new Error(`Failed to initialize upload: ${error}`);
  }  

  const blockIds: string[] = [];

  try {
    for (let i = 0; i < numberOfChunks; i++) {
      const start = i * MAX_CHUNK_SIZE;
      const end = Math.min(start + MAX_CHUNK_SIZE, file.size);
      const chunk = file.slice(start, end);

      const blockId = Buffer.from(i.toString().padStart(8, '0')).toString('hex');
      blockIds.push(blockId);

      try {
        await UploadChunk(resourceId, fileType, blockId, chunk);
      } catch (error) {
        throw new Error(`Failed to upload chunk ${i+1}/${numberOfChunks}. ERROR: ${error}`);
      }
    }


    const finalizeDto: LargeFileFinalizeDto = {
      ResourceId: resourceId,
      FileType: fileType,
      FileName: fileName,
      BlockIds: blockIds
    };

    return await finalizeLargeUpload(finalizeDto);
  } catch (error) {
    // If anything fails during the upload process, clean up
    await RevertChunkUploads(resourceId, fileType);
    throw error;
  }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


