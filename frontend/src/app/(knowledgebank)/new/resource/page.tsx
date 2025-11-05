"use client"

import React from 'react';
import FileUploadArea from '@/components/new/FileUploadArea';
import FilePreview from '@/components/new/FilePreview';
import UrlInput from '@/components/new/UrlInput';
import { ProgressBox } from '@/components/progress-box';
import { Spinner } from '@/components/ui/spinner';
import { EditableField, EditableLanguageField, EditableDateField } from '@/components/new/MetadataFields';
import { TagsSection } from '@/components/new/TagsSection';
import { getFileHasher } from '@/utils/fileHashWorker';
import { uploadFileChunked } from '@/actions/fileUploadActions';

class ResourceUploadDto
{
    hash: string = '';
}

export default function NewResourcePage()
{
    const [file, setFile] = React.useState<File | null>(null);
    const [url, setUrl] = React.useState('');
    const [isDragging, setIsDragging] = React.useState(false);
    const [supportedExtensions, setSupportedExtensions] = React.useState<string[]>([]);
    const [isLoading, setIsLoading] = React.useState(true);
    const [phase, setPhase] = React.useState<'select' | 'processing' | 'review'>('select');
    const [progress, setProgress] = React.useState(0);
    const [processingStatus, setProcessingStatus] = React.useState('');
    
    const [progressSteps, setProgressSteps] = React.useState(5);
    const [currentStep, setCurrentStep] = React.useState(1);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const [editableMetadata, setEditableMetadata] = React.useState<any>(null);
    const [editingField, setEditingField] = React.useState<string | null>(null);

    const handleFileSelect = (selectedFile: File) => 
    {
        setFile(selectedFile);
        setUrl('');
    }
    
    const handleUrlChange = (e: React.ChangeEvent<HTMLInputElement>) => 
    {
        setUrl(e.target.value);
    }
    
    const handleRemoveFile = () => 
    {
        setFile(null);
    }

    const handleFileUpload = async () =>
    {
        if (file == null) return;

        setProgressSteps(3);

        const uploadDto: ResourceUploadDto = new ResourceUploadDto();

        // STEP 1: HASH THE FILE
        setProcessingStatus('Hashing file...');
        setProgress(0);
        setCurrentStep(1);

        const fileHasher = getFileHasher();
        const result = await fileHasher.checkDuplicate(file, (progress) => {
            setProgress(Math.round(progress));
        });

        if (result.isDuplicate)
        {
            console.log("This file already exists with ID: " + result.id);

            // TODO: DUPLICATE FILE HANDLING
            setProcessingStatus('File already exists!');

            return;
        }

        // Store hash
        uploadDto.hash = result.hash;
        setProcessingStatus('File hash complete');



        // STEP 2: UPLOAD FILE
        setProcessingStatus("Uploading file...");
        setProgress(0);
        setCurrentStep(2);

        const fileGuid = await uploadFileChunked(file, undefined, setProgress);
        setProcessingStatus("File upload complete");



        // STEP 3: RETRIEVE METADATA
        setProcessingStatus("Extracting metadata...");
        setProgress(0);
        setCurrentStep(3);

        // Fake progress bar: gradually increase to 90% while waiting for API
        const progressInterval = setInterval(() => {
            setProgress((prev) => {
                if (prev >= 90) {
                    clearInterval(progressInterval);
                    return 90;
                }
                return prev + 1;
            });
        }, 100); // Interval time in ms

        try
        {
            const metaResult = await fetch(`/api/ai/extract-metadata/${fileGuid}`, { credentials: 'include' });
            const metadata = await metaResult.json();
            console.log(metadata.body);

            // Clear interval and jump to 100%
            clearInterval(progressInterval);
            setProgress(100);
            setProcessingStatus("Metadata extraction complete");

            // Store metadata and transition to review phase
            setEditableMetadata(metadata.body);
            setTimeout(() => setPhase('review'), 500); // Small delay to show completion
        } catch (error)
        {
            clearInterval(progressInterval);
            throw error;
        }
    }
    
    const handleContinue = async () =>
    {
        if (!file && !url)
            return;

        setPhase('processing')

        if (file)
        {
            console.log("Uploading file: ", file);
            handleFileUpload();
        }
        else if (url)
        {
            console.log("Processing URL: ", url);
            // TODO: URL PROCESSING LOGIC
        }
    };

    const canContinue = file !== null || url.trim().length > 0;

    const handleEditField = (fieldName: string) => {
        setEditingField(fieldName);
    };

    const handleSaveField = (fieldName: string, value: string) => {
        setEditableMetadata({ ...editableMetadata, [fieldName]: value });
        setEditingField(null);
    };

    const handleCancelEdit = () => {
        setEditingField(null);
    };

    const handleRemoveTag = (tagToRemove: string) => {
        setEditableMetadata({
            ...editableMetadata,
            tags: editableMetadata.tags.filter((tag: string) => tag !== tagToRemove)
        });
    };

    const handleAddTag = (newTag: string) => {
        if (!newTag.trim()) return;
        const tags = editableMetadata.tags || [];
        if (!tags.includes(newTag.trim())) {
            setEditableMetadata({
                ...editableMetadata,
                tags: [...tags, newTag.trim()]
            });
        }
    };

    React.useEffect(() => 
    {
        const fetchExtensions = async () => 
        {
            try 
            {
                const response = await fetch('/api/resources/supported_extensions', { credentials: 'include' });
                const data = await response.json();
                setSupportedExtensions(data.body.document);
            }
            catch (error) 
            {
                console.error('Failed to fetch extensions: ', error);
                // Fallback to default extensions
                setSupportedExtensions(['.pdf', '.doc', '.docx', '.txt', '.png', '.jpg']);
            }
            finally 
            {
                setIsLoading(false);
            }
        }
        
        fetchExtensions();
    }, []);
    
    if (isLoading)
    {
        return (
            <div className="flex items-center justify-center min-h-[100vh]">
                <Spinner className="w-[3rem] h-[3rem] text-gray-400" />
            </div>
        );
    }
    
    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-6xl py-10">
            {/* Show selection UI */}
            { phase == 'select' && 
                <>
                    <div className="w-full text-center mb-8">
                        <h1 className="text-3xl font-semibold text-gray-900 mb-2">
                            Create a New Resource
                        </h1>
                        <p className="text-sm text-gray-600">
                            Upload a file or provide a URL to automatically extract metadata
                        </p>
                    </div>

                    {/* File Upload Area */}
                    {!file && (
                        <FileUploadArea
                            isDragging={isDragging}
                            setIsDragging={setIsDragging}
                            onFileSelect={handleFileSelect}
                            acceptedExtensions={supportedExtensions}
                        />
                    )}

                    {/* File Preview */}
                    {file && (
                        <FilePreview file={file} onRemove={handleRemoveFile} />
                    )}

                    {/* Divider */}
                    <div className="flex items-center my-6">
                        <div className="flex-1 border-t border-gray-300"></div>
                        <span className="px-4 text-sm text-gray-500">or</span>
                        <div className="flex-1 border-t border-gray-300"></div>
                    </div>

                    {/* URL Input */}
                    <UrlInput
                        value={url}
                        onChange={handleUrlChange}
                        disabled={file !== null}
                    />

                    {/* Continue Button */}
                    <div className="mt-8 flex justify-center">
                        <button
                            onClick={handleContinue}
                            disabled={!canContinue}
                            className="bg-purple-600 text-white px-20 py-3 rounded-lg font-semibold 
                            hover:bg-purple-700 disabled:bg-gray-300 disabled:cursor-not-allowed
                            transition-all hover:shadow-lg hover:-translate-y-0.5
                            disabled:transform-none disabled:shadow-none"
                        >
                            Continue
                        </button>
                    </div>
                </>
            }

            {/** Show progress bar of processing the file */}
            { phase == 'processing' &&
                <div className="flex items-center min-h-[80vh]">
                    <ProgressBox title="Processing resource" value={progress} subtext={processingStatus} currentStep={currentStep} maxSteps={progressSteps} />
                </div>
            }

            {/** Show all extracted metadata so the user can edit it */}
            {phase == 'review' && editableMetadata && (
                <div className="space-y-6">
                    {/* Header */}
                    <div className="text-center mb-8">
                        <h1 className="text-3xl font-semibold text-gray-900 mb-2">
                            Review Extracted Metadata
                        </h1>
                        <p className="text-sm text-gray-600">
                            Review and edit the AI-extracted metadata before saving
                        </p>
                    </div>

                    {/* Masonry-style layout for cards */}
                    <div className="columns-1 md:columns-2 gap-6">
                        {/* Basic Information Section */}
                        <div className="bg-white border border-gray-300 rounded-lg p-6 break-inside-avoid mb-6">
                            <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                Basic Information
                            </h2>
                            <div className="space-y-4">
                                <EditableField
                                    fieldName="title"
                                    label="Title *"
                                    value={editableMetadata.title}
                                    editingField={editingField}
                                    onEdit={handleEditField}
                                    onSave={handleSaveField}
                                    onCancel={handleCancelEdit}
                                />
                                <EditableField
                                    fieldName="description"
                                    label="Description"
                                    value={editableMetadata.description}
                                    multiline
                                    editingField={editingField}
                                    onEdit={handleEditField}
                                    onSave={handleSaveField}
                                    onCancel={handleCancelEdit}
                                />
                                <EditableLanguageField
                                    fieldName="languageCode"
                                    label="Language"
                                    value={editableMetadata.languageCode}
                                    editingField={editingField}
                                    onEdit={handleEditField}
                                    onSave={handleSaveField}
                                    onCancel={handleCancelEdit}
                                />
                            </div>
                        </div>

                        {/* Publication Details Section */}
                        <div className="bg-white border border-gray-300 rounded-lg p-6 break-inside-avoid mb-6">
                            <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                Publication Details
                            </h2>
                            <div className="space-y-4">
                                <EditableDateField
                                    fieldName="publicationDate"
                                    label="Publication Date"
                                    value={editableMetadata.publicationDate}
                                    editingField={editingField}
                                    onEdit={handleEditField}
                                    onSave={handleSaveField}
                                    onCancel={handleCancelEdit}
                                />
                                <EditableField
                                    fieldName="publicationCode"
                                    label="Publication Code"
                                    value={editableMetadata.publicationCode}
                                    editingField={editingField}
                                    onEdit={handleEditField}
                                    onSave={handleSaveField}
                                    onCancel={handleCancelEdit}
                                />
                                <EditableField
                                    fieldName="license"
                                    label="License"
                                    value={editableMetadata.license}
                                    editingField={editingField}
                                    onEdit={handleEditField}
                                    onSave={handleSaveField}
                                    onCancel={handleCancelEdit}
                                />
                            </div>
                        </div>

                        {/* Authors */}
                        <div className="bg-white border border-gray-300 rounded-lg p-6 break-inside-avoid mb-6">
                            <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                Authors
                            </h2>
                            <div className="space-y-2">
                                {/* TODO: Map through authors */}
                                <p className="text-sm text-gray-500">No authors added</p>
                                <button className="text-purple-600 hover:text-purple-700 text-sm font-medium">
                                    + Add Author
                                </button>
                            </div>
                        </div>

                        {/* Tags */}
                        <div className="bg-white border border-gray-300 rounded-lg p-6 break-inside-avoid mb-6">
                            <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                Tags
                            </h2>
                            <TagsSection
                                tags={editableMetadata?.tags || []}
                                onAddTag={handleAddTag}
                                onRemoveTag={handleRemoveTag}
                            />
                        </div>

                        {/* Type-Specific Metadata */}
                        <div className="bg-white border border-gray-300 rounded-lg p-6 break-inside-avoid mb-6">
                            <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                Document Metadata
                            </h2>
                            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                                {/* TODO: Add type-specific fields based on resource type */}
                                <EditableField
                                    fieldName="sourceUrl"
                                    label="Source URL"
                                    value={editableMetadata.sourceUrl}
                                    editingField={editingField}
                                    onEdit={handleEditField}
                                    onSave={handleSaveField}
                                    onCancel={handleCancelEdit}
                                />
                            </div>
                        </div>
                    </div>

                    {/* Action Buttons */}
                    <div className="flex justify-center gap-4 pt-4">
                        <button
                            onClick={() => setPhase('select')}
                            className="px-8 py-3 border-2 border-gray-300 text-gray-700 rounded-lg font-semibold
                            hover:bg-gray-50 transition-all"
                        >
                            Cancel
                        </button>
                        <button
                            onClick={() => {
                                // TODO: Handle save
                                console.log('Saving resource with metadata:', editableMetadata);
                            }}
                            className="px-8 py-3 bg-purple-600 text-white rounded-lg font-semibold
                            hover:bg-purple-700 transition-all hover:shadow-lg hover:-translate-y-0.5"
                        >
                            Save Resource
                        </button>
                    </div>
                </div>
            )}
        </div>
    );
}