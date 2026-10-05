export type PagedResult<T> = {
	items: T[];
	totalCount: number;
};

export type ListItem = {
	id: string;
	name: string;
};
