import LeftSidebarServer from "@/components/left-sidebar/left-sidebar-server";

export default async function KnowledgeBankLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flex h-screen w-full">
      <LeftSidebarServer />
      <main className="w-full overflow-y-auto p-4">{children}</main>
    </div>
  );
}
