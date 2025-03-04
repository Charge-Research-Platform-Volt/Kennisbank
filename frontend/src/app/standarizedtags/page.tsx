import ListDocuments from "@/components/list-documents";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { TagsArraySchema } from "@/types/tag.type";
import { z } from "zod";
import CreateStandarizedTag from "./components/create-standarized-tag";
import ListStandarizedTags from "./components/list-standarized-tags";

export default async function StandardizedTagsPage() {
    return (
        <div className="p-4">
            <CreateStandarizedTag />
            <h1>Standarized tag list:</h1>
            <ListStandarizedTags />
        </div>
    )
}