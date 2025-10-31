import { useRef } from 'react';
import { Upload } from 'lucide-react';
import { toast } from 'sonner';

interface FileUploadAreaProps
{
    isDragging: boolean;
    setIsDragging: (dragging: boolean) => void;
    onFileSelect: (file: File) => void;
    acceptedExtensions?: string[];
}

export default function FileUploadArea({
    isDragging,
    setIsDragging,
    onFileSelect,
    acceptedExtensions = ['.pdf', '.doc', '.docx', '.txt', '.png', '.jpg', '.jpeg'],
}: FileUploadAreaProps)
{
    const fileInputRef = useRef<HTMLInputElement>(null);

    // Convert extensions array to accept string for input
    const acceptString = acceptedExtensions.join(',');

    // Format extensions for display
    const getExtensionDisplay = () =>
    {
        return acceptedExtensions
                .map(ext => ext.replace('.', '').toUpperCase())
                .join(', ');
    
        // if (acceptedExtensions.length <= 5)
        // {
        //     // Show all if 5 or fewer
        //     return acceptedExtensions
        //         .map(ext => ext.replace('.', '').toUpperCase())
        //         .join(', ');
        // }
        // else
        // {
        //     // Show first few + count
        //     const first5 = acceptedExtensions
        //         .slice(0, 5)
        //         .map(ext => ext.replace('.', '').toUpperCase())
        //         .join(', ');
        //     const remaining = acceptedExtensions.length - 5;
        //     return `${first5} and ${remaining} more`;
        // }
    };

    const validateFile = (file: File): boolean =>
    {
        // Validate file extension
        const fileExtension = "" + file.name.split('.').pop()?.toLowerCase();
        
        if (!acceptedExtensions.includes(fileExtension))
        {
            toast.error(`File type not supported. Please use: ${getExtensionDisplay()}`);
            return false;
        }
        
        return true;
    };

    const handleDragOver = (e: React.DragEvent) =>
    {
        e.preventDefault();
        setIsDragging(true);
    };

    const handleDragLeave = () =>
    {
        setIsDragging(false);
    };

    const handleDrop = (e: React.DragEvent) =>
    {
        e.preventDefault();
        setIsDragging(false);
        console.log('File dropped');

        if (e.dataTransfer.files && e.dataTransfer.files[0])
        {
            const file = e.dataTransfer.files[0];

            if (validateFile(file))
            {
                onFileSelect(file);
            }
        }
    };

    const handleClick = () =>
    {
        fileInputRef.current?.click();
    };

    const handleFileInputChange = (e: React.ChangeEvent<HTMLInputElement>) =>
    {
        console.log('File input changed');
        
        if (e.target.files && e.target.files[0])
        {
            const file = e.target.files[0];

            if (validateFile(file))
            {
                onFileSelect(file);
            }
            else
            {
                // Clear the input so user can try again
                if (fileInputRef.current)
                {
                    fileInputRef.current.value = '';
                }
            }
        }
    };

    return (
        <div
            onClick={handleClick}
            onDragOver={handleDragOver}
            onDragLeave={handleDragLeave}
            onDrop={handleDrop}
            className={`
                bg-white border-2 border-dashed rounded-xl p-12 text-center cursor-pointer
                transition-all
                ${isDragging 
                    ? 'border-purple-600 bg-purple-50 border-solid' 
                    : 'border-gray-300 hover:border-purple-600 hover:bg-purple-50/50'
                }
            `}
        >
            <div className="w-16 h-16 mx-auto mb-4 bg-purple-100 rounded-xl flex items-center justify-center">
                <Upload className="w-8 h-8 text-purple-600" />
            </div>
            <div>
                <h3 className="text-base font-semibold text-gray-900 mb-2">
                    Drop your file here or click to browse
                </h3>
                <p className="text-sm text-gray-600 mb-2">
                    Supported formats: {getExtensionDisplay()}
                </p>
            </div>
            <input
                ref={fileInputRef}
                type="file"
                onChange={handleFileInputChange}
                className="hidden"
                accept={acceptString}
            />
        </div>
    );
}