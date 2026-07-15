export type EntityEntry = {
	extracted: string;
	value: string;
	displayValue: string;
	role?: string;
	authorType?: string;
	suggestedAliases?: string[];
	reason?: string | null;
	occupation?: string | null;
	website?: string | null;
	email?: string | null;
};
