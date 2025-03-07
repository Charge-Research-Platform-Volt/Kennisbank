import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { Toaster } from "@/components/ui/sonner";
import { SideBarMenu } from "@/components/menu/side-bar-menu";

export const metadata: Metadata = {
  title: "Charge",
  description: "Research knowledgebank by Charge",
};

export default function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {
  return (
    <html>
    <body className="h-screen w-screen flex">
      <SideBarMenu className="w-64 h-screen bg-gray-100 p-4" />
      
      {children}
      <Toaster />
    </body>
    </html>
  );
}
