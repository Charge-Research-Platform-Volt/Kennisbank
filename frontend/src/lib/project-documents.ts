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

// projectId can be the root project or any of its folders: lists and uploads use it as the location,
// single-document calls only need it to find the project
const base = (projectId: string) => `/api/projects/${projectId}/documents`;

/** What the backend accepts; the upload is refused for anything else */
export const supportedExtensions = ['pdf', 'docx'];

export function isSupportedFile(fileName: string): boolean {
	const dot = fileName.lastIndexOf('.');
	return dot > 0 && supportedExtensions.includes(fileName.slice(dot + 1).toLowerCase());
}

export const projectDocuments = {
	list: (projectId: string) => api.get<ProjectDocument[]>(base(projectId)),
	get: (projectId: string, documentId: string) =>
		api.get<ProjectDocumentDetail>(`${base(projectId)}/${documentId}`),
	create: (
		projectId: string,
		body: {
			objectName: string;
			fileName: string;
			title: string;
			typeId: string;
			note?: string;
		} & ProjectDocumentAccess
	) => api.post<string>(base(projectId), body),
	addVersion: (
		projectId: string,
		documentId: string,
		body: { objectName: string; fileName: string; note?: string }
	) => api.post<string>(`${base(projectId)}/${documentId}/versions`, body),
	update: (projectId: string, documentId: string, body: { title?: string; typeId?: string }) =>
		api.patch(`${base(projectId)}/${documentId}`, body),
	setAccess: (projectId: string, documentId: string, body: ProjectDocumentAccess) =>
		api.put(`${base(projectId)}/${documentId}/access`, body),
	move: (projectId: string, documentId: string, destinationId: string) =>
		api.post(`${base(projectId)}/${documentId}/move`, { destinationId }),
	delete: (projectId: string, documentId: string) => api.delete(`${base(projectId)}/${documentId}`),
	types: () => api.get<ProjectDocumentType[]>('/api/project-document-types')
};

export const previewUrl = (projectId: string, documentId: string, versionId: string) =>
	`${base(projectId)}/${documentId}/versions/${versionId}/preview`;

export const downloadUrl = (projectId: string, documentId: string, versionId: string) =>
	`${base(projectId)}/${documentId}/versions/${versionId}/download`;

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
