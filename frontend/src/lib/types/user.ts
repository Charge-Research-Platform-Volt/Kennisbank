export type UserEntry = {
	type: 'user';
	id: string;
	email: string;
	role: string;
	emailConfirmed: boolean;
	firstName: string;
	lastName: string;
	customAvatarVersion: number | null;
	createdAt: null;
};

export type InvitedEntry = {
	type: 'invited';
	id: string;
	email: string;
	role: string;
	createdAt: string;
	emailConfirmed: false;
	firstName: null;
	lastName: null;
	customAvatarVersion: null;
};

export type CombinedEntry = UserEntry | InvitedEntry;

export type CombinedListResponse = {
	items: CombinedEntry[];
	pageCount: number;
	pageIndex: number;
	pageSize: number;
	totalCount: number;
};
