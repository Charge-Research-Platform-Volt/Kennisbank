import { FileIcon } from "lucide-react";

/**
 * Component that returns a file icon based on the file type.
 *
 * @param fileType - The type of file to get an icon for (e.g., "pdf", "image", "jpg", "png")
 * @returns A FileIcon component with size 18
 *
 */
export default function GetFileIcon(fileType: string) {
  switch (fileType.toLowerCase()) {
    case "pdf":
      return <FileIcon size={18} />;
    case "image":
    case "jpg":
    case "png":
      return <FileIcon size={18} />;
    default:
      return <FileIcon size={18} />;
  }
}
