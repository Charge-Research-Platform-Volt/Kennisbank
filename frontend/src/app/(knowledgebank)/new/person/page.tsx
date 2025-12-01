"use client"

import React from 'react';
import { useRouter } from 'next/navigation';
import { toast } from 'sonner';
import { Spinner } from '@/components/ui/spinner';

export default function NewPersonPage()
{
    const router = useRouter();
    const [isLoading, setIsLoading] = React.useState(false);
    const [isCheckingDuplicate, setIsCheckingDuplicate] = React.useState(false);
    const [duplicateInfo, setDuplicateInfo] = React.useState<{ exists: boolean; id: string } | null>(null);
    const [formData, setFormData] = React.useState({
        name: '',
        occupation: '',
        description: '',
        emailAddress: '',
        linkedin: ''
    });

    // Debounce timer ref
    const checkDuplicateTimerRef = React.useRef<NodeJS.Timeout | null>(null);

    const checkForDuplicate = React.useCallback(async (name: string) => {
        if (!name.trim()) {
            setDuplicateInfo(null);
            return;
        }

        setIsCheckingDuplicate(true);
        try {
            const response = await fetch(`/api/persons/exists?name=${encodeURIComponent(name)}`, {
                credentials: 'include'
            });
            const data = await response.json();

            if (response.ok && data.body) {
                setDuplicateInfo({
                    exists: data.body.exists,
                    id: data.body.id
                });
            }
        } catch (error) {
            console.error('Error checking for duplicate:', error);
        } finally {
            setIsCheckingDuplicate(false);
        }
    }, []);

    const handleInputChange = (field: string, value: string) => {
        setFormData(prev => ({ ...prev, [field]: value }));

        // If name field changed, debounce the duplicate check
        if (field === 'name') {
            // Clear existing timer
            if (checkDuplicateTimerRef.current) {
                clearTimeout(checkDuplicateTimerRef.current);
            }

            // Set new timer
            checkDuplicateTimerRef.current = setTimeout(() => {
                checkForDuplicate(value);
            }, 500); // Wait 500ms after user stops typing
        }
    };

    // Cleanup timer on unmount
    React.useEffect(() => {
        return () => {
            if (checkDuplicateTimerRef.current) {
                clearTimeout(checkDuplicateTimerRef.current);
            }
        };
    }, []);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();

        // Validate required fields
        if (!formData.name.trim()) {
            toast.error('Name is required');
            return;
        }

        // Check for duplicate
        if (duplicateInfo?.exists) {
            toast.error('A person with this name already exists. Please use a different name or view the existing person.');
            return;
        }

        setIsLoading(true);

        try {
            const response = await fetch('/api/persons/new', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                credentials: 'include',
                body: JSON.stringify({
                    Name: formData.name,
                    Occupation: formData.occupation || undefined,
                    Description: formData.description || undefined,
                    EmailAddress: formData.emailAddress || undefined,
                    Linkedin: formData.linkedin || undefined,
                    OrganisationRelations: [],
                    PersonRelations: []
                }),
            });

            const responseData = await response.json();

            if (!response.ok) {
                throw new Error(responseData.message || 'Failed to create person');
            }

            toast.success('Person created successfully!');
            console.log('Person ID:', responseData.body);

            // Navigate to the archive with the new person selected
            router.push(`/archive?id=${responseData.body}`);

        } catch (error) {
            console.error('Error creating person:', error);
            toast.error(`Error creating person: ${error instanceof Error ? error.message : 'Unknown error'}`);
        } finally {
            setIsLoading(false);
        }
    };

    const canSubmit = formData.name.trim().length > 0 && !isLoading && !duplicateInfo?.exists;

    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-3xl py-10">
            <div className="w-full text-center mb-8">
                <h1 className="text-3xl font-semibold text-gray-900 mb-2">
                    Create a New Person
                </h1>
                <p className="text-sm text-gray-600">
                    Add a new person to your knowledge base
                </p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-6">
                {/* Basic Information */}
                <div className="bg-white border border-gray-300 rounded-lg p-6">
                    <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                        Basic Information
                    </h2>
                    <div className="space-y-4">
                        {/* Name */}
                        <div>
                            <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
                                Name <span className="text-red-500">*</span>
                            </label>
                            <input
                                type="text"
                                id="name"
                                value={formData.name}
                                onChange={(e) => handleInputChange('name', e.target.value)}
                                className={`w-full px-3 py-2 border rounded-md focus:outline-none focus:ring-2 focus:border-transparent ${
                                    duplicateInfo?.exists
                                        ? 'border-red-300 focus:ring-red-500'
                                        : 'border-gray-300 focus:ring-purple-500'
                                }`}
                                placeholder="Enter person's name"
                                required
                            />
                            {isCheckingDuplicate && (
                                <p className="text-xs text-gray-500 mt-1 flex items-center gap-1">
                                    <Spinner className="w-3 h-3" />
                                    Checking for duplicates...
                                </p>
                            )}
                            {duplicateInfo?.exists && (
                                <div className="mt-2 p-3 bg-red-50 border border-red-200 rounded-md">
                                    <p className="text-sm text-red-800 font-medium mb-2">
                                        A person with this name already exists
                                    </p>
                                    <button
                                        type="button"
                                        onClick={() => router.push(`/archive?id=${duplicateInfo.id}`)}
                                        className="text-sm text-red-700 underline hover:text-red-900"
                                    >
                                        View existing person
                                    </button>
                                </div>
                            )}
                        </div>

                        {/* Occupation */}
                        <div>
                            <label htmlFor="occupation" className="block text-sm font-medium text-gray-700 mb-1">
                                Occupation
                            </label>
                            <input
                                type="text"
                                id="occupation"
                                value={formData.occupation}
                                onChange={(e) => handleInputChange('occupation', e.target.value)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                                placeholder="e.g., Software Engineer, Professor, etc."
                            />
                        </div>

                        {/* Description */}
                        <div>
                            <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
                                Description
                            </label>
                            <textarea
                                id="description"
                                value={formData.description}
                                onChange={(e) => handleInputChange('description', e.target.value)}
                                rows={4}
                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent resize-none"
                                placeholder="Brief description or biography"
                            />
                        </div>
                    </div>
                </div>

                {/* Contact Information */}
                <div className="bg-white border border-gray-300 rounded-lg p-6">
                    <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                        Contact Information
                    </h2>
                    <div className="space-y-4">
                        {/* Email */}
                        <div>
                            <label htmlFor="emailAddress" className="block text-sm font-medium text-gray-700 mb-1">
                                Email Address
                            </label>
                            <input
                                type="email"
                                id="emailAddress"
                                value={formData.emailAddress}
                                onChange={(e) => handleInputChange('emailAddress', e.target.value)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                                placeholder="email@example.com"
                            />
                        </div>

                        {/* LinkedIn */}
                        <div>
                            <label htmlFor="linkedin" className="block text-sm font-medium text-gray-700 mb-1">
                                LinkedIn
                            </label>
                            <input
                                type="url"
                                id="linkedin"
                                value={formData.linkedin}
                                onChange={(e) => handleInputChange('linkedin', e.target.value)}
                                className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent"
                                placeholder="https://linkedin.com/in/username"
                            />
                        </div>
                    </div>
                </div>

                {/* Action Buttons */}
                <div className="flex justify-center gap-4 pt-4">
                    <button
                        type="button"
                        onClick={() => router.back()}
                        disabled={isLoading}
                        className="px-8 py-3 border-2 border-gray-300 text-gray-700 rounded-lg font-semibold
                        hover:bg-gray-50 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
                    >
                        Cancel
                    </button>
                    <button
                        type="submit"
                        disabled={!canSubmit}
                        className="px-8 py-3 bg-purple-600 text-white rounded-lg font-semibold
                        hover:bg-purple-700 transition-all hover:shadow-lg hover:-translate-y-0.5
                        disabled:bg-gray-300 disabled:cursor-not-allowed disabled:transform-none disabled:shadow-none
                        flex items-center gap-2"
                    >
                        {isLoading && <Spinner className="w-4 h-4" />}
                        {isLoading ? 'Creating...' : 'Create Person'}
                    </button>
                </div>
            </form>
        </div>
    );
}
