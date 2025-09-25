import LeftSidebarServer from "@/components/sidebars/left-sidebar/left-sidebar-server";
import { QuickSearchProvider } from "@/context/quick-search-provider";
import { SidebarProvider } from "@/context/sidebar-provider";
import { TanStackQueryProvider } from "@/context/tanStack-query-provider";
import { UserRoleProvider } from "@/context/user-role-context";
import { cookies } from "next/headers";

export default async function KnowledgeBankLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  // Get the state of the right sidebar from cookies
  const leftSidebar = (await cookies()).get("leftSidebar:state");
  let leftSidebarDefault = true;
  if (leftSidebar) {
    leftSidebarDefault = leftSidebar.value === "true";
  }

  return (
    <TanStackQueryProvider>
      <UserRoleProvider>
      <SidebarProvider leftSidebarDefaultState={leftSidebarDefault}>
        <QuickSearchProvider>
            <div className="flex h-screen w-full">
              <LeftSidebarServer />
              <main className="w-full overflow-y-auto">{children}</main>
            </div>
        </QuickSearchProvider>
      </SidebarProvider>
      </UserRoleProvider>
    </TanStackQueryProvider>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
