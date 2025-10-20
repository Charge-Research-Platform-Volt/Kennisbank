import { PageRoleGuard } from "@/components/auth/RoleGuard";
import UsersList from "./_components/UsersList";
import InvitationCard from "./_components/invitation-card";

export default async function UsersPage() {
  return (
    <PageRoleGuard requiredRole="admin">
      <div className="w-full p-2">
        <div className="flex h-full w-full">
          <div className="flex h-full w-full">
            {/* Users list */}
            <div className="h-full w-full flex-1">
              <UsersList />
            </div>
            {/* Invitation card */}
            <div className="ml-4 w-75">
              <InvitationCard />
            </div>
          </div>
        </div>
      </div>
    </PageRoleGuard>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
