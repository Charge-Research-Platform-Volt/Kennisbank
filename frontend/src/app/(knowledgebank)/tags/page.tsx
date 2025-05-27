import { ContentRoleGuard } from "@/components/auth/RoleGuard";
import CreateStandardizedTag from "./_components/create-standardized-tag";
import CreateUserTag from "./_components/create-tag";
import ListTags from "./_components/tag-list";

export default async function StandardizedTagsPage() {
  return (
    <div className="flex p-2">
      <div className="w-full flex-1 p-4">
        <div className="flex w-full gap-2">
          <div className="w-full">
            <h1 className="mb-2 text-xl font-bold">Tags:</h1>
            <CreateUserTag />
          </div>
          <ContentRoleGuard requiredRole="admin">
            <div className="w-full">
              <h1 className="mb-2 text-xl font-bold">Create Standardized Tag:</h1>
              <CreateStandardizedTag />
            </div>
          </ContentRoleGuard>
        </div>

        <ListTags />
      </div>
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
