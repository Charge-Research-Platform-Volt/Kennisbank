import { z } from "zod";

/**
 * Constants for response types
 */
export const STORAGE_RESPONSE_TYPE = {
  MESSAGE: 'message',
  UPLOAD: 'upload',
  FILE: 'file',
  FILEINFO: 'fileinfo',
  CONTAINER: 'container',
  PAGE: 'page',
  EXISTS: 'exists'
} as const;

// Zod schema for response type values
export const StorageResponseTypeSchema = z.enum([
  'message',
  'upload',
  'file',
  'fileinfo',
  'container',
  'page',
  'exists'
]);

// Type for the response type values, derived from the schema
export type StorageResponseType = z.infer<typeof StorageResponseTypeSchema>;

/**
 * File item schema
 */
export const FileItemSchema = z.object({
  id: z.string(),
  name: z.string(),
  fileType: z.string(),
  size: z.number(),
  hash: z.string().optional(),
  overwrite: z.boolean().optional()
});

// Type for FileItem, derived from the schema
export type FileItem = z.infer<typeof FileItemSchema>;

/**
 * Base response schema
 */
export const StorageResponseSchema = z.object({
  message: z.string(),
  responseType: StorageResponseTypeSchema
});

// Type for StorageResponse, derived from the schema
export type StorageResponse = z.infer<typeof StorageResponseSchema>;

/**
 * File upload result schema
 */
export const FileUploadResultSchema = StorageResponseSchema.extend({
  responseType: z.literal(STORAGE_RESPONSE_TYPE.UPLOAD),
  id: z.string(),
  fileType: z.string(),
  size: z.number()
});

// Type for FileUploadResult, derived from the schema
export type FileUploadResult = z.infer<typeof FileUploadResultSchema>;

/**
 * File response schema
 */
export const FileResponseSchema = StorageResponseSchema.extend({
  responseType: z.literal(STORAGE_RESPONSE_TYPE.FILE),
  id: z.string(),
  fileType: z.string()
});

// Type for FileResponse, derived from the schema
export type FileResponse = z.infer<typeof FileResponseSchema>;

/**
 * Container response schema
 */
export const ContainerResponseSchema = StorageResponseSchema.extend({
  responseType: z.literal(STORAGE_RESPONSE_TYPE.CONTAINER),
  containerName: z.string()
});

// Type for ContainerResponse, derived from the schema
export type ContainerResponse = z.infer<typeof ContainerResponseSchema>;

/**
 * Exists response schema
 */
export const ExistsResponseSchema = StorageResponseSchema.extend({
  responseType: z.literal(STORAGE_RESPONSE_TYPE.EXISTS),
  exists: z.boolean(),
  id: z.string()
});

// Type for ExistsResponse, derived from the schema
export type ExistsResponse = z.infer<typeof ExistsResponseSchema>;

/**
 * File info response schema
 */
export const FileInfoResponseSchema = StorageResponseSchema.extend({
  responseType: z.literal(STORAGE_RESPONSE_TYPE.FILEINFO),
  fileInfo: FileItemSchema
});

// Type for FileInfoResponse, derived from the schema
export type FileInfoResponse = z.infer<typeof FileInfoResponseSchema>;

/**
 * Page response schema
 */
export const PageResponseSchema = StorageResponseSchema.extend({
  responseType: z.literal(STORAGE_RESPONSE_TYPE.PAGE),
  pageIndex: z.number(),
  pageSize: z.number(),
  files: z.array(FileItemSchema)
});

// Type for PageResponse, derived from the schema
export type PageResponse = z.infer<typeof PageResponseSchema>;

/**
 * Type guard functions using Zod validation
 */
export function isFileUploadResult(response: StorageResponse): response is FileUploadResult {
  return FileUploadResultSchema.safeParse(response).success;
}

export function isFileResponse(response: StorageResponse): response is FileResponse {
  return FileResponseSchema.safeParse(response).success;
}

export function isContainerResponse(response: StorageResponse): response is ContainerResponse {
  return ContainerResponseSchema.safeParse(response).success;
}

export function isExistsResponse(response: StorageResponse): response is ExistsResponse {
  return ExistsResponseSchema.safeParse(response).success;
}

export function isFileInfoResponse(response: StorageResponse): response is FileInfoResponse {
  return FileInfoResponseSchema.safeParse(response).success;
}

export function isPageResponse(response: StorageResponse): response is PageResponse {
  return PageResponseSchema.safeParse(response).success;
}

// Alternative type guard functions using simple property checking
// These are more performant but less safe than the Zod validation approach
export function isFileUploadResultSimple(response: StorageResponse): response is FileUploadResult {
  return response.responseType === STORAGE_RESPONSE_TYPE.UPLOAD;
}

export function isFileResponseSimple(response: StorageResponse): response is FileResponse {
  return response.responseType === STORAGE_RESPONSE_TYPE.FILE;
}

export function isContainerResponseSimple(response: StorageResponse): response is ContainerResponse {
  return response.responseType === STORAGE_RESPONSE_TYPE.CONTAINER;
}

export function isExistsResponseSimple(response: StorageResponse): response is ExistsResponse {
  return response.responseType === STORAGE_RESPONSE_TYPE.EXISTS;
}

export function isFileInfoResponseSimple(response: StorageResponse): response is FileInfoResponse {
  return response.responseType === STORAGE_RESPONSE_TYPE.FILEINFO;
}

export function isPageResponseSimple(response: StorageResponse): response is PageResponse {
  return response.responseType === STORAGE_RESPONSE_TYPE.PAGE;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


