"use client"

import React, { JSX } from 'react';
import { Input } from "@/components/ui/input";
import { format, parseISO } from 'date-fns';

// Helper to normalize precision (handle both string and numeric enum values)
const normalizePrecision = (precision?: 'Year' | 'Month' | 'Day' | number | null): 'Year' | 'Month' | 'Day' => {
    if (precision === 0 || precision === 'Year') return 'Year';
    if (precision === 1 || precision === 'Month') return 'Month';
    if (precision === 2 || precision === 'Day') return 'Day';
    return 'Day'; // default
};

interface EditableDateProps {
    date: Date | null;
    text: string;
    icon: JSX.Element;
    cName: string;
    editMode: boolean;
    precision?: 'Year' | 'Month' | 'Day' | number | null;
    onDateChange?: (date: Date | null) => void;
    onPrecisionChange?: (precision: 'Year' | 'Month' | 'Day') => void;
}

export default function EditableDate({
    date,
    text,
    icon,
    cName,
    editMode,
    precision,
    onDateChange,
    onPrecisionChange
}: EditableDateProps) {
    const normalizedPrecision = normalizePrecision(precision);
    const [localPrecision, setLocalPrecision] = React.useState(normalizedPrecision);

    React.useEffect(() => {
        setLocalPrecision(normalizedPrecision);
    }, [normalizedPrecision]);

    const handlePrecisionChange = (newPrecision: 'Year' | 'Month' | 'Day') => {
        setLocalPrecision(newPrecision);
        if (onPrecisionChange) {
            onPrecisionChange(newPrecision);
        }
    };

    const formatDate = (date: Date | null, datePrecision: 'Year' | 'Month' | 'Day') => {
        if (!date) return 'Unknown';
        const d = new Date(date);

        if (datePrecision === 'Year') {
            return d.getFullYear().toString();
        } else if (datePrecision === 'Month') {
            return format(d, 'MMMM yyyy');
        } else {
            return format(d, 'MMM d, yyyy');
        }
    };

    const handleDateChange = (e: React.ChangeEvent<HTMLInputElement> | { target: { value: string } }) => {
        const newValue = e.target.value;
        if (newValue && onDateChange) {
            try {
                // Parse the date from the input (YYYY-MM-DD format)
                const parsedDate = parseISO(newValue);
                onDateChange(parsedDate);
            } catch (error) {
                console.error('Invalid date format:', error);
            }
        } else if (!newValue && onDateChange) {
            // If the date is cleared, set it to null
            onDateChange(null);
        }
    };

    const handleYearChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const year = e.target.value;
        if (!onDateChange) return;

        if (!year) {
            // If year is cleared, set date to null
            onDateChange(null);
            return;
        }

        // Only update if we have a valid 4-digit year
        const yearNum = parseInt(year, 10);
        if (year.length === 4 && !isNaN(yearNum) && yearNum >= 1900 && yearNum <= 2100) {
            handleDateChange({ target: { value: `${year}-01-01` } });
        }
    };

    const handleMonthChange = (month: string, year: string) => {
        if (!onDateChange) return;

        if (!month || !year) {
            // If incomplete, set to null
            onDateChange(null);
            return;
        }

        // Validate year
        const yearNum = parseInt(year, 10);
        if (year.length === 4 && !isNaN(yearNum) && yearNum >= 1900 && yearNum <= 2100) {
            handleDateChange({ target: { value: `${year}-${month}-01` } });
        }
    };

    const getInputValue = () => {
        if (!date) return '';
        try {
            // Format date to YYYY-MM-DD for the input field
            return format(new Date(date), 'yyyy-MM-dd');
        } catch {
            return '';
        }
    };

    if (editMode) {
        return (
            <div className="flex flex-col gap-2 min-w-0">
                <div className="flex items-center justify-between">
                    <div className="flex items-center gap-2">
                        {icon}
                        <span className="text-gray-700 text-sm">{text}</span>
                    </div>
                    <div className="flex gap-1 text-xs">
                        {date && (
                            <button
                                type="button"
                                onClick={() => onDateChange && onDateChange(null)}
                                className="px-2 py-1 rounded bg-red-100 text-red-700 hover:bg-red-200"
                                title="Clear publication date"
                            >
                                Clear
                            </button>
                        )}
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
                </div>
                <div className="flex flex-col min-w-0">
                    {localPrecision === 'Year' ? (
                        <Input
                            type="number"
                            value={date ? new Date(date).getFullYear() : ''}
                            onChange={handleYearChange}
                            min="1900"
                            max="2100"
                            placeholder="YYYY"
                            className="h-8 text-sm font-medium"
                        />
                    ) : localPrecision === 'Month' ? (
                        <div className="flex gap-2">
                            <select
                                value={date ? String(new Date(date).getMonth() + 1).padStart(2, '0') : '01'}
                                onChange={(e) => {
                                    const year = date ? String(new Date(date).getFullYear()) : String(new Date().getFullYear());
                                    handleMonthChange(e.target.value, year);
                                }}
                                className="flex-1 h-8 text-sm font-medium px-2 border rounded"
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
                            <Input
                                type="number"
                                value={date ? new Date(date).getFullYear() : ''}
                                onChange={(e) => {
                                    const month = date ? String(new Date(date).getMonth() + 1).padStart(2, '0') : '01';
                                    handleMonthChange(month, e.target.value);
                                }}
                                min="1900"
                                max="2100"
                                placeholder="YYYY"
                                className="w-24 h-8 text-sm font-medium"
                            />
                        </div>
                    ) : (
                        <Input
                            type="date"
                            value={getInputValue()}
                            onChange={handleDateChange}
                            className="h-8 text-sm font-medium"
                        />
                    )}
                </div>
            </div>
        );
    }

    return (
        <div className={cName}>
            {icon}
            <div className="truncate">
                <span className="text-gray-700">{text}</span>
                <div className="font-medium text-gray-700">
                    {formatDate(date, normalizedPrecision)}
                </div>
            </div>
        </div>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
