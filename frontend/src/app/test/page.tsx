import DropDownBox from "@/components/menu/newDropDownBox/newDropDownBox"
import { TagsArraySchema } from "@/types/tag.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";

export default async function StandardizedTagsPage() {
    const result = await FetchWithValidation(
        TagsArraySchema,
        "http://backend:8080/Tag/all-tags",
      );

    if(!result.success) {
        throw new Error("Data validation failed");
      }

    return (
        <div className="flex">
            <div className="flex-1 p6 p-4">
                <DropDownBox tags={result.data}></DropDownBox>
            </div>
        </div>
    )
}
