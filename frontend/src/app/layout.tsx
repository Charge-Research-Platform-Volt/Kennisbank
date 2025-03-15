import type { Metadata } from "next";
import "./globals.css";

import { Toaster } from "@/components/ui/sonner";
import { TabletSmartphone } from "lucide-react";
import LeftSidebarServer from "@/components/left-sidebar/left-sidebar-server";
import { Inter } from "next/font/google";

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
  return (
    <html className={inter.className}>
      <body className="flex h-screen w-screen overflow-hidden">
        {/* -------------------------- */}
        <LeftSidebarServer />
        <main className="w-full overflow-y-auto">{children}</main>
        {/* -------------------------- */}

        <Toaster />

        {/* Mobile device warning */}
        <div className="absolute top-0 left-0 flex h-screen w-screen flex-col items-start justify-center gap-1.5 bg-white p-2 text-base font-medium sm:hidden md:hidden lg:hidden xl:hidden 2xl:hidden">
          <TabletSmartphone />
          This website does not support mobile devices. Please use a desktop or laptop computer.
        </div>
      </body>
    </html>
  );
}
