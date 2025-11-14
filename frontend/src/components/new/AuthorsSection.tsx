import React from 'react';
import { AuthorWithSimilars, AuthorSelection } from '@/types/extractedMetadata.type';
import { Check } from 'lucide-react';

interface AuthorsSectionProps {
    authors: AuthorWithSimilars[];
    selections: Map<string, AuthorSelection>;
    onSelectionChange: (authorName: string, selection: AuthorSelection) => void;
}

export function AuthorsSection({ authors, selections, onSelectionChange }: AuthorsSectionProps) {
    if (!authors || authors.length === 0) {
        return (
            <div className="space-y-2">
                <p className="text-sm text-gray-500">No authors found</p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {authors.map((author, index) => (
                <AuthorItem
                    key={index}
                    author={author}
                    selection={selections.get(author.name)}
                    onSelectionChange={(selection) => onSelectionChange(author.name, selection)}
                />
            ))}
        </div>
    );
}

interface AuthorItemProps {
    author: AuthorWithSimilars;
    selection?: AuthorSelection;
    onSelectionChange: (selection: AuthorSelection) => void;
}

function AuthorItem({ author, selection, onSelectionChange }: AuthorItemProps) {
    const hasSimilars = author.similars && author.similars.length > 0;

    return (
        <div className="border border-gray-200 rounded-lg p-4 bg-gray-50">
            {/* Extracted author name */}
            <div className="mb-3">
                <h3 className="text-sm font-semibold text-gray-900">
                    Extracted: <span className="text-purple-600">{author.name}</span>
                </h3>
            </div>

            {/* Similar entities found */}
            {hasSimilars && (
                <div className="mb-3">
                    <p className="text-xs text-gray-600 mb-2">
                        Found {author.similars.length} similar {author.similars.length === 1 ? 'entity' : 'entities'}:
                    </p>
                </div>
            )}

            {/* Selection options */}
            <div className="space-y-2">
                {/* Create new option */}
                <SelectionOption
                    label="Create new"
                    sublabel={`Add "${author.name}" as a new entity`}
                    selected={!selection || selection.action === 'create'}
                    onClick={() =>
                        onSelectionChange({
                            extractedName: author.name,
                            action: 'create',
                        })
                    }
                />

                {/* Similar entity options */}
                {hasSimilars &&
                    author.similars.map((similar) => {
                        // Normalize score for display (search boosting can push scores > 1.0)
                        const displayScore = similar.score > 1.0
                            ? 95 + Math.min(5, (similar.score - 1.0) * 2) // 95-100% for boosted scores
                            : Math.round(similar.score * 95); // 0-95% for normal scores

                        const matchQuality = displayScore >= 95 ? 'Excellent' :
                                           displayScore >= 80 ? 'Very good' :
                                           displayScore >= 60 ? 'Good' : 'Fair';

                        return (
                            <SelectionOption
                                key={similar.id}
                                label={similar.name}
                                sublabel={`${similar.type} • ${matchQuality} match (${displayScore}%)`}
                                selected={selection?.action === 'use_existing' && selection.existingId === similar.id}
                                onClick={() =>
                                    onSelectionChange({
                                        extractedName: author.name,
                                        action: 'use_existing',
                                        existingId: similar.id,
                                    })
                                }
                            />
                        );
                    })}
            </div>
        </div>
    );
}

interface SelectionOptionProps {
    label: string;
    sublabel: string;
    selected: boolean;
    onClick: () => void;
}

function SelectionOption({ label, sublabel, selected, onClick }: SelectionOptionProps) {
    return (
        <button
            onClick={onClick}
            className={`w-full text-left px-4 py-3 rounded-md border-2 transition-all ${
                selected
                    ? 'border-purple-500 bg-purple-50'
                    : 'border-gray-200 bg-white hover:border-gray-300 hover:bg-gray-50'
            }`}
        >
            <div className="flex items-start justify-between">
                <div className="flex-1">
                    <p className="text-sm font-medium text-gray-900">{label}</p>
                    <p className="text-xs text-gray-500 mt-0.5">{sublabel}</p>
                </div>
                {selected && (
                    <div className="ml-3 flex-shrink-0">
                        <div className="w-5 h-5 rounded-full bg-purple-600 flex items-center justify-center">
                            <Check className="w-3 h-3 text-white" />
                        </div>
                    </div>
                )}
            </div>
        </button>
    );
}
