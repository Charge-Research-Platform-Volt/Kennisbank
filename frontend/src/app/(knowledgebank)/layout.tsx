import LeftSidebarServer from "@/components/sidebar/left-sidebar/left-sidebar-server";
import RightSidebar from "@/components/sidebar/right-sidebar/right-sidebar";
import { ArchiveProvider } from "@/context/archive-provider";
import { QuickSearchProvider } from "@/context/quick-search-provider";
import { SidebarProvider } from "@/context/sidebar-provider";
import { TanStackQueryProvider } from "@/context/tanStack-query-provider";
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
      <SidebarProvider leftSidebarDefaultState={leftSidebarDefault}>
        <QuickSearchProvider>
          <ArchiveProvider>
            <div className="flex h-screen w-full">
              <LeftSidebarServer />
              <main className="w-full overflow-y-auto">{children}</main>
              <RightSidebar />
            </div>
          </ArchiveProvider>
        </QuickSearchProvider>
      </SidebarProvider>
    </TanStackQueryProvider>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
