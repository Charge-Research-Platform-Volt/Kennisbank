import React from 'react';
import { Check } from 'lucide-react';

export interface SimilarEntity {
    id: string;
    name: string;
    score: number;
    type: string;
}

export interface EntityWithSimilars {
    name: string;
    type: string; // "person" or "organisation" - suggested type for new entities
    similars: SimilarEntity[];
}

export interface EntitySelection {
    extractedName: string;
    action: 'create' | 'use_existing';
    existingId?: string;
    type?: string; // "person" or "organisation" - type to use when creating new entity
}

interface EntitySelectionSectionProps {
    entities: EntityWithSimilars[];
    selections: Map<string, EntitySelection>;
    onSelectionChange: (entityName: string, selection: EntitySelection) => void;
    emptyMessage?: string;
    singularLabel?: string; // e.g., "entity", "author", "organization"
    pluralLabel?: string;   // e.g., "entities", "authors", "organizations"
}

export function EntitySelectionSection({
    entities,
    selections,
    onSelectionChange,
    emptyMessage = "No entities found",
    singularLabel = "entity",
    pluralLabel = "entities"
}: EntitySelectionSectionProps) {
    if (!entities || entities.length === 0) {
        return (
            <div className="space-y-2">
                <p className="text-sm text-gray-500">{emptyMessage}</p>
            </div>
        );
    }

    return (
        <div className="space-y-6">
            {entities.map((entity, index) => (
                <EntityItem
                    key={index}
                    entity={entity}
                    selection={selections.get(entity.name)}
                    onSelectionChange={(selection) => onSelectionChange(entity.name, selection)}
                    singularLabel={singularLabel}
                    pluralLabel={pluralLabel}
                />
            ))}
        </div>
    );
}

interface EntityItemProps {
    entity: EntityWithSimilars;
    selection?: EntitySelection;
    onSelectionChange: (selection: EntitySelection) => void;
    singularLabel: string;
    pluralLabel: string;
}

function EntityItem({ entity, selection, onSelectionChange, singularLabel, pluralLabel }: EntityItemProps) {
    const hasSimilars = entity.similars && entity.similars.length > 0;

    return (
        <div className="border border-gray-200 rounded-lg p-4 bg-gray-50">
            {/* Extracted entity name */}
            <div className="mb-3">
                <h3 className="text-sm font-semibold text-gray-900">
                    Extracted: <span className="text-purple-600">{entity.name}</span>
                </h3>
            </div>

            {/* Similar entities found */}
            {hasSimilars && (
                <div className="mb-3">
                    <p className="text-xs text-gray-600 mb-2">
                        Found {entity.similars.length} similar {entity.similars.length === 1 ? singularLabel : pluralLabel}:
                    </p>
                </div>
            )}

            {/* Selection options */}
            <div className="space-y-2">
                {/* Create new option */}
                <SelectionOption
                    label="Create new"
                    sublabel={`Add "${entity.name}" as a new ${singularLabel}`}
                    selected={!selection || selection.action === 'create'}
                    onClick={() =>
                        onSelectionChange({
                            extractedName: entity.name,
                            action: 'create',
                        })
                    }
                />

                {/* Similar entity options */}
                {hasSimilars &&
                    entity.similars.map((similar) => {
                        // Normalize score for display (search boosting can push scores > 1.0)
                        const calculatedScore = similar.score > 1.0
                            ? 95 + Math.min(5, (similar.score - 1.0) * 2) // 95-100% for boosted scores
                            : Math.round(similar.score * 95); // 0-95% for normal scores
                            
                        const displayScore = Math.round(calculatedScore * 100) / 100;

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
                                        extractedName: entity.name,
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
