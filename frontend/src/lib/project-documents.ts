import { api } from '#lib/api.js';
import { Globe, Lock, Users } from '@lucide/svelte';
import type {
	DocumentVisibility,
	ProjectDocument,
	ProjectDocumentAccess,
	ProjectDocumentDetail,
	ProjectDocumentType,
	ProjectDocumentVersion
} from '#lib/types/project-document.js';

// Lists and uploads go through a project: the root or one of its folders, which is where a new document lands.
// Everything about one document only needs its own id, like a Drive file link, so links survive moves.
const projectUrl = (projectId: string) => `/api/projects/${projectId}/documents`;
const documentUrl = (documentId: string) => `/api/project-documents/${documentId}`;

type UploadedFile = { objectName: string; fileName: string; note?: string };
type NewDocument = UploadedFile & ProjectDocumentAccess & { title: string; typeId: string };
type DocumentChanges = { title?: string; typeId?: string };

/** What the backend accepts; the upload is refused for anything else */
export const supportedExtensions = ['pdf', 'docx', 'odt'];

export function isSupportedFile(fileName: string): boolean {
	const dot = fileName.lastIndexOf('.');
	return dot > 0 && supportedExtensions.includes(fileName.slice(dot + 1).toLowerCase());
}

export const projectDocuments = {
	// Per location
	list: (projectId: string) => api.get<ProjectDocument[]>(projectUrl(projectId)),
	create: (projectId: string, body: NewDocument) => api.post<string>(projectUrl(projectId), body),

	// Per document
	get: (documentId: string) => api.get<ProjectDocumentDetail>(documentUrl(documentId)),
	update: (documentId: string, body: DocumentChanges) => api.patch(documentUrl(documentId), body),
	addVersion: (documentId: string, body: UploadedFile) =>
		api.post<string>(`${documentUrl(documentId)}/versions`, body),
	setAccess: (documentId: string, body: ProjectDocumentAccess) =>
		api.put(`${documentUrl(documentId)}/access`, body),
	move: (documentId: string, destinationId: string) =>
		api.post(`${documentUrl(documentId)}/move`, { destinationId }),
	delete: (documentId: string) => api.delete(documentUrl(documentId)),

	types: () => api.get<ProjectDocumentType[]>('/api/project-document-types')
};

export const previewUrl = (documentId: string, versionId: string) =>
	`${documentUrl(documentId)}/versions/${versionId}/preview`;

export const downloadUrl = (documentId: string, versionId: string) =>
	`${documentUrl(documentId)}/versions/${versionId}/download`;

export const visibilityLabels: Record<DocumentVisibility, string> = {
	Restricted: 'Restricted',
	ProjectMembers: 'Project members',
	Everyone: 'Everyone'
};

export const visibilityDescriptions: Record<DocumentVisibility, string> = {
	Restricted: 'Only people with access can open it',
	ProjectMembers: 'Anyone in this project can open it',
	Everyone: 'Anyone in the Kennisbank can open it'
};

export function getVisibilityIcon(visibility: DocumentVisibility) {
	switch (visibility) {
		case 'Restricted':
			return Lock;
		case 'ProjectMembers':
			return Users;
		case 'Everyone':
			return Globe;
	}
}

/** True while the background pipeline still has work to do, so lists know to keep polling */
export function isProcessing(version: ProjectDocumentVersion): boolean {
	return [version.previewStatus, version.embeddingStatus].some(
		(s) => s === 'Pending' || s === 'Processing'
	);
}

/** "Minutes 4 Nov.docx" -> "Minutes 4 Nov", as the default title in the upload dialog */
export function titleFromFileName(fileName: string): string {
	const dot = fileName.lastIndexOf('.');
	return (dot > 0 ? fileName.slice(0, dot) : fileName).trim();
}

export function formatFileSize(bytes: number | null): string {
	if (bytes == null) return '-';
	if (bytes < 1024) return `${bytes} B`;
	if (bytes < 1048576) return `${(bytes / 1024).toFixed(0)} KB`;
	return `${(bytes / 1048576).toFixed(1)} MB`;
}
