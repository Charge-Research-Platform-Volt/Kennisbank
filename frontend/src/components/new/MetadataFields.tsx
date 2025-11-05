import React from 'react';
import { LanguageCodes } from '@/lists/languageCodes';

interface EditableFieldProps {
    fieldName: string;
    label: string;
    value: string;
    multiline?: boolean;
    editingField: string | null;
    onEdit: (fieldName: string) => void;
    onSave: (fieldName: string, value: string) => void;
    onCancel: () => void;
}

export function EditableField({
    fieldName,
    label,
    value,
    multiline = false,
    editingField,
    onEdit,
    onSave,
    onCancel
}: EditableFieldProps) {
    const [tempValue, setTempValue] = React.useState(value || '');
    const isEditing = editingField === fieldName;

    React.useEffect(() => {
        setTempValue(value || '');
    }, [value]);

    return (
        <div>
            <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                {label}
            </label>
            {isEditing ? (
                <div className="flex items-start gap-2">
                    {multiline ? (
                        <textarea
                            value={tempValue}
                            onChange={(e) => setTempValue(e.target.value)}
                            className="flex-1 px-3 py-2 border border-purple-500 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                            rows={3}
                            autoFocus
                            onKeyDown={(e) => {
                                if (e.key === 'Escape') onCancel();
                            }}
                        />
                    ) : (
                        <input
                            type="text"
                            value={tempValue}
                            onChange={(e) => setTempValue(e.target.value)}
                            className="flex-1 px-3 py-2 border border-purple-500 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                            autoFocus
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') onSave(fieldName, tempValue);
                                if (e.key === 'Escape') onCancel();
                            }}
                        />
                    )}
                    <button
                        onClick={() => onSave(fieldName, tempValue)}
                        className="p-2 text-green-600 hover:text-green-700"
                        title="Save"
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                    </button>
                    <button
                        onClick={onCancel}
                        className="p-2 text-red-600 hover:text-red-700"
                        title="Cancel"
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>
            ) : (
                <div className="flex items-start gap-2">
                    <span className="flex-1 text-base text-gray-900">{value || 'N/A'}</span>
                    <button
                        onClick={() => onEdit(fieldName)}
                        className="text-purple-600 hover:text-purple-700 p-1"
                    >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                        </svg>
                    </button>
                </div>
            )}
        </div>
    );
}

interface EditableLanguageFieldProps {
    fieldName: string;
    label: string;
    value: string;
    editingField: string | null;
    onEdit: (fieldName: string) => void;
    onSave: (fieldName: string, value: string) => void;
    onCancel: () => void;
}

export function EditableLanguageField({
    fieldName,
    label,
    value,
    editingField,
    onEdit,
    onSave,
    onCancel
}: EditableLanguageFieldProps) {
    const [tempValue, setTempValue] = React.useState(value || '');
    const isEditing = editingField === fieldName;

    React.useEffect(() => {
        setTempValue(value || '');
    }, [value]);

    // Find the label for the current language code
    const displayLabel = LanguageCodes.find(lang => lang.value === value)?.label || value || 'N/A';

    return (
        <div>
            <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                {label}
            </label>
            {isEditing ? (
                <div className="flex items-start gap-2">
                    <select
                        value={tempValue}
                        onChange={(e) => setTempValue(e.target.value)}
                        className="flex-1 px-3 py-2 border border-purple-500 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                        autoFocus
                        onKeyDown={(e) => {
                            if (e.key === 'Enter') onSave(fieldName, tempValue);
                            if (e.key === 'Escape') onCancel();
                        }}
                    >
                        <option value="">Select a language</option>
                        {LanguageCodes.map((lang) => (
                            <option key={lang.value} value={lang.value}>
                                {lang.label}
                            </option>
                        ))}
                    </select>
                    <button
                        onClick={() => onSave(fieldName, tempValue)}
                        className="p-2 text-green-600 hover:text-green-700"
                        title="Save"
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                    </button>
                    <button
                        onClick={onCancel}
                        className="p-2 text-red-600 hover:text-red-700"
                        title="Cancel"
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>
            ) : (
                <div className="flex items-start gap-2">
                    <span className="flex-1 text-base text-gray-900">{displayLabel}</span>
                    <button
                        onClick={() => onEdit(fieldName)}
                        className="text-purple-600 hover:text-purple-700 p-1"
                    >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                        </svg>
                    </button>
                </div>
            )}
        </div>
    );
}

