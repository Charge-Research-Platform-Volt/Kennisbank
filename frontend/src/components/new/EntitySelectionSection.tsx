import React from 'react';
import { Check, Trash2, Plus, Pencil } from 'lucide-react';

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
    isManual?: boolean; // true if manually added, false/undefined if AI-extracted
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
    onRemoveEntity?: (entityName: string) => void;
    onEditEntity?: (oldName: string, newName: string) => void;
    onConvertToManual?: (oldName: string, newName: string, similars: SimilarEntity[], existingId?: string) => void; // Convert AI entity to manual with search results
    onAddEntity?: (entityName: string, entityId?: string, entityType?: string) => void;
    searchEndpoint?: string; // e.g., "/api/persons/list" or "/api/organisations/list"
    emptyMessage?: string;
    singularLabel?: string; // e.g., "entity", "author", "organization"
    pluralLabel?: string;   // e.g., "entities", "authors", "organizations"
}

export function EntitySelectionSection({
    entities,
    selections,
    onSelectionChange,
    onRemoveEntity,
    onEditEntity,
    onConvertToManual,
    onAddEntity,
    searchEndpoint,
    emptyMessage = "No entities found",
    singularLabel = "entity",
    pluralLabel = "entities"
}: EntitySelectionSectionProps) {
    const [isAdding, setIsAdding] = React.useState(false);
    const [searchQuery, setSearchQuery] = React.useState('');
    const [searchResults, setSearchResults] = React.useState<any[]>([]);
    const [isSearching, setIsSearching] = React.useState(false);

    // Debounced search
    React.useEffect(() => {
        if (!searchEndpoint || !searchQuery.trim() || !isAdding) {
            setSearchResults([]);
            return;
        }

        const timer = setTimeout(async () => {
            setIsSearching(true);
            try {
                const response = await fetch(`${searchEndpoint}?searchQuery=${encodeURIComponent(searchQuery)}`, {
                    credentials: 'include'
                });
                const data = await response.json();
                if (data.success) {
                    setSearchResults(data.body || []);
                }
            } catch (error) {
                console.error('Error searching:', error);
            } finally {
                setIsSearching(false);
            }
        }, 300);

        return () => clearTimeout(timer);
    }, [searchQuery, searchEndpoint, isAdding]);

    const handleAddExisting = (entity: any) => {
        if (onAddEntity) {
            onAddEntity(entity.name || entity.Name, entity.id || entity.Id, entity.type || entity.Type || singularLabel);
        }
        setIsAdding(false);
        setSearchQuery('');
        setSearchResults([]);
    };

    const handleAddNew = () => {
        if (onAddEntity && searchQuery.trim()) {
            onAddEntity(searchQuery.trim());
        }
        setIsAdding(false);
        setSearchQuery('');
        setSearchResults([]);
    };

    return (
        <div className="space-y-4">
            {/* Existing entities */}
            {entities && entities.length > 0 && (
                <div className="space-y-6">
                    {entities.map((entity, index) => (
                        <EntityItem
                            key={index}
                            entity={entity}
                            selection={selections.get(entity.name)}
                            onSelectionChange={(selection) => onSelectionChange(entity.name, selection)}
                            onRemove={onRemoveEntity ? () => onRemoveEntity(entity.name) : undefined}
                            onEdit={onEditEntity}
                            onConvertToManual={onConvertToManual}
                            searchEndpoint={searchEndpoint}
                            singularLabel={singularLabel}
                            pluralLabel={pluralLabel}
                        />
                    ))}
                </div>
            )}

            {/* Empty state */}
            {(!entities || entities.length === 0) && !isAdding && (
                <p className="text-sm text-gray-500">{emptyMessage}</p>
            )}

            {/* Add entity section */}
            {onAddEntity && searchEndpoint && (
                <div className="pt-2">
                    {!isAdding ? (
                        <button
                            onClick={() => setIsAdding(true)}
                            className="w-full px-4 py-2 border-2 border-dashed border-gray-300 rounded-lg text-sm text-gray-600 hover:border-purple-400 hover:text-purple-600 transition-colors flex items-center justify-center gap-2"
                        >
                            <Plus className="w-4 h-4" />
                            Add {singularLabel}
                        </button>
                    ) : (
                        <div className="border-2 border-purple-300 rounded-lg p-4 bg-purple-50">
                            <input
                                type="text"
                                value={searchQuery}
                                onChange={(e) => setSearchQuery(e.target.value)}
                                placeholder={`Search for existing ${pluralLabel} or type a new name...`}
                                className="w-full px-3 py-2 border border-gray-300 rounded-md mb-2 text-sm"
                                autoFocus
                            />

                            {/* Search results */}
                            {searchQuery.trim() && (
                                <div className="space-y-2 mb-2">
                                    {isSearching && (
                                        <p className="text-xs text-gray-500">Searching...</p>
                                    )}
                                    {!isSearching && (
                                        <React.Fragment>
                                            {/* Existing matches */}
                                            {searchResults.length > 0 && (
                                                <React.Fragment>
                                                    <p className="text-xs text-gray-600 font-medium">Existing {pluralLabel}:</p>
                                                    {searchResults.slice(0, 5).map((result, index) => (
                                                        <button
                                                            key={result.id || result.Id || `search-result-${index}`}
                                                            onClick={() => handleAddExisting(result)}
                                                            className="w-full text-left px-3 py-2 bg-white border border-gray-200 rounded-md hover:border-purple-400 hover:bg-purple-50 transition-colors text-sm"
                                                        >
                                                            <div className="font-medium">{result.name || result.Name}</div>
                                                            {(result.description || result.Description) && (
                                                                <div className="text-xs text-gray-500 truncate">{result.description || result.Description}</div>
                                                            )}
                                                        </button>
                                                    ))}
                                                </React.Fragment>
                                            )}

                                            {/* Create new option - always shown */}
                                            {searchResults.length > 0 && (
                                                <p className="text-xs text-gray-600 font-medium pt-2">Or create new:</p>
                                            )}
                                            <button
                                                onClick={handleAddNew}
                                                className="w-full text-left px-3 py-2 bg-white border border-gray-200 rounded-md hover:border-purple-400 hover:bg-purple-50 transition-colors text-sm"
                                            >
                                                <div className="font-medium">Create new: "{searchQuery}"</div>
                                                <div className="text-xs text-gray-500">Add as a new {singularLabel}</div>
                                            </button>
                                        </React.Fragment>
                                    )}
                                </div>
                            )}

                            <div className="flex gap-2">
                                <button
                                    onClick={() => {
                                        setIsAdding(false);
                                        setSearchQuery('');
                                        setSearchResults([]);
                                    }}
                                    className="px-3 py-1.5 text-sm border border-gray-300 rounded-md hover:bg-gray-50"
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

interface EntityItemProps {
    entity: EntityWithSimilars;
    selection?: EntitySelection;
    onSelectionChange: (selection: EntitySelection) => void;
    onRemove?: () => void;
    onEdit?: (oldName: string, newName: string) => void;
    onConvertToManual?: (oldName: string, newName: string, similars: SimilarEntity[], existingId?: string) => void;
    searchEndpoint?: string;
    singularLabel: string;
    pluralLabel: string;
}

function EntityItem({ entity, selection, onSelectionChange, onRemove, onEdit, onConvertToManual, searchEndpoint, singularLabel, pluralLabel }: EntityItemProps) {
    const hasSimilars = entity.similars && entity.similars.length > 0;
    const [isEditing, setIsEditing] = React.useState(false);
    const [editedName, setEditedName] = React.useState(entity.name);
    const [searchResults, setSearchResults] = React.useState<SimilarEntity[]>([]);
    const [isSearching, setIsSearching] = React.useState(false);

    // Search when editing AI entities
    React.useEffect(() => {
        if (!isEditing || entity.isManual || !searchEndpoint || !editedName.trim()) {
            setSearchResults([]);
            return;
        }

        const timer = setTimeout(async () => {
            setIsSearching(true);
            try {
                const response = await fetch(`${searchEndpoint}?searchQuery=${encodeURIComponent(editedName)}`, {
                    credentials: 'include'
                });
                const data = await response.json();
                if (data.success && data.body) {
                    // Convert to SimilarEntity format
                    const results = data.body.slice(0, 5).map((item: any) => ({
                        id: item.id || item.Id,
                        name: item.name || item.Name,
                        score: 1.0, // Manual search results are exact matches
                        type: item.type || item.Type || singularLabel
                    }));
                    setSearchResults(results);
                }
            } catch (error) {
                console.error('Error searching:', error);
            } finally {
                setIsSearching(false);
            }
        }, 300);

        return () => clearTimeout(timer);
    }, [editedName, isEditing, entity.isManual, searchEndpoint, singularLabel]);

    const handleSaveEdit = () => {
        if (editedName.trim() && editedName !== entity.name) {
            if (!entity.isManual && onConvertToManual) {
                // Convert AI entity to manual with search results
                onConvertToManual(entity.name, editedName.trim(), searchResults);
            } else if (onEdit) {
                // Edit manual entity
                onEdit(entity.name, editedName.trim());
            }
        }
        setIsEditing(false);
        setSearchResults([]);
    };

    const handleCancelEdit = () => {
        setEditedName(entity.name);
        setIsEditing(false);
        setSearchResults([]);
    };

    return (
        <div className="border border-gray-200 rounded-lg p-4 bg-gray-50 relative">
            {/* Action buttons */}
            <div className="absolute top-3 right-3 flex gap-1">
                {onEdit && !isEditing && (
                    <button
                        onClick={() => setIsEditing(true)}
                        className="p-1.5 rounded-md text-gray-400 hover:text-blue-600 hover:bg-blue-50 transition-colors"
                        title={`Edit ${singularLabel} name`}
                    >
                        <Pencil className="w-4 h-4" />
                    </button>
                )}
                {onRemove && !isEditing && (
                    <button
                        onClick={onRemove}
                        className="p-1.5 rounded-md text-gray-400 hover:text-red-600 hover:bg-red-50 transition-colors"
                        title={`Remove this ${singularLabel}`}
                    >
                        <Trash2 className="w-4 h-4" />
                    </button>
                )}
            </div>

            {/* Entity name */}
            <div className="mb-3 pr-16">
                {!isEditing ? (
                    <h3 className="text-sm font-semibold text-gray-900">
                        {!entity.isManual ? (
                            <>Extracted: <span className="text-purple-600">{entity.name}</span></>
                        ) : (
                            <span className="text-gray-900">{entity.name}</span>
                        )}
                    </h3>
                ) : (
                    <div className="space-y-2">
                        <label className="text-xs text-gray-600 font-medium">Edit name:</label>
                        <input
                            type="text"
                            value={editedName}
                            onChange={(e) => setEditedName(e.target.value)}
                            onKeyDown={(e) => {
                                if (e.key === 'Enter' && (!entity.isManual || searchResults.length === 0)) handleSaveEdit();
                                if (e.key === 'Escape') handleCancelEdit();
                            }}
                            className="w-full px-3 py-2 border border-gray-300 rounded-md text-sm"
                            autoFocus
                        />

                        {/* Search results when editing AI entities */}
                        {!entity.isManual && editedName.trim() && editedName !== entity.name && (
                            <div className="space-y-2">
                                {isSearching && (
                                    <p className="text-xs text-gray-500">Searching database...</p>
                                )}
                                {!isSearching && searchResults.length > 0 && (
                                    <>
                                        <div className="text-xs text-gray-600 font-medium mb-1">Found similar {pluralLabel} (click to select):</div>
                                        {searchResults.map((result, index) => (
                                            <button
                                                key={result.id || index}
                                                onClick={() => {
                                                    // Convert to manual and link to this entity
                                                    if (onConvertToManual) {
                                                        onConvertToManual(entity.name, result.name, [], result.id);
                                                    }
                                                    setIsEditing(false);
                                                    setSearchResults([]);
                                                }}
                                                className="w-full text-left px-3 py-2 bg-white border border-gray-200 rounded-md hover:border-purple-400 hover:bg-purple-50 transition-colors text-sm"
                                            >
                                                <div className="font-medium text-gray-900">{result.name}</div>
                                                <div className="text-xs text-gray-600">{result.type}</div>
                                            </button>
                                        ))}
                                    </>
                                )}
                            </div>
                        )}

                        <div className="flex gap-2">
                            <button
                                onClick={handleSaveEdit}
                                className="px-3 py-1.5 text-sm bg-purple-600 text-white rounded-md hover:bg-purple-700"
                            >
                                Save
                            </button>
                            <button
                                onClick={handleCancelEdit}
                                className="px-3 py-1.5 text-sm border border-gray-300 rounded-md hover:bg-gray-50"
                            >
                                Cancel
                            </button>
                        </div>
                    </div>
                )}
            </div>

            {/* AI entities: Show similar entities and selection options */}
            {!entity.isManual && (
                <>
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
                </>
            )}

            {/* Manual entities: Show status or selection options */}
            {entity.isManual && (
                <>
                    {/* If already linked to existing, show status */}
                    {selection?.action === 'use_existing' && selection.existingId ? (
                        <div className="px-4 py-3 rounded-md bg-green-50 border-2 border-green-200">
                            <p className="text-sm font-medium text-green-900">Linked to existing {singularLabel}</p>
                            <p className="text-xs text-green-700 mt-0.5">This will use the existing {singularLabel} from your database</p>
                        </div>
                    ) : (
                        <>
                            {/* Not linked yet - show selection options if has similars */}
                            {hasSimilars ? (
                                <>
                                    <div className="mb-3">
                                        <p className="text-xs text-gray-600 mb-2">
                                            Found {entity.similars.length} similar {entity.similars.length === 1 ? singularLabel : pluralLabel}:
                                        </p>
                                    </div>
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
                                        {entity.similars.map((similar) => {
                                            const calculatedScore = similar.score > 1.0
                                                ? 95 + Math.min(5, (similar.score - 1.0) * 2)
                                                : Math.round(similar.score * 95);

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
                                </>
                            ) : (
                                /* No similars - show "will create new" status */
                                <div className="px-4 py-3 rounded-md bg-blue-50 border-2 border-blue-200">
                                    <p className="text-sm font-medium text-blue-900">Will create new {singularLabel}</p>
                                    <p className="text-xs text-blue-700 mt-0.5">This {singularLabel} will be added to your database</p>
                                </div>
                            )}
                        </>
                    )}
                </>
            )}
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
