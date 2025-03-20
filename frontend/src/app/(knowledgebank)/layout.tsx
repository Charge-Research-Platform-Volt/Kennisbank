import LeftSidebarServer from "@/components/left-sidebar/left-sidebar-server";

export default async function KnowledgeBankLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <div className="flex w-full h-screen">
        <LeftSidebarServer />
        <main className="w-full overflow-y-auto">{children}</main>
    </div> 
  );
}
