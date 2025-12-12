"use client"

import React from 'react';
import FileUploadArea from '@/components/new/FileUploadArea';
import FilePreview from '@/components/new/FilePreview';
import UrlInput from '@/components/new/UrlInput';
import { ProgressBox } from '@/components/progress-box';
import { Spinner } from '@/components/ui/spinner';
import { EditableField, EditableLanguageField, EditableDateField } from '@/components/new/MetadataFields';
import { TagsSection } from '@/components/new/TagsSection';
import { EntitySelectionSection } from '@/components/new/EntitySelectionSection';
import { getFileHasher } from '@/utils/fileHashWorker';
import { uploadFileChunked } from '@/actions/fileUploadActions';
import { ExtractedMetadata, EntitySelection } from '@/types/extractedMetadata.type';
import { DocumentCreateDtoSchema, type DocumentCreateDto } from '@/types/uploadTypes';
import { toast } from 'sonner';
import { useRouter } from 'next/navigation';

class ResourceUploadDto
{
    hash: string = '';
}

// Helper function to poll job status
async function pollJobStatus(
    jobId: string,
    setProgress: (progress: number) => void,
    setProcessingStatus: (status: string) => void
): Promise<ExtractedMetadata> {
    const pollInterval = 2000; // Poll every 2 seconds
    const maxAttempts = 150; // Max 5 minutes (150 * 2 seconds = 300 seconds)

    for (let attempt = 0; attempt < maxAttempts; attempt++) {
        const statusResult = await fetch(`/api/ai/extract-metadata/status/${jobId}`, {
            credentials: 'include'
        });

        if (!statusResult.ok) {
            throw new Error('Failed to get job status');
        }

        const statusData = await statusResult.json();
        const job = statusData.body;

        // Update progress and status message
        setProgress(job.progressPercentage || 0);
        if (job.statusMessage) {
            setProcessingStatus(job.statusMessage);
        }

        // Check if job is complete
        if (job.status === 'Completed') {
            console.log('Metadata extraction complete:', job.result);
            return job.result as ExtractedMetadata;
        }

        // Check if job failed
        if (job.status === 'Failed') {
            throw new Error(job.errorMessage || 'Metadata extraction failed');
        }

        // Wait before next poll
        await new Promise(resolve => setTimeout(resolve, pollInterval));
    }

    throw new Error('Metadata extraction timed out');
}

