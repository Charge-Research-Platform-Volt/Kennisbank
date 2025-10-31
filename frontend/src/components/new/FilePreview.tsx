import { File } from 'lucide-react';

interface FilePreviewProps {
  file: File;
  onRemove: () => void;
}

export default function FilePreview({ file, onRemove }: FilePreviewProps) {
  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return Math.round((bytes / Math.pow(k, i)) * 100) / 100 + ' ' + sizes[i];
  };

  return (
    <div className="bg-white border-2 border-gray-300 rounded-xl p-6 flex items-center gap-4">
      <div className="w-12 h-12 bg-purple-100 rounded-lg flex items-center justify-center flex-shrink-0">
        <File className="w-6 h-6 text-purple-600" />
      </div>
      <div className="flex-1">
        <h4 className="text-sm font-semibold text-gray-900">{file.name}</h4>
        <p className="text-xs text-gray-600">{formatFileSize(file.size)}</p>
      </div>
      <button
        onClick={onRemove}
        className="bg-red-50 text-red-600 px-3 py-2 rounded-lg text-sm 
                   hover:bg-red-100 transition-colors"
      >
        Remove
      </button>
    </div>
  );
}