export interface SimilarEntity {
    id: string;
    name: string;
    score: number;
    type: 'person' | 'organisation';
}

export interface AuthorWithSimilars {
    name: string;
    similars: SimilarEntity[];
}

export interface ExtractedMetadata {
    title?: string;
    abstract?: string;
    description?: string;
    publicationDate?: string;
    languageCode?: string;
    authors?: AuthorWithSimilars[];
    publicationCode?: string;
    tags?: string[];
    license?: string;
    sourceUrl?: string;
}

export interface AuthorSelection {
    extractedName: string;
    action: 'create' | 'use_existing';
    existingId?: string;
}
