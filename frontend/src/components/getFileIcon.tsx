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
      return <FileIcon size={18} className={cn("flex-shrink-0", className)} />;

    case "image":
    case "jpg":
    case "png":
      return <FileIcon size={18} className={cn("flex-shrink-0", className)} />;
    default:
      return <FileIcon size={18} className={cn("flex-shrink-0", className)} />;
  }
}
