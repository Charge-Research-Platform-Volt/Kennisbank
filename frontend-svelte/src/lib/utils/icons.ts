import { File, FileText, Globe, Music, Video, User, Building2 } from 'lucide-svelte';

export function getFileIcon(fileType: string)
{
    switch (fileType.toLowerCase())
    {
        case 'document': return FileText;
        case 'pdf': case 'doc': case 'docx': case 'pptx': case 'xlsx': case 'txt': return FileText;
        case 'audio': case 'mp3': case 'wav': case 'ogg': return Music;
        case 'video': case 'mp4': case 'avi': case 'mkv': case 'mov': return Video;
        case 'website': return Globe;
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
        case 'document': case 'pdf': case 'doc': case 'docx': case 'pptx': case 'xlsx': case 'txt':
        case 'audio': case 'mp3': case 'wav': case 'ogg':
        case 'video': case 'mp4': case 'avi': case 'mkv': case 'mov': return 'download';
        default: return null;
    }
}