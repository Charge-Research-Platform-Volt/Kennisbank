import { SideBarMenu } from "@/components/menu/side-bar-menu";
import CreateStandardizedTag from "./components/create-standardized-tag";
import ListStandardizedTags from "./components/list-standardized-tags";

export default async function StandardizedTagsPage() {
    return (
        <div className="flex">
            <SideBarMenu className="w-64 h-screen bg-gray-100 p-4" />
            <div className="flex-1 p6 p-4">
                <CreateStandardizedTag />
                <h1>Standardized tag list:</h1>
                <ListStandardizedTags />
            </div>
        </div>
    )
}