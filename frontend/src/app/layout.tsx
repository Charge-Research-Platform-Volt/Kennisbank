import type { Metadata } from "next";
import "./globals.css";

import { Toaster } from "@/components/ui/sonner";
import { TabletSmartphone } from "lucide-react";
import { Inter } from "next/font/google";
import { SidebarProvider } from "@/context/sidebar-provider";
import { cookies } from "next/headers";
//import LeftSidebarServer from "@/components/left-sidebar/left-sidebar-server";

// Metadata
export const metadata: Metadata = {
  title: "KnowledgeBank",
  description: "Research knowledgebank by Charge",
};

// Fonts
const inter = Inter({
  subsets: ["latin"],
  display: "swap",
});

export default async function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  // Get the state of the right sidebar from cookies
  const leftSidebar = (await cookies()).get("leftSidebar:state");
  let leftSidebarDefault = true;
  if (leftSidebar) {
    leftSidebarDefault = leftSidebar.value === "true";
  }

  return (
    <html className={inter.className} lang="en">
      <body className="flex h-screen w-screen overflow-hidden">
        <SidebarProvider leftSidebarDefaultState={leftSidebarDefault}>{children}</SidebarProvider>
        <Toaster />

        {/* Mobile device warning */}
        <div className="fixed inset-0 z-[9999] flex h-screen w-screen flex-col items-start justify-center gap-1.5 bg-white p-2 text-base font-medium lg:hidden">
          <TabletSmartphone />
          This website does not support mobile devices. Please use a desktop or laptop computer. If you are using a computer, please make the window larger.
        </div>
      </body>
    </html>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
