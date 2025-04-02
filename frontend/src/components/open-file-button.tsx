"use client";

import { Button } from "@/components/ui/button";
import DownloadIcon from "@/icons/download-icon";
import { DocumentResponse } from "@/types/document.type";

export default function OpenFileButton({file, asIcon = false}: {file: DocumentResponse, asIcon?: boolean}) {
    const openFileInNewTab = async () => {
            const url = `http://localhost:8080/Storage/download/${file.id}`;

            if(file.fileType === "pdf"){
                try {
                    const response = await fetch(url, {
                        method: 'GET',
                        credentials: 'include', // Zorgt ervoor dat cookies worden meegestuurd
                    });
                  
                    if (!response.ok) {
                    throw new Error(`Fout bij ophalen van bestand: ${response.statusText}`);
                    }
    
                    const blob = await response.blob();
                    const blobUrl = URL.createObjectURL(blob);
                    
                    window.open(blobUrl, '_blank');
                } catch (error) {
                    console.error("Fout bij ophalen van bestand:", error);
                }
            }
            else{
                window.open(url, '_blank');
            }
        };
    return asIcon ? 
    (
        <Button
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="button"
          onClick={openFileInNewTab}
          title="Delete tag"
        >
          <DownloadIcon className="h-5 w-5" fill="#737373" />
        </Button>

    ) : 
    (
        <Button onClick={openFileInNewTab} className="w-[99.08px]">{file.fileType === "pdf" ? 'Open' : 'Download' }</Button>
    );
}