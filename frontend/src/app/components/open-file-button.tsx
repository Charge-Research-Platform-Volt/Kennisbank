"use client";

import { Button } from "@/components/ui/button";
import { DocumentResponse } from "@/types/document.type";

export default function OpenFileButton({file}: {file: DocumentResponse}) {
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
    return (
        <Button onClick={openFileInNewTab} className="w-[99.08px]">{file.fileType === "pdf" ? 'Open' : 'Download' }</Button>
    )
}