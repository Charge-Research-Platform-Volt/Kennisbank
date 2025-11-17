import React from 'react';
import { LanguageCodes } from '@/lists/languageCodes';
import type { PublicationDatePrecision } from '@/types/extractedMetadata.type';

// Helper to normalize precision (handle both string and numeric enum values)
const normalizePrecision = (precision?: PublicationDatePrecision): 'Year' | 'Month' | 'Day' => {
    if (precision === 0 || precision === 'Year') return 'Year';
    if (precision === 1 || precision === 'Month') return 'Month';
    if (precision === 2 || precision === 'Day') return 'Day';
    return 'Day'; // default
};

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
    precision?: PublicationDatePrecision;
    editingField: string | null;
    onEdit: (fieldName: string) => void;
    onSave: (fieldName: string, value: string) => void;
    onCancel: () => void;
    onPrecisionChange?: (precision: 'Year' | 'Month' | 'Day') => void;
}

export function EditableDateField({
    fieldName,
    label,
    value,
    precision,
    editingField,
    onEdit,
    onSave,
    onCancel,
    onPrecisionChange
}: EditableDateFieldProps) {
    const normalizedPrecision = normalizePrecision(precision);
    const [localPrecision, setLocalPrecision] = React.useState(normalizedPrecision);

    React.useEffect(() => {
        setLocalPrecision(normalizedPrecision);
    }, [normalizedPrecision]);
    // Convert ISO datetime to date-only format for input
    const getDateOnly = (dateString: string) => {
        if (!dateString) return '';
        try {
            // If already in YYYY-MM-DD format, return as-is
            if (/^\d{4}-\d{2}-\d{2}$/.test(dateString)) {
                return dateString;
            }
            // If only year (YYYY format)
            if (/^\d{4}$/.test(dateString)) {
                return `${dateString}-01-01`;
            }
            // If year-month (YYYY-MM format)
            if (/^\d{4}-\d{2}$/.test(dateString)) {
                return `${dateString}-01`;
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

    // Format date for display based on precision
    const formatDate = (dateString: string, datePrecision: 'Year' | 'Month' | 'Day') => {
        if (!dateString) return 'N/A';
        try {
            // Extract just the date part to avoid timezone issues
            let datePart = dateString;
            if (dateString.includes('T')) {
                datePart = dateString.split('T')[0];
            }

            // Parse the YYYY-MM-DD format directly
            const [year, month, day] = datePart.split('-').map(Number);

            // Format based on precision
            if (datePrecision === 'Year') {
                return String(year);
            } else if (datePrecision === 'Month') {
                const date = new Date(Date.UTC(year, month - 1, 1));
                return date.toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    timeZone: 'UTC'
                });
            } else {
                // Full date
                const date = new Date(Date.UTC(year, month - 1, day));
                return date.toLocaleDateString('en-US', {
                    year: 'numeric',
                    month: 'long',
                    day: 'numeric',
                    timeZone: 'UTC'
                });
            }
        } catch {
            return dateString;
        }
    };

    const handlePrecisionChange = (newPrecision: 'Year' | 'Month' | 'Day') => {
        setLocalPrecision(newPrecision);
        if (onPrecisionChange) {
            onPrecisionChange(newPrecision);
        }
    };

    return (
        <div>
            <div className="flex items-center justify-between mb-2">
                <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wide">
                    {label}
                </label>
                {isEditing && (
                    <div className="flex gap-1 text-xs">
                        <button
                            type="button"
                            onClick={() => handlePrecisionChange('Year')}
                            className={`px-2 py-1 rounded ${
                                localPrecision === 'Year'
                                    ? 'bg-purple-600 text-white'
                                    : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                            }`}
                        >
                            Year
                        </button>
                        <button
                            type="button"
                            onClick={() => handlePrecisionChange('Month')}
                            className={`px-2 py-1 rounded ${
                                localPrecision === 'Month'
                                    ? 'bg-purple-600 text-white'
                                    : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                            }`}
                        >
                            Month
                        </button>
                        <button
                            type="button"
                            onClick={() => handlePrecisionChange('Day')}
                            className={`px-2 py-1 rounded ${
                                localPrecision === 'Day'
                                    ? 'bg-purple-600 text-white'
                                    : 'bg-gray-200 text-gray-700 hover:bg-gray-300'
                            }`}
                        >
                            Day
                        </button>
                    </div>
                )}
            </div>
            {isEditing ? (
                <div className="flex items-start gap-2">
                    {localPrecision === 'Year' ? (
                        <input
                            type="number"
                            value={tempValue.split('-')[0] || ''}
                            onChange={(e) => {
                                const year = e.target.value;
                                setTempValue(`${year}-01-01`);
                            }}
                            min="1900"
                            max="2100"
                            placeholder="YYYY"
                            className="flex-1 px-3 py-2 border border-purple-500 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                            autoFocus
                            onKeyDown={(e) => {
                                if (e.key === 'Enter') onSave(fieldName, tempValue);
                                if (e.key === 'Escape') onCancel();
                            }}
                        />
                    ) : localPrecision === 'Month' ? (
                        <div className="flex-1 flex gap-2">
                            <select
                                value={tempValue.split('-')[1] || '01'}
                                onChange={(e) => {
                                    const parts = tempValue.split('-');
                                    const year = parts[0] || new Date().getFullYear();
                                    setTempValue(`${year}-${e.target.value}-01`);
                                }}
                                className="px-3 py-2 border border-purple-500 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                            >
                                <option value="01">January</option>
                                <option value="02">February</option>
                                <option value="03">March</option>
                                <option value="04">April</option>
                                <option value="05">May</option>
                                <option value="06">June</option>
                                <option value="07">July</option>
                                <option value="08">August</option>
                                <option value="09">September</option>
                                <option value="10">October</option>
                                <option value="11">November</option>
                                <option value="12">December</option>
                            </select>
                            <input
                                type="number"
                                value={tempValue.split('-')[0] || ''}
                                onChange={(e) => {
                                    const parts = tempValue.split('-');
                                    const month = parts[1] || '01';
                                    setTempValue(`${e.target.value}-${month}-01`);
                                }}
                                min="1900"
                                max="2100"
                                placeholder="YYYY"
                                className="w-24 px-3 py-2 border border-purple-500 rounded focus:outline-none focus:ring-2 focus:ring-purple-500"
                                autoFocus
                                onKeyDown={(e) => {
                                    if (e.key === 'Enter') onSave(fieldName, tempValue);
                                    if (e.key === 'Escape') onCancel();
                                }}
                            />
                        </div>
                    ) : (
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
                    <span className="flex-1 text-base text-gray-900">{formatDate(value, normalizedPrecision)}</span>
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
