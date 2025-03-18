import CreateStandardizedTag from "./components/create-standardized-tag";
import ListStandardizedTags from "./components/list-standardized-tags";

export default async function StandardizedTagsPage() {
    return (
        <div className="flex">
            <div className="flex-1 p6 p-4">
                <CreateStandardizedTag />
                <h1>Standardized tag list:</h1>
                <ListStandardizedTags />
            </div>
        </div>
    )
}