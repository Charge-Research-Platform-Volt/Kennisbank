"use client"

import React, { JSX } from 'react';
import { Input } from "@/components/ui/input";
import { format, parseISO } from 'date-fns';

interface EditableDateProps {
    date: Date | null;
    text: string;
    icon: JSX.Element;
    cName: string;
    editMode: boolean;
    onDateChange?: (date: Date | null) => void;
}

export default function EditableDate({
    date,
    text,
    icon,
    cName,
    editMode,
    onDateChange
}: EditableDateProps) {
    const formatDate = (date: Date | null) => {
        if (!date) return '';
        return format(new Date(date), 'MMM d, yyyy');
    };

    const handleDateChange = (e: React.ChangeEvent<HTMLInputElement>) => {
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
            <div className={cName}>
                {icon}
                <div className="flex flex-col min-w-0">
                    <span className="text-gray-700 text-sm">{text}</span>
                    <Input
                        type="date"
                        value={getInputValue()}
                        onChange={handleDateChange}
                        className="h-8 text-sm font-medium"
                    />
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
                    {formatDate(date)}
                </div>
            </div>
        </div>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
