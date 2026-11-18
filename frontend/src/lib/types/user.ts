/** Who did something, as the backend sends it for avatars. id is null when the user can't be found anymore */
export type UserSummary = {
	id: string | null;
	name: string;
	customAvatarVersion: number | null;
};

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
	page: number;
	pageSize: number;
	totalCount: number;
};
