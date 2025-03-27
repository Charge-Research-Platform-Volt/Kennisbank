import CreateStandardizedTag from "./components/create-standardized-tag";
import ListStandardizedTags from "./components/list-standardized-tags";

export default async function StandardizedTagsPage() {
    return (
        <div className="flex">
            <div className="flex-1 p6 p-4 w-full">
                <h1 className="text-xl font-bold mb-2">Standardized Tags:</h1>
                <CreateStandardizedTag />
                <ListStandardizedTags />
            </div>
        </div>
    )
}