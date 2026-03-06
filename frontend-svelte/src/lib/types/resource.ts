export type ResourceType = 'resource' | 'person' | 'organisation';

export type DatePrecision = 'Year' | 'Month' | 'Day';

export type ResourceItem = {
    id: string;
    name: string;
    type: ResourceType;
    fileType: string;
    publicationDate: string;
    publicationDatePrecision: DatePrecision;
    creationDate: string;
    description: string;
    chunks?: string[];
};

export type TrashItem = ResourceItem & {
    trashDate: string;
};
