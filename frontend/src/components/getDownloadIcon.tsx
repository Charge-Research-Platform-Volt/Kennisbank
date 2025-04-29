import DownloadIcon from "@/icons/file-type-icons/download-icon";
import OpenWebsiteIcon from "@/icons/file-type-icons/open-website";
import ReadDocumentIcon from "@/icons/file-type-icons/read-document-icon";


/**
 * Component that returns a download icon based on the file type.
 *
 * @param fileType - The type of file to get an icon for (e.g., "pdf", "image", "jpg", "png")
 * @returns A DownloadIcon component
 *
 */
export default function GetDownloadIcon({ fileType, className }: {fileType: string; className?: string}) {
    switch (fileType) {
      case "website":
        return <OpenWebsiteIcon className={className} fill="#737373" />;
      case "pdf":
        return <ReadDocumentIcon className={className} fill="#737373" />;
      default:
        return <DownloadIcon className={className} fill="#737373" />;
    }
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)