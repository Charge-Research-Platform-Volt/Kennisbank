// Re-export generic types from EntitySelectionSection
export type {
    SimilarEntity,
    EntityWithSimilars,
    EntitySelection
} from '@/components/new/EntitySelectionSection';
import type { EntityWithSimilars } from '@/components/new/EntitySelectionSection';

export type PublicationDatePrecision = 'Year' | 'Month' | 'Day' | 0 | 1 | 2;

export interface ExtractedMetadata {
    title?: string;
    abstract?: string;
    description?: string;
    publicationDate?: string;
    publicationDatePrecision?: PublicationDatePrecision;
    languageCode?: string;
    authors?: EntityWithSimilars[];
    organisations?: EntityWithSimilars[];
    relatedPersons?: EntityWithSimilars[];
    publicationCode?: string;
    tags?: string[];
    license?: string;
    sourceUrl?: string;
}
