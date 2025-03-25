import { PageRoleGuard } from "@/components/auth/RoleGuard";
import InvitationCard from "./components/invitation-card";

export default async function InvitePage() {
  return (
    <PageRoleGuard requiredRole="admin">
      <InvitationCard />
    </PageRoleGuard>
  );
}