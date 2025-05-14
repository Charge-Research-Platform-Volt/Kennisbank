import { z } from "zod"
import { ResourceCreateDto, WebsiteCreateDto, DocumentCreateDto, VideoCreateDto, AudioCreateDto, FileResourceCreateDto, PersonCreateDto, OrganisationCreateDto, LargeFileFinalizeDto } from "@/types/uploadTypes"
import { resourceCreateFormSchema } from "@/components/new/NewResource"
import { ApiResponse } from "@/types/apiResponse.type";


/**
 * Helper function to convert dates to UTC ISO strings
 */
const convertToUTCDate = (dateValue: string): string => {
  if (/^\d{4}-\d{2}-\d{2}/.test(dateValue)) {
    // Create a date object and convert to UTC ISO string
    const date = new Date(dateValue);
    return date.toISOString();
  }
  return dateValue;
};

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
    throw new Error(`Request failed: ${response.status} - ${result.message}`);
  }
  
  if (!result.success) {
    throw new Error(`Operation failed: ${result.message}`);
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
 */
async function UploadChunk(resourceId: string, fileType: string, blockId: string, chunkData: Blob): Promise<void> {
  const response = await fetch(`/api/resources/large/chunk/${resourceId}/${fileType}/${blockId}`, {
    method: "POST",
    credentials: "include",
    body: chunkData,
  });

  await handleApiResponse(response);
}

/**
 * Finalizes a large file upload
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
 * Uploads a large resource by splitting it into chunks
 */
export async function UploadNewLargeResource(form: z.infer<typeof resourceCreateFormSchema>): Promise<string> {
  const CHUNK_SIZE = 20 * 1024 * 1024; // TODO: 100MB?
  const file = form.file;
  const fileName = file.name;
  const fileType = form.uploadType;
  const resourceId = await InitLargeResourceUpload(form);
  
  const numberOfChunks = Math.ceil(file.size / CHUNK_SIZE);
  const blockIds: string[] = [];

  for (let i = 0; i < numberOfChunks; i++) {
    const start = i * CHUNK_SIZE;
    const end = Math.min(start + CHUNK_SIZE, file.size);
    const chunk = file.slice(start, end);

    const blockId = Buffer.from(`block-${i}`).toString('base64');
    blockIds.push(blockId);

    await UploadChunk(resourceId, fileType, blockId, chunk);
  }

  const finalizeDto: LargeFileFinalizeDto = {
    ResourceId: resourceId,
    FileType: fileType,
    FileName: fileName,
    BlockIds: blockIds
  };

  return await finalizeLargeUpload(finalizeDto);
}