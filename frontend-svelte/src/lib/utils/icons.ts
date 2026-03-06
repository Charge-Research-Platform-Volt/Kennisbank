import { File, FileText, Globe, FileArchive, Image, User, Building2 } from 'lucide-svelte';

export function getFileIcon(fileType: string) 
{
    switch (fileType.toLowerCase()) 
    {
        case 'pdf': case 'doc': case 'docx': return FileText;
        case 'website': return Globe;
        case 'zip': return FileArchive;
        case 'image': case 'jpg': case 'png': return Image;
        case 'person': return User;
        case 'organisation': return Building2;
        default: return File;
    }
}

export function getFileAction(fileType: string): 'open' | 'download' | null
{
    switch (fileType.toLowerCase()) 
    {
        case 'website': return 'open';
        case 'pdf': case 'doc': case 'docx': case 'zip': case 'image': case 'jpg': case 'png': return 'download';
        default: return null;
    }
}