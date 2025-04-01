import ListResources from "@/components/list-documents";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { ResourcePageResponseSchema } from "@/types/resource.type";

export default async function ArchivePage() {
  const result = await FetchWithValidation(ResourcePageResponseSchema, "http://backend:8080/Storage/list-all");

  if (result.error) console.log(result.error);

  return <div className="flex">{result.success ? <ListResources data={result.data} /> : <div>Error loading documents</div>}</div>;
}
