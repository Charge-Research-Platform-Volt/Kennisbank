import LeftSidebarServer from "@/components/sidebar/left-sidebar/left-sidebar-server";
import { QuickSearchProvider } from "@/context/quick-search-provider";
import RightSidebar from "@/components/sidebar/right-sidebar/right-sidebar";

export default async function KnowledgeBankLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <QuickSearchProvider>
      <div className="flex h-screen w-full">
        <LeftSidebarServer />
        <main className="w-full overflow-y-auto">{children}</main>
        <RightSidebar />
      </div>
    </QuickSearchProvider>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
