import { ContentRoleGuard } from "@/components/auth/RoleGuard";
import CreateStandardizedTag from "./components/create-standardized-tag";
import CreateUserTag from "./components/create-tag";
import ListTags from "./components/list-tags";

export default async function StandardizedTagsPage() {
    return (
        <div className="flex">
            <div className="flex-1 p6 p-4 w-full">
                <div className="flex gap-2 w-full">
                    <div className="w-full">
                        <h1 className="text-xl font-bold mb-2">Tags:</h1>
                        <CreateUserTag />
                    </div>
                    <ContentRoleGuard requiredRole="admin">
                        <div className="w-full">
                            <h1 className="text-xl font-bold mb-2">Create Standardized Tag:</h1>
                            <CreateStandardizedTag />
                        </div>
                    </ContentRoleGuard>
                </div>
                
                <ListTags />
            </div>
        </div>
    )
}