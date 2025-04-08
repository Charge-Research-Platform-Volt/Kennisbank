import ListDocuments from "@/components/list-documents";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { DocumentPageResponseSchema } from "@/types/document.type";

export default async function ArchivePage() {
  const result = await FetchWithValidation(DocumentPageResponseSchema, "http://backend:8080/Storage/list-all");
  console.log(result.error);

  return <div className="flex">{result.success ? <ListDocuments data={result.data} /> : <div>Error loading documents</div>}</div>;
}
