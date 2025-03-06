import React from "react";
import CreateDocument from "./create-document";
import { DocumentArraySchema } from "@/types/document.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { CloudAlert } from "lucide-react";

export default async function ListDocuments() {
    //const result = await FetchWithValidation(
    //    DocumentArraySchema,
    //    "http://backend:8080/Drive/all-documents",
    //); //DUE TO BACKEND CODE CHANGES THIS CODE ISNT COMPATIBLE
         //ANYMORE AND WAS CAUSING CONTINUOUS RECOMPILATION OF FRONTEND

    return (
        <div className="p-2">
            Create a document:
            <CreateDocument />
            {/* <h1>Documents (from the database):</h1>
            {result.data &&
                result.data.map((document) => (
                    <div key={document.id} className="flex flex-row gap-4">
                        <h2>{document.name}</h2>        //DUE TO BACKEND CODE CHANGES THIS CODE ISNT COMPATIBLE
                        <p>{document.description}</p>   //ANYMORE AND WAS CAUSING CONTINUOUS RECOMPILATION OF FRONTEND
                    </div>
                ))}
            {result.error && (
                <div className="flex flex-row items-center gap-2 text-red-500">
                    <CloudAlert size={20} />
                    <p>{result.error.message}</p>
                </div>
            )} */}
        </div>
    );
}
