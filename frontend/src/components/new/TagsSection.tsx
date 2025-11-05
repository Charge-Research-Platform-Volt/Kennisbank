import React from 'react';

interface TagsSectionProps {
    tags: string[];
    onAddTag: (tag: string) => void;
    onRemoveTag: (tag: string) => void;
}

export function TagsSection({ tags, onAddTag, onRemoveTag }: TagsSectionProps) {
    const [newTagInput, setNewTagInput] = React.useState('');

    const handleSubmitTag = (e: React.FormEvent) => {
        e.preventDefault();
        if (newTagInput.trim()) {
            onAddTag(newTagInput);
            setNewTagInput('');
        }
    };

    return (
        <div>
            {/* Display tags */}
            {tags.length > 0 && (
                <div className="flex flex-wrap gap-2 mb-3">
                    {tags.map((tag: string, index: number) => (
                        <span
                            key={index}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-purple-100 text-purple-800 rounded-full text-sm font-medium"
                        >
                            {tag}
                            <button
                                onClick={() => onRemoveTag(tag)}
                                className="hover:text-purple-900 focus:outline-none"
                                title="Remove tag"
                            >
                                <svg className="w-3.5 h-3.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                                </svg>
                            </button>
                        </span>
                    ))}
                </div>
            )}

            {/* Add new tag */}
            <form onSubmit={handleSubmitTag} className="flex gap-2">
                <input
                    type="text"
                    value={newTagInput}
                    onChange={(e) => setNewTagInput(e.target.value)}
                    placeholder="Add a tag..."
                    className="flex-1 px-3 py-2 border border-gray-300 rounded focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent text-sm"
                />
                <button
                    type="submit"
                    className="px-4 py-2 bg-purple-600 text-white rounded hover:bg-purple-700 transition-colors text-sm font-medium"
                >
                    Add
                </button>
            </form>
        </div>
    );
}
