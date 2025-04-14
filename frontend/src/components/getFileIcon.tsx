import ImgIcon from "@/icons/file-type-icons/img-icon";
import PdfIcon from "@/icons/file-type-icons/pdf-icon";
import WebsiteIcon from "@/icons/file-type-icons/website-icon";
import WordIcon from "@/icons/file-type-icons/word-icon";
import ZipIcon from "@/icons/file-type-icons/zip-icon";
import { cn } from "@/lib/utils";
import { FileIcon } from "lucide-react";

/**
 * Component that returns a file icon based on the file type.
 *
 * @param fileType - The type of file to get an icon for (e.g., "pdf", "image", "jpg", "png")
 * @returns A FileIcon component with size 18
 *
 */
export default function GetFileIcon({ fileType, className }: { fileType: string; className?: string }) {
  switch (fileType.toLowerCase()) {
    case "pdf":
      return <PdfIcon className={cn("flex-shrink-0", className)} />;
    case "website":
      return <WebsiteIcon className={cn("flex-shrink-0", className)} />;
    case "doc":
    case "docx":
    case "word":
      return <WordIcon className={cn("flex-shrink-0", className)} />;
    case "zip":
      return <ZipIcon className={cn("flex-shrink-0", className)} />;
    case "image":
    case "jpg":
    case "png":
      return <ImgIcon className={cn("flex-shrink-0", className)} />;
    default:
      return <FileIcon size={18} className={cn("flex-shrink-0", className)} />;
  }
}
