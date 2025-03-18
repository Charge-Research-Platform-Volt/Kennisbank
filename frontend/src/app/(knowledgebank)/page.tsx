import { ContentRoleGuard } from '@/components/auth/RoleGuard';

export default async function Home() {
  // const result = await FetchWithValidation(UsersArraySchema, "http://backend:8080/KnowledgeBank/members");

  return (
    <div>
      <h1>Hello World!</h1>
      <ContentRoleGuard requiredRole="user">
        <h2>Hello user!</h2>
      </ContentRoleGuard>
      <ContentRoleGuard requiredRole="admin">
        <h2>Hello admin!</h2>
      </ContentRoleGuard>
      <ContentRoleGuard requiredRoles={["admin", "user"]}>
        <h3>Hello both!</h3>
      </ContentRoleGuard>
    </div>
  );
}
