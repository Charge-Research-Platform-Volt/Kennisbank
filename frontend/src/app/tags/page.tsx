import ListStandardizedTags from "../standardizedtags/components/list-standardized-tags";
import ListUserTags from "./components/list-usertags";
import CreateUserTag from "./components/create-usertag";


export default async function StandardizedTagsPage() {
    return (
        <div className="p-4">
            <h1>Create User Tag:</h1>
            <CreateUserTag />
            <h1>Standardized tag list:</h1>
            <ListStandardizedTags />
            <h1>User tag list:</h1>
            <ListUserTags />
        </div>
    )
}