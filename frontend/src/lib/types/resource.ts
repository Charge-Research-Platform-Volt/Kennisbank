export type EntityType = 'resource' | 'person' | 'organisation';

export type DatePrecision = 'Year' | 'Month' | 'Day';

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
};

export type PersonDetail = {
	name: string;
	description?: string;
	occupation?: string;
	createdOn?: string;
	emailAddress?: string;
	linkedin?: string;
	authored: RelationItem[];
	relatedResources: RelationItem[];
	targetPersons: RelationItem[];
	sourcePersons: RelationItem[];
	relatedOrganisations: RelationItem[];
};

export type OrganisationDetail = {
	name: string;
	website?: string;
	description?: string;
	createdOn?: string;
	emailAddress?: string;
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
	authors: {
		name: string;
		type: string;
		similars: { id: string; name: string; score: number; type: string }[];
	}[];
	organisations: {
		name: string;
		type: string;
		similars: { id: string; name: string; score: number; type: string }[];
	}[];
	relatedPersons: {
		name: string;
		type: string;
		similars: { id: string; name: string; score: number; type: string }[];
	}[];
	publicationCode: string | null;
	tags: string[];
};

export type ResourceType = {
	id: string;
	name: string;
};
