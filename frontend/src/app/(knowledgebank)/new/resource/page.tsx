"use client"

import React from 'react';
import { useRouter } from 'next/navigation';
import FileUploadArea from '@/components/new/FileUploadArea';
import FilePreview from '@/components/new/FilePreview';
import UrlInput from '@/components/new/UrlInput';

export default function NewResourcePage() 
{
    const router = useRouter();
    const [file, setFile] = React.useState<File | null>(null);
    const [url, setUrl] = React.useState('');
    const [isDragging, setIsDragging] = React.useState(false);
    const [supportedExtensions, setSupportedExtensions] = React.useState<string[]>([]);
    const [isLoading, setIsLoading] = React.useState(true);
    
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
    
    const handleContinue = async () => 
    {
        if (file) 
        {
            console.log("Uploading file: ", file);
            // TODO: UPLOAD LOGIC
        }
        else if (url) 
        {
            console.log("Processing URL: ", url);
            // TODO: URL PROCESSING LOGIC
        }
    };
    
    const canContinue = file !== null || url.trim().length > 0;
    
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
            <div className="container mx-auto px-4 py-10 text-center">
                <p>Loading...</p>
            </div>
        );
    }
    
    return (
        <div className="container mx-auto px-4 sm:px-6 lg:px-8 max-w-3xl py-10">
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
        </div>
    );
}