import React from "react";
import { DocumentPageResponseSchema } from "@/types/document.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { CloudAlert } from "lucide-react";

export default async function ListDocuments() {
    const result = await FetchWithValidation(
       DocumentPageResponseSchema,
       "http://backend:8080/Storage/list-all",
    );

    return (
        <div>
            <h1>Documents</h1>
            {result.data &&
                result.data.files.map((document) => (
                    <div key={document.id} className="flex flex-row gap-4">
                        <h2>{document.name}</h2>        
                        <p>{document.description}</p>  
                    </div>
                ))}
            {result.error && (
                <div className="flex flex-row items-center gap-2 text-red-500">
                    <CloudAlert size={20} />
                    <p>{result.error.message}</p>
                </div>
            )}
        </div>
    );
}