export default function NewResourcePage()
{
    const router = useRouter();

    const [file, setFile] = React.useState<File | null>(null);
    const [url, setUrl] = React.useState('');
    const [isDragging, setIsDragging] = React.useState(false);
    const [supportedExtensions, setSupportedExtensions] = React.useState<string[]>([]);
    const [isLoading, setIsLoading] = React.useState(true);
    const [phase, setPhase] = React.useState<'select' | 'processing' | 'review' | 'duplicate'>('select');
    const [progress, setProgress] = React.useState(0);
    const [processingStatus, setProcessingStatus] = React.useState('');

    const [progressSteps, setProgressSteps] = React.useState(5);
    const [currentStep, setCurrentStep] = React.useState(1);
    const [editableMetadata, setEditableMetadata] = React.useState<ExtractedMetadata | null>(null);
    const [editingField, setEditingField] = React.useState<string | null>(null);
    const [authorSelections, setAuthorSelections] = React.useState<Map<string, EntitySelection>>(new Map());
    const [organisationSelections, setOrganisationSelections] = React.useState<Map<string, EntitySelection>>(new Map());
    const [relatedPersonSelections, setRelatedPersonSelections] = React.useState<Map<string, EntitySelection>>(new Map());
    const [uploadedFileId, setUploadedFileId] = React.useState<string | null>(null);
    const [fileHash, setFileHash] = React.useState<string | null>(null);
    const [duplicateResourceId, setDuplicateResourceId] = React.useState<string | null>(null);
    const [duplicateType, setDuplicateType] = React.useState<'file' | 'url' | null>(null);

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

    const handleReset = () =>
    {
        setFile(null);
        setUrl('');
        setPhase('select');
        setProgress(0);
        setProcessingStatus('');
        setDuplicateResourceId(null);
        setDuplicateType(null);
        setEditableMetadata(null);
        setUploadedFileId(null);
        setFileHash(null);
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

            // Store duplicate info and show duplicate UI
            setDuplicateResourceId(result.id);
            setDuplicateType('file');
            setPhase('duplicate');

            return;
        }

        // Store hash
        uploadDto.hash = result.hash;
        setFileHash(result.hash);
        setProcessingStatus('File hash complete');



        // STEP 2: UPLOAD FILE
        setProcessingStatus("Uploading file...");
        setProgress(0);
        setCurrentStep(2);

        const fileGuid = await uploadFileChunked(file, undefined, setProgress);
        setUploadedFileId(fileGuid);
        setProcessingStatus("File upload complete");



        // STEP 3: RETRIEVE METADATA
        setProcessingStatus("Extracting metadata...");
        setProgress(0);
        setCurrentStep(3);

        try
        {
            // Start the extraction job
            const startResult = await fetch(`/api/ai/extract-metadata/start?type=file&value=${encodeURIComponent(fileGuid)}`, {
                method: 'POST',
                credentials: 'include'
            });

            if (!startResult.ok) {
                throw new Error('Failed to start metadata extraction');
            }

            const startData = await startResult.json();
            const jobId = startData.body.jobId;
            console.log('Metadata extraction job started:', jobId);

            // Poll for job status
            const metadata = await pollJobStatus(jobId, setProgress, setProcessingStatus);

            // Store metadata and transition to review phase
            setEditableMetadata(metadata);

            // Initialize default author selections
            const defaultAuthorSelections = new Map<string, EntitySelection>();
            metadata.authors?.forEach((author: { name: string; type: string; similars: Array<{ id: string; score: number }> }) => {
                // If there's a high-confidence match (>90%), auto-select it
                const bestMatch = author.similars?.[0]; // Similars are already sorted by score
                if (bestMatch && bestMatch.score >= 0.9) {
                    defaultAuthorSelections.set(author.name, {
                        extractedName: author.name,
                        action: 'use_existing',
                        existingId: bestMatch.id,
                        type: author.type
                    });
                } else {
                    defaultAuthorSelections.set(author.name, {
                        extractedName: author.name,
                        action: 'create',
                        type: author.type
                    });
                }
            });
            setAuthorSelections(defaultAuthorSelections);

            // Initialize default organisation selections
            const defaultOrgSelections = new Map<string, EntitySelection>();
            metadata.organisations?.forEach((org: { name: string; similars: Array<{ id: string; score: number }> }) => {
                const bestMatch = org.similars?.[0];
                if (bestMatch && bestMatch.score >= 0.9) {
                    defaultOrgSelections.set(org.name, {
                        extractedName: org.name,
                        action: 'use_existing',
                        existingId: bestMatch.id
                    });
                } else {
                    defaultOrgSelections.set(org.name, {
                        extractedName: org.name,
                        action: 'create'
                    });
                }
            });
            setOrganisationSelections(defaultOrgSelections);

            // Initialize default related person selections
            const defaultRelatedPersonSelections = new Map<string, EntitySelection>();
            metadata.relatedPersons?.forEach((person: { name: string; similars: Array<{ id: string; score: number }> }) => {
                const bestMatch = person.similars?.[0];
                if (bestMatch && bestMatch.score >= 0.9) {
                    defaultRelatedPersonSelections.set(person.name, {
                        extractedName: person.name,
                        action: 'use_existing',
                        existingId: bestMatch.id
                    });
                } else {
                    defaultRelatedPersonSelections.set(person.name, {
                        extractedName: person.name,
                        action: 'create'
                    });
                }
            });
            setRelatedPersonSelections(defaultRelatedPersonSelections);

            setTimeout(() => setPhase('review'), 500); // Small delay to show completion
        } catch (error)
        {
            throw error;
        }
    }
    
    const handleWebUpload = async () => 
    {
        if (url == "") return;
        
        setProgressSteps(1);
        
        // STEP 1: CHECK DUPLICATE (is nearly instant, so no progress bar)
        const response = await fetch(`/api/resources/exists?url=${encodeURIComponent(url)}`, { credentials: 'include' });
        
        if (!response.ok) 
        {
            // TODO: HANDLE ERROR
            return;
        }
        
        const data = await response.json();
        
        if (data.body.exists)
        {
            console.log("This webpage already exists with ID: " + data.body.id);

            // Store duplicate info and show duplicate UI
            setDuplicateResourceId(data.body.id);
            setDuplicateType('url');
            setPhase('duplicate');

            return;
        }
        
        // STEP 2: RETRIEVE METADATA
        setProcessingStatus("Extracting metadata...");
        setProgress(0);
        setCurrentStep(1);

        try
        {
            // Start the extraction job
            const startResult = await fetch(`/api/ai/extract-metadata/start?type=web&value=${encodeURIComponent(url)}`, {
                method: 'POST',
                credentials: 'include'
            });

            if (!startResult.ok) {
                throw new Error('Failed to start metadata extraction');
            }

            const startData = await startResult.json();
            const jobId = startData.body.jobId;
            console.log('Metadata extraction job started:', jobId);

            // Poll for job status
            const metadata = await pollJobStatus(jobId, setProgress, setProcessingStatus);

            // Store metadata and transition to review phase
            setEditableMetadata(metadata);
            
            // Initialize default author selections
            const defaultAuthorSelections = new Map<string, EntitySelection>();
            metadata.authors?.forEach((author: { name: string; type: string; similars: Array<{ id: string; score: number }> }) => {
                // If there's a high-confidence match (>90%), auto-select it
                const bestMatch = author.similars?.[0]; // Similars are already sorted by score
                if (bestMatch && bestMatch.score >= 0.9) {
                    defaultAuthorSelections.set(author.name, {
                        extractedName: author.name,
                        action: 'use_existing',
                        existingId: bestMatch.id,
                        type: author.type
                    });
                } else {
                    defaultAuthorSelections.set(author.name, {
                        extractedName: author.name,
                        action: 'create',
                        type: author.type
                    });
                }
            });
            setAuthorSelections(defaultAuthorSelections);

            // Initialize default organisation selections
            const defaultOrgSelections = new Map<string, EntitySelection>();
            metadata.organisations?.forEach((org: { name: string; similars: Array<{ id: string; score: number }> }) => {
                const bestMatch = org.similars?.[0];
                if (bestMatch && bestMatch.score >= 0.9) {
                    defaultOrgSelections.set(org.name, {
                        extractedName: org.name,
                        action: 'use_existing',
                        existingId: bestMatch.id
                    });
                } else {
                    defaultOrgSelections.set(org.name, {
                        extractedName: org.name,
                        action: 'create'
                    });
                }
            });
            setOrganisationSelections(defaultOrgSelections);

            // Initialize default related person selections
            const defaultRelatedPersonSelections = new Map<string, EntitySelection>();
            metadata.relatedPersons?.forEach((person: { name: string; similars: Array<{ id: string; score: number }> }) => {
                const bestMatch = person.similars?.[0];
                if (bestMatch && bestMatch.score >= 0.9) {
                    defaultRelatedPersonSelections.set(person.name, {
                        extractedName: person.name,
                        action: 'use_existing',
                        existingId: bestMatch.id
                    });
                } else {
                    defaultRelatedPersonSelections.set(person.name, {
                        extractedName: person.name,
                        action: 'create'
                    });
                }
            });
            setRelatedPersonSelections(defaultRelatedPersonSelections);

            setTimeout(() => setPhase('review'), 500); // Small delay to show completion
        } catch (error)
        {
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
            handleWebUpload();
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

    const handlePrecisionChange = (precision: 'Year' | 'Month' | 'Day') => {
        setEditableMetadata({ ...editableMetadata, publicationDatePrecision: precision });
    };

    const handleCancelEdit = () => {
        setEditingField(null);
    };

    const handleRemoveTag = (tagToRemove: string) => {
        if (!editableMetadata) return;
        setEditableMetadata({
            ...editableMetadata,
            tags: (editableMetadata.tags || []).filter((tag: string) => tag !== tagToRemove)
        });
    };

    const handleAddTag = (newTag: string) => {
        if (!newTag.trim()) return;
        const tags = editableMetadata?.tags || [];
        if (!tags.includes(newTag.trim())) {
            setEditableMetadata({
                ...editableMetadata!,
                tags: [...tags, newTag.trim()]
            });
        }
    };

    const handleAuthorSelectionChange = (authorName: string, selection: EntitySelection) => {
        setAuthorSelections((prev) => {
            const newSelections = new Map(prev);
            newSelections.set(authorName, selection);
            return newSelections;
        });
    };

    const handleOrganisationSelectionChange = (orgName: string, selection: EntitySelection) => {
        setOrganisationSelections((prev) => {
            const newSelections = new Map(prev);
            newSelections.set(orgName, selection);
            return newSelections;
        });
    };

    const handleRelatedPersonSelectionChange = (personName: string, selection: EntitySelection) => {
        setRelatedPersonSelections((prev) => {
            const newSelections = new Map(prev);
            newSelections.set(personName, selection);
            return newSelections;
        });
    };

    const handleRemoveAuthor = (authorName: string) => {
        if (!editableMetadata) return;
        setEditableMetadata({
            ...editableMetadata,
            authors: editableMetadata.authors?.filter(a => a.name !== authorName)
        });
        setAuthorSelections((prev) => {
            const newSelections = new Map(prev);
            newSelections.delete(authorName);
            return newSelections;
        });
    };

    const handleRemoveOrganisation = (orgName: string) => {
        if (!editableMetadata) return;
        setEditableMetadata({
            ...editableMetadata,
            organisations: editableMetadata.organisations?.filter(o => o.name !== orgName)
        });
        setOrganisationSelections((prev) => {
            const newSelections = new Map(prev);
            newSelections.delete(orgName);
            return newSelections;
        });
    };

    const handleRemoveRelatedPerson = (personName: string) => {
        if (!editableMetadata) return;
        setEditableMetadata({
            ...editableMetadata,
            relatedPersons: editableMetadata.relatedPersons?.filter(p => p.name !== personName)
        });
        setRelatedPersonSelections((prev) => {
            const newSelections = new Map(prev);
            newSelections.delete(personName);
            return newSelections;
        });
    };

    const handleSave = async () => {
        if (!editableMetadata || !uploadedFileId || !fileHash || !file) {
            toast.error("Missing required data. Please try uploading again.");
            return;
        }

        // Convert author selections to Authors array with type information
        const authors: Array<{ value: string; type?: string }> = [];
        for (const [authorName, selection] of authorSelections.entries()) {
            if (selection.action === 'use_existing' && selection.existingId) {
                // For existing entities, just send the GUID (type not needed, backend can look it up)
                authors.push({ value: selection.existingId });
            } else {
                // For new entities, send the name and type so backend knows what to create
                authors.push({ value: authorName, type: selection.type });
            }
        }

        // Convert organisation selections to Organisations array (GUIDs or names)
        const organisations: string[] = [];
        for (const [orgName, selection] of organisationSelections.entries()) {
            if (selection.action === 'use_existing' && selection.existingId) {
                organisations.push(selection.existingId); // Use existing GUID
            } else {
                organisations.push(orgName); // Use name (backend will create new organisation)
            }
        }

        // Convert related person selections to RelatedPersons array (GUIDs or names)
        const relatedPersons: string[] = [];
        for (const [personName, selection] of relatedPersonSelections.entries()) {
            if (selection.action === 'use_existing' && selection.existingId) {
                relatedPersons.push(selection.existingId); // Use existing GUID
            } else {
                relatedPersons.push(personName); // Use name (backend will create new person)
            }
        }

        // Get file extension
        const fileExtension = file.name.split('.').pop() || '';

        // Default to "Unknown" resource type (matches backend DatabaseSeeder.UnknownResourceTypeId)
        const UNKNOWN_TYPE_ID = '0cc285a8-0f07-11f0-a0a6-5600051f1387';

        // Convert publication date to ISO datetime format (if provided)
        let publicationDate: string | undefined = undefined;
        let publicationDatePrecision: 'Year' | 'Month' | 'Day' | undefined = undefined;

        if (editableMetadata.publicationDate) {
            const dateObj = new Date(editableMetadata.publicationDate);
            if (!isNaN(dateObj.getTime())) {
                publicationDate = dateObj.toISOString();

                // Convert numeric precision to string enum
                const p = editableMetadata.publicationDatePrecision;
                if (p === 0 || p === 'Year') publicationDatePrecision = 'Year';
                else if (p === 1 || p === 'Month') publicationDatePrecision = 'Month';
                else if (p === 2 || p === 'Day') publicationDatePrecision = 'Day';
                else publicationDatePrecision = 'Day';
            }
        }

        // Build the DTO
        const dto: DocumentCreateDto = {
            // Required fields
            Title: editableMetadata.title || '',
            TypeId: UNKNOWN_TYPE_ID, // Default to "Unknown" type
            LanguageCode: editableMetadata.languageCode || '',
            PublicationDate: publicationDate,
            PublicationDatePrecision: publicationDatePrecision,

            // File-specific fields
            Id: uploadedFileId,
            Hash: fileHash,
            FileExtension: fileExtension,

            // Authors (GUIDs or names)
            Authors: authors,

            // Optional metadata fields
            Description: editableMetadata.description,
            PublicationCode: editableMetadata.publicationCode,
            License: editableMetadata.license,
            SourceUrl: editableMetadata.sourceUrl,
            Tags: editableMetadata.tags || [],
            Abstract: editableMetadata.abstract,

            // Optional relations
            Organisations: organisations,
            Regions: [],
            RelatedPersons: relatedPersons,
        };

        // Validate with Zod
        const result = DocumentCreateDtoSchema.safeParse(dto);

        if (!result.success) {
            // Show first validation error
            const firstError = result.error.issues[0];
            toast.error(`Validation error: ${firstError.message} (${firstError.path.join('.')})`);
            console.error('Validation errors:', result.error.issues);
            return;
        }

        // Send to backend
        try {
            setPhase('processing');
            setProcessingStatus('Saving resource...');

            const response = await fetch('/api/resources/new', {
                method: 'PUT',
                headers: {
                    'Content-Type': 'application/json',
                },
                credentials: 'include',
                body: JSON.stringify({ uploadType: 'document', ...result.data }),
            });

            const responseData = await response.json();

            if (!response.ok) {
                throw new Error(responseData.message || 'Failed to save resource');
            }
            
            // Success!
            toast.success('Resource saved successfully!');
            console.log('Resource ID:', responseData.body);

            router.push(`/archive?id=${responseData.body}`);

        } catch (error) {
            console.error('Error saving resource:', error);
            toast.error(`Error saving resource: ${error instanceof Error ? error.message : 'Unknown error'}`);
            setPhase('review');
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

            {/** Show duplicate warning */}
            { phase == 'duplicate' && duplicateResourceId &&
                <div className="flex items-center justify-center min-h-[60vh]">
                    <div className="max-w-lg w-full">
                        <div className="bg-white border-2 border-orange-300 rounded-lg p-8 shadow-lg">
                            <div className="flex flex-col items-center text-center">
                                {/* Icon */}
                                <div className="w-16 h-16 bg-orange-100 rounded-full flex items-center justify-center mb-4">
                                    <svg className="w-8 h-8 text-orange-600" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01m-6.938 4h13.856c1.54 0 2.502-1.667 1.732-3L13.732 4c-.77-1.333-2.694-1.333-3.464 0L3.34 16c-.77 1.333.192 3 1.732 3z" />
                                    </svg>
                                </div>

                                {/* Title */}
                                <h2 className="text-2xl font-semibold text-gray-900 mb-2">
                                    {duplicateType === 'file' ? 'File Already Exists' : 'Webpage Already Exists'}
                                </h2>

                                {/* Message */}
                                <p className="text-gray-600 mb-6">
                                    {duplicateType === 'file'
                                        ? 'This file has already been uploaded to your knowledge base.'
                                        : 'This webpage has already been added to your knowledge base.'}
                                </p>

                                {/* Action Buttons */}
                                <div className="flex flex-col sm:flex-row gap-3 w-full">
                                    <button
                                        onClick={() => router.push(`/archive?id=${duplicateResourceId}`)}
                                        className="flex-1 px-6 py-3 bg-purple-600 text-white rounded-lg font-semibold
                                        hover:bg-purple-700 transition-all hover:shadow-lg hover:-translate-y-0.5"
                                    >
                                        View Existing Resource
                                    </button>
                                    <button
                                        onClick={handleReset}
                                        className="flex-1 px-6 py-3 border-2 border-gray-300 text-gray-700 rounded-lg font-semibold
                                        hover:bg-gray-50 transition-all"
                                    >
                                        Try Another {duplicateType === 'file' ? 'File' : 'URL'}
                                    </button>
                                </div>
                            </div>
                        </div>
                    </div>
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

                    {/* Two-column layout: Metadata (left) | Entities (right) */}
                    <div className="flex flex-col md:flex-row gap-6 items-start">
                        {/* Left Column: All Metadata */}
                        <div className="flex-1 w-full space-y-6">
                            {/* Basic Information */}
                            <div className="bg-white border border-gray-300 rounded-lg p-6">
                                <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                    Basic Information
                                </h2>
                                <div className="space-y-4">
                                    <EditableField
                                        fieldName="title"
                                        label="Title *"
                                        value={editableMetadata.title || ''}
                                        editingField={editingField}
                                        onEdit={handleEditField}
                                        onSave={handleSaveField}
                                        onCancel={handleCancelEdit}
                                    />
                                    <EditableField
                                        fieldName="description"
                                        label="Description"
                                        value={editableMetadata.description || ''}
                                        multiline
                                        editingField={editingField}
                                        onEdit={handleEditField}
                                        onSave={handleSaveField}
                                        onCancel={handleCancelEdit}
                                    />
                                    <EditableLanguageField
                                        fieldName="languageCode"
                                        label="Language"
                                        value={editableMetadata.languageCode || ''}
                                        editingField={editingField}
                                        onEdit={handleEditField}
                                        onSave={handleSaveField}
                                        onCancel={handleCancelEdit}
                                    />
                                </div>
                            </div>

                            {/* Publication Details */}
                            <div className="bg-white border border-gray-300 rounded-lg p-6">
                                <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                    Publication Details
                                </h2>
                                <div className="space-y-4">
                                    <EditableDateField
                                        fieldName="publicationDate"
                                        label="Publication Date"
                                        value={editableMetadata.publicationDate || ''}
                                        precision={editableMetadata.publicationDatePrecision}
                                        editingField={editingField}
                                        onEdit={handleEditField}
                                        onSave={handleSaveField}
                                        onCancel={handleCancelEdit}
                                        onPrecisionChange={handlePrecisionChange}
                                    />
                                    <EditableField
                                        fieldName="publicationCode"
                                        label="Publication Code"
                                        value={editableMetadata.publicationCode || ''}
                                        editingField={editingField}
                                        onEdit={handleEditField}
                                        onSave={handleSaveField}
                                        onCancel={handleCancelEdit}
                                    />
                                    <EditableField
                                        fieldName="license"
                                        label="License"
                                        value={editableMetadata.license || ''}
                                        editingField={editingField}
                                        onEdit={handleEditField}
                                        onSave={handleSaveField}
                                        onCancel={handleCancelEdit}
                                    />
                                </div>
                            </div>

                            {/* Document Metadata */}
                            <div className="bg-white border border-gray-300 rounded-lg p-6">
                                <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                    Document Metadata
                                </h2>
                                <div className="space-y-4">
                                    <EditableField
                                        fieldName="sourceUrl"
                                        label="Source URL"
                                        value={editableMetadata.sourceUrl || ''}
                                        editingField={editingField}
                                        onEdit={handleEditField}
                                        onSave={handleSaveField}
                                        onCancel={handleCancelEdit}
                                    />
                                </div>
                            </div>

                            {/* Tags */}
                            <div className="bg-white border border-gray-300 rounded-lg p-6">
                                <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                    Tags
                                </h2>
                                <TagsSection
                                    tags={editableMetadata?.tags || []}
                                    onAddTag={handleAddTag}
                                    onRemoveTag={handleRemoveTag}
                                />
                            </div>
                        </div>

                        {/* Right Column: All Entities */}
                        <div className="flex-1 w-full space-y-6">
                            {/* Authors */}
                            <div className="bg-white border border-gray-300 rounded-lg p-6">
                                <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                    Authors
                                </h2>
                                <EntitySelectionSection
                                    entities={editableMetadata.authors || []}
                                    selections={authorSelections}
                                    onSelectionChange={handleAuthorSelectionChange}
                                    onRemoveEntity={handleRemoveAuthor}
                                    emptyMessage="No authors found"
                                    singularLabel="author"
                                    pluralLabel="authors"
                                />
                            </div>

                            {/* Organisations */}
                            <div className="bg-white border border-gray-300 rounded-lg p-6">
                                <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                    Organisations
                                </h2>
                                <EntitySelectionSection
                                    entities={editableMetadata.organisations || []}
                                    selections={organisationSelections}
                                    onSelectionChange={handleOrganisationSelectionChange}
                                    onRemoveEntity={handleRemoveOrganisation}
                                    emptyMessage="No organisations found"
                                    singularLabel="organisation"
                                    pluralLabel="organisations"
                                />
                            </div>

                            {/* Related Persons */}
                            <div className="bg-white border border-gray-300 rounded-lg p-6">
                                <h2 className="text-lg font-semibold text-gray-900 mb-4 border-b border-gray-200 pb-2">
                                    Related Persons
                                </h2>
                                <EntitySelectionSection
                                    entities={editableMetadata.relatedPersons || []}
                                    selections={relatedPersonSelections}
                                    onSelectionChange={handleRelatedPersonSelectionChange}
                                    onRemoveEntity={handleRemoveRelatedPerson}
                                    emptyMessage="No related persons found"
                                    singularLabel="person"
                                    pluralLabel="persons"
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
                            onClick={handleSave}
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