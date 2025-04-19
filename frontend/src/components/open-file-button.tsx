"use client";

import { handleOpenFile } from "@/actions/openFileActions";
import { Button } from "@/components/ui/button";
import OpenDocumentIcon from "@/icons/file-type-icons/open-document-icon";
import OpenWebsiteIcon from "@/icons/file-type-icons/open-website";
import { ResourceResponse } from "@/types/resource.type";

/**
 * 
 * @param File - The file in question, used to determine hover text and alt text and which icon to display
 * @param asIcon - Whether the button should be an icon or not, by default it should be on false, but in the archive it is set on true
 * @param variant - Variant of how the button should look if it is not an icon, also useful for if the icons don't load on the page
 * @returns A button where users can either visit the page or open the file in question
 */
export default function OpenFileButton({
  file,
  asIcon = false,
  variant = "default",
}: {
  file: ResourceResponse,
  asIcon?: boolean,
  variant?: "default" | "link" | "destructive" | "outline" | "secondary" | "ghost" | null | undefined;
}) {

  // Text when hovering over the button and the icon is loaded.
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

  // Text of the button otherwise.
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
