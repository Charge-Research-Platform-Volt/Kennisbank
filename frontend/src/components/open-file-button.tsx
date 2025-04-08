"use client";

import { Button } from "@/components/ui/button";
import DownloadIcon from "@/icons/download-icon";
import { DocumentResponse } from "@/types/document.type";

export default function OpenFileButton({
  file,
  asIcon = false,
  variant = "default",
}: {
  file: DocumentResponse;
  asIcon?: boolean;
  variant?: "default" | "link" | "destructive" | "outline" | "secondary" | "ghost" | null | undefined;
}) {
  const openFileInNewTab = async () => {
    const url = `http://localhost:8080/Storage/download/${file.id}`;

    if (file.fileType === "pdf") {
      try {
        const response = await fetch(url, {
          method: "GET",
          credentials: "include", // Makes sure cookies are included
        });

        if (!response.ok) {
          throw new Error(`Error getting file: ${response.statusText}`);
        }

        const blob = await response.blob();
        const blobUrl = URL.createObjectURL(blob);

        window.open(blobUrl, "_blank");
      } catch (error) {
        console.error("Error getting file:", error);
      }
    } else {
      window.open(url, "_blank");
    }
  };
  return asIcon ? (
    <Button className="text-muted-foreground bg-transparent shadow-none hover:bg-gray-200" variant="default" type="button" onClick={openFileInNewTab} title="Download file">
      <DownloadIcon className="h-5 w-5" fill="#737373" />
    </Button>
  ) : (
    <Button onClick={openFileInNewTab} variant={variant}>
      {file.fileType === "pdf" ? "Open" : "Download"}
    </Button>
  );
}
