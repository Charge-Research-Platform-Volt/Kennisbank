import type { ResourceItem } from './resource';

export type Project = {
	id: string;
	title: string;
	description: string | null;
	createdOn: string;
	projectType: 'root' | 'folder';
	projectMemberRelations?: { memberId: string }[];
	projectTagRelations?: { tag: { id: string; name: string } }[];
	members?: { id: string; firstName: string; lastName: string; customAvatarVersion: number | null; hasCustom: boolean }[];
	tags?: { id: string; name: string }[];
};

export type ProjectListResponse = {
	items: Project[];
	page?: number;
	pageSize?: number;
	pageCount?: number;
	totalCount?: number;
};

export type ProjectFolder = {
	id: string;
	title: string;
	addedBy: string;
	depth: number;
};

export type ProjectItem = ResourceItem & {
	addedBy: string;
};

export type ProjectAncestor = {
	id: string;
	title: string;
};

export type ProjectInfo = {
	project: Project;
	rootProject: Project;
	folders: ProjectFolder[];
	items: ProjectItem[];
	members: {
		id: string;
		firstName: string;
		lastName: string;
		customAvatarVersion: number | null;
	}[];
	tags: { id: string; name: string }[];
	ancestors: ProjectAncestor[];
};
