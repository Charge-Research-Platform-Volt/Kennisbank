import { ResourceResponse } from "@/types/resource.type";

export const handleOpenFile = async (file: ResourceResponse) => {
    const url = `http://localhost:8080/Storage/download/${file.id}`;
    
    if(file.fileType === "pdf"){
        try {
            const response = await fetch(url, {
                method: 'GET',
                credentials: 'include', // Makes sure cookies are included
            });
          
            if (!response.ok) {
            throw new Error(`Error getting file: ${response.statusText}`);
            }

            const blob = await response.blob();
            const blobUrl = URL.createObjectURL(blob);
            
            window.open(blobUrl, '_blank');
        } catch (error) {
            console.error("Error getting file:", error);
        }
    }
    else{
        window.open(url, '_blank');
    }
  }