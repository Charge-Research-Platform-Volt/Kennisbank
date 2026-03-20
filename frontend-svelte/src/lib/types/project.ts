import type { ResourceItem } from "./resource";

export type Project = {
    id: string;
    title: string;
    description: string | null;
    creationDate: string;
    projectType: 'root' | 'folder';
    projectCreatorRelations?: { creatorId: string }[];
    projectTagRelations?: { tag: { id: string; name: string } }[];
    creators?: { id: string; firstName: string; lastName: string; customAvatarVersion: number }[];
};

export type ProjectListResponse = {
    projects: Project[];
    pageIndex?: number;
    pageSize?: number;
    pageCount?: number;
    totalCount?: number;
};

export type ProjectFolder = {
    folder: { id: string; title: string; };
    addedBy: string;
};

export type ProjectItem = {
    item: ResourceItem;
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
    creators: { id: string; firstName: string; lastName: string; customAvatarVersion: number | null; }[];
    tags: { id: string; name: string; }[];
    ancestors: ProjectAncestor[];
};