import LeftSidebarServer from "@/components/sidebar/left-sidebar/left-sidebar-server";
import { QuickSearchProvider } from "@/components/quick-search-context";
import RightSidebar from "@/components/sidebar/right-sidebar/right-sidebar";

export default async function KnowledgeBankLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <QuickSearchProvider>
      <div className="flex h-screen w-full">
        <LeftSidebarServer />
        <main className="w-full overflow-y-auto p-2.5">{children}</main>
        <RightSidebar />
    </div>
    </QuickSearchProvider>
  );
}
