export type EntityType = 'resource' | 'person' | 'organisation';
export type DatePrecision = 'Year' | 'Month' | 'Day';
export type EmbeddingStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

export type ResourceItem = {
	id: string;
	name: string;
	type: EntityType;
	fileType: string;
	publicationDate: string;
	publicationDatePrecision: DatePrecision;
	createdOn: string;
	description: string;
	trashed?: boolean;
	chunks?: string[];
	sourceUrl?: string;
	embeddingStatus: EmbeddingStatus;
};

export type RawResourceItem = ResourceItem & {
	title: string;
}

export type TrashItem = ResourceItem & {
	trashDate: string;
};

export type RelationItem = {
	id: string;
	name: string;
	role?: string;
	relation?: string;
	authorType?: string;
	fileType?: string;
};

export type ResourceDetail = {
	title: string;
	description?: string;
	languageCode?: string;
	publicationCode?: string;
	publicationDate?: string;
	publicationDatePrecision?: DatePrecision;
	journalId?: string;
	journalName?: string;
	license?: string;
	createdOn?: string;
	note?: string;
	fileType?: string;
	fileExt?: string;
	sourceUrl?: string;
	abstract?: string;
	typeId?: string;
	typeName?: string;
	authors: RelationItem[];
	organisations: RelationItem[];
	regions: RelationItem[];
	relatedPersons: RelationItem[];
	tags: RelationItem[];
	embeddingStatus: EmbeddingStatus;
};

export type PersonDetail = {
	id: string;
	name: string;
	aliases?: string[];
	description?: string;
	occupation?: string;
	createdOn?: string;
	emailAddress?: string;
	linkedin?: string;
	trashed: boolean;
	trashDate?: string;
	embeddingStatus: EmbeddingStatus;
	authored: RelationItem[];
	relatedResources: RelationItem[];
	targetPersons: RelationItem[];
	sourcePersons: RelationItem[];
	relatedOrganisations: RelationItem[];
};

export type OrganisationDetail = {
	id: string;
	name: string;
	aliases?: string[];
	website?: string;
	description?: string;
	createdOn?: string;
	emailAddress?: string;
	trashed: boolean;
	trashDate?: string;
	embeddingStatus: EmbeddingStatus;
	authored: RelationItem[];
	relatedResources: RelationItem[];
	targetOrganisations: RelationItem[];
	sourceOrganisations: RelationItem[];
	persons: RelationItem[];
};

export type NavigationTarget = {
	id: string;
	name: string;
	type: EntityType;
};

export type ExtractedMetadata = {
	title: string | null;
	abstract: string | null;
	description: string | null;
	publicationDate: string | null;
	publicationDatePrecision: 'Day' | 'Month' | 'Year' | null;
	languageCode: string | null;
	journal: string | null;
	license: string | null;
	resourceTypeName: string | null;
	authors: {
		name: string;
		type: string;
		reason?: string | null;
		occupation?: string | null;
		email?: string | null;
		suggestedAliases?: string[];
		confirmedMatch: { id: string; name: string; type: string; suggestedAliases?: string[] } | null;
	}[];
	organisations: {
		name: string;
		type: string;
		role?: string;
		reason?: string | null;
		website?: string | null;
		email?: string | null;
		suggestedAliases?: string[];
		confirmedMatch: { id: string; name: string; type: string; suggestedAliases?: string[] } | null;
	}[];
	relatedPersons: {
		name: string;
		type: string;
		role?: string;
		reason?: string | null;
		occupation?: string | null;
		website?: string | null;
		email?: string | null;
		suggestedAliases?: string[];
		confirmedMatch: { id: string; name: string; type: string; suggestedAliases?: string[] } | null;
	}[];
	publicationCode: string | null;
	tags: string[];
	regions: string[];
};

export type ResourceType = {
	id: string;
	name: string;
};
