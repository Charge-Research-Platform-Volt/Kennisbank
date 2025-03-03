import React from "react";
import CreateDocument from "./create-document";
import { DocumentArraySchema } from "@/types/document.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";

export default async function ListDocuments() {
  const result = await FetchWithValidation(
    DocumentArraySchema,
    "http://backend:8080/Drive/all-documents",
  );

  if (!result.success) {
    throw new Error("Data validation failed");
  }

  return (
    <div className="p-2">
      Create a document:
      <CreateDocument />
      <h1>Documents (from the database):</h1>
      {result.data.map((document) => (
        <div key={document.id} className="flex flex-row gap-4">
          <h2>{document.name}</h2>
          <p>{document.description}</p>
        </div>
      ))}
    </div>
  );
}
