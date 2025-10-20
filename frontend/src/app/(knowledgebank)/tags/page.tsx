import { ContentRoleGuard } from "@/components/auth/RoleGuard";
import CreateStandardizedTag from "./_components/create-standardized-tag";
import CreateUserTag from "./_components/create-tag";
import ListTags from "./_components/tag-list";

export default async function StandardizedTagsPage() {
    return (
        <div className="flex p-2">
            <div className="flex-1 p6 w-full">
                <div className="flex gap-2 mb-2 w-full">
                    <div className="w-full">
                        <CreateUserTag />
                    </div>
                    <ContentRoleGuard requiredRole="admin">
                        <div className="w-full">
                            <CreateStandardizedTag />
                        </div>
                    </ContentRoleGuard>
                </div>
                <ListTags/>
            </div>
        </div>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
