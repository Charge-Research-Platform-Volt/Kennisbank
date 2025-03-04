import CreateStandardizedTag from "./components/create-standardized-tag";
import ListStandardizedTags from "./components/list-standardized-tags";

export default async function StandardizedTagsPage() {
    return (
        <div className="p-4">
            <CreateStandardizedTag />
            <h1>Standardized tag list:</h1>
            <ListStandardizedTags />
        </div>
    )
}