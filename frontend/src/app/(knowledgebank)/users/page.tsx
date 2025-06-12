import { PageRoleGuard } from "@/components/auth/RoleGuard";
import UsersList from "./components/UsersList";
import InvitationCard from "./components/invitation-card";

export default async function UsersPage() {   
  return (
    <PageRoleGuard requiredRole="admin">
      <div className="w-full">
        <div className="h-full w-full flex">
          <div className="flex h-full w-full">
            { /* Users list */ }
            <div className="flex-1 h-full w-full">
              <UsersList />
            </div>
            { /* Invitation card */ }
            <div className="w-75 ml-4">
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


