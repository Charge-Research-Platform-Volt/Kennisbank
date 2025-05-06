import { ContentRoleGuard } from "@/components/auth/RoleGuard";
import CreateStandardizedTag from "./components/create-standardized-tag";
import CreateUserTag from "./components/create-tag";
import ListTags from "./components/tag-list";
import { getCurrentUserRole } from "@/lib/auth-server";

export default async function StandardizedTagsPage() {
    const userRole = (await getCurrentUserRole()).role;

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
                
                <ListTags userRole={userRole}/>
            </div>
        </div>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


