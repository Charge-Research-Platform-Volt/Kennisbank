"use client";

import { handleOpenFile } from "@/actions/openFileActionsClient";
import { Button } from "@/components/ui/button";
import GetDownloadIcon from "./getDownloadIcon";

/**
 * 
 * @param File - The file in question, used to determine hover text and alt text and which icon to display
 * @param asIcon - Whether the button should be an icon or not, by default it should be on false, but in the archive it is set on true
 * @param variant - Variant of how the button should look if it is not an icon, also useful for if the icons don't load on the page
 * @returns A button where users can either visit the page or open the file in question
 */
export default function OpenFileButton({
  id,
  fileType,
  asIcon = false,
  variant = "default",
}: {
  id: string,
  fileType: string,
  asIcon?: boolean,
  variant?: "default" | "link" | "destructive" | "outline" | "secondary" | "ghost" | null | undefined;
}) {

  // Text when hovering over the button and the icon is loaded.
  function buttonText(): string 
  {
    switch(fileType)
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
    switch(fileType)
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
          onClick={async () => await handleOpenFile(id, fileType)}
          title={buttonText()}
        >
          <GetDownloadIcon fileType={fileType} className="h-5 w-5" />
        </Button>

    ) : 
    (
        <Button onClick={() => handleOpenFile(id, fileType)} className="w-[99.08px]" variant={variant}>{buttonAltText()}</Button>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


