import type { EmbeddingStatus } from './resource';
import type { UserSummary } from './user';

export type DocumentVisibility = 'Restricted' | 'ProjectMembers' | 'Everyone';
export type DocumentAccessType = 'Viewer' | 'Editor';

export type ProjectDocumentType = {
	id: string;
	name: string;
};

export type ProjectDocumentVersion = {
	id: string;
	versionNumber: number;
	extension: string;
	originalFileName: string;
	sizeBytes: number | null;
	note: string | null;
	uploadedBy: UserSummary;
	uploadedOn: string;
	previewStatus: EmbeddingStatus;
	embeddingStatus: EmbeddingStatus;
};

export type ProjectDocument = {
	id: string;
	title: string;
	typeId: string;
	typeName: string;
	/** Always the root project, whichever folder the document sits in */
	projectId: string;
	/** Where it sits in that project; null is the root itself */
	folderId: string | null;
	visibility: DocumentVisibility;
	generalAccessType: DocumentAccessType;
	createdBy: string;
	createdOn: string;
	updatedOn: string;
	latestVersion: ProjectDocumentVersion;
	// The current user's own level: decides which actions to show
	accessLevel: DocumentAccessType;
};

export type ProjectDocumentGrant = {
	userId: string;
	firstName: string;
	lastName: string;
	customAvatarVersion: number | null;
	accessType: DocumentAccessType;
};

export type ProjectDocumentDetail = ProjectDocument & {
	versions: ProjectDocumentVersion[];
	grants: ProjectDocumentGrant[];
};

export type ProjectDocumentAccess = {
	visibility: DocumentVisibility;
	generalAccessType: DocumentAccessType;
	grants: { userId: string; accessType: DocumentAccessType }[];
};
