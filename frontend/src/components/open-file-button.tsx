"use client";

import { handleOpenFile } from "@/actions/openFileActions";
import { Button } from "@/components/ui/button";
import OpenDocumentIcon from "@/icons/file-type-icons/open-document-icon";
import OpenWebsiteIcon from "@/icons/file-type-icons/open-website";
import { ResourceResponse } from "@/types/resource.type";

export default function OpenFileButton({
  file,
  asIcon = false,
  variant = "default",
}: {
  file: ResourceResponse,
  asIcon?: boolean,
  variant?: "default" | "link" | "destructive" | "outline" | "secondary" | "ghost" | null | undefined;
}) {
  function buttonText(): string 
  {
    switch(file.fileType)
    {
      case "pdf":
        return "Open PDF"
      case "website":
        return "Visit Website"
      default:
        return "Download File"
    }
  }

  function buttonAltText(): string 
  {
    switch(file.fileType)
    {
      case "pdf":
        return "Open"
      case "website":
        return "Visit"
      default:
        return "Download"
    }
  }
    return asIcon ? 
    (
        <Button
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="button"
          onClick={() => handleOpenFile(file)}
          title={buttonText()}
        >
          {file.fileType == "website" ? <OpenWebsiteIcon className="h-5 w-5" fill="#737373"></OpenWebsiteIcon> : <OpenDocumentIcon className="h-5 w-5" fill="#737373" />}
        </Button>

    ) : 
    (
        <Button onClick={() => handleOpenFile(file)} className="w-[99.08px]" variant={variant}>{buttonAltText()}</Button>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


