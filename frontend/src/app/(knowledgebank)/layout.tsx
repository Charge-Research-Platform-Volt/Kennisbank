import LeftSidebarServer from "@/components/left-sidebar/left-sidebar-server";
import { QuickSearchProvider } from "@/components/quick-search-context";

export default async function KnowledgeBankLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <QuickSearchProvider>
      <div className="flex h-screen w-full">
        <LeftSidebarServer />
        <main className="w-full overflow-y-auto p-2.5">{children}</main>
      </div>
    </QuickSearchProvider>
  );
}