interface EditableDateFieldProps {
    fieldName: string;
    label: string;
    value: string;
    editingField: string | null;
    onEdit: (fieldName: string) => void;
    onSave: (fieldName: string, value: string) => void;
    onCancel: () => void;
}

export function EditableDateField({
    fieldName,
    label,
    value,
    editingField,
    onEdit,
    onSave,
    onCancel
}: EditableDateFieldProps) {
    // Convert ISO datetime to date-only format for input
    const getDateOnly = (dateString: string) => {
        if (!dateString) return '';
        try {
            // If already in YYYY-MM-DD format, return as-is
            if (/^\d{4}-\d{2}-\d{2}$/.test(dateString)) {
                return dateString;
            }
            // Extract just the date part from ISO string (before 'T')
            // This avoids timezone conversion issues
            const datePart = dateString.split('T')[0];
            if (/^\d{4}-\d{2}-\d{2}$/.test(datePart)) {
                return datePart;
            }
            // Fallback to date parsing
            const date = new Date(dateString);
            const year = date.getFullYear();
            const month = String(date.getMonth() + 1).padStart(2, '0');
            const day = String(date.getDate()).padStart(2, '0');
            return `${year}-${month}-${day}`;
        } catch {
            return '';
        }
    };

    const [tempValue, setTempValue] = React.useState(getDateOnly(value));
    const isEditing = editingField === fieldName;

    React.useEffect(() => {
        setTempValue(getDateOnly(value));
    }, [value]);

    // Format date for display
    const formatDate = (dateString: string) => {
        if (!dateString) return 'N/A';
        try {
            // Extract just the date part to avoid timezone issues
            let datePart = dateString;
            if (dateString.includes('T')) {
                datePart = dateString.split('T')[0];
            }

            // Parse the YYYY-MM-DD format directly
            const [year, month, day] = datePart.split('-').map(Number);

            // Create a date in UTC to avoid any timezone shifts
            const date = new Date(Date.UTC(year, month - 1, day));
            return date.toLocaleDateString('en-US', {
                year: 'numeric',
                month: 'long',
                day: 'numeric',
                timeZone: 'UTC'
            });
        } catch {
            return dateString;
        }
    };

    return (
        <div>
            <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wide mb-2">
                {label}
            </label>
            {isEditing ? (
                <div className="flex items-start gap-2">
                    <input
                        type="date"
                        value={tempValue}
                        onChange={(e) => setTempValue(e.target.value)}
                        className="flex-1 px-3 py-2 border border-purple-500 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                        autoFocus
                        onKeyDown={(e) => {
                            if (e.key === 'Enter') onSave(fieldName, tempValue);
                            if (e.key === 'Escape') onCancel();
                        }}
                    />
                    <button
                        onClick={() => onSave(fieldName, tempValue)}
                        className="p-2 text-green-600 hover:text-green-700"
                        title="Save"
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                        </svg>
                    </button>
                    <button
                        onClick={onCancel}
                        className="p-2 text-red-600 hover:text-red-700"
                        title="Cancel"
                    >
                        <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>
            ) : (
                <div className="flex items-start gap-2">
                    <span className="flex-1 text-base text-gray-900">{formatDate(value)}</span>
                    <button
                        onClick={() => onEdit(fieldName)}
                        className="text-purple-600 hover:text-purple-700 p-1"
                    >
                        <svg className="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15.232 5.232l3.536 3.536m-2.036-5.036a2.5 2.5 0 113.536 3.536L6.5 21.036H3v-3.572L16.732 3.732z" />
                        </svg>
                    </button>
                </div>
            )}
        </div>
    );
}
