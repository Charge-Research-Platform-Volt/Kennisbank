import type { Metadata } from "next";
import { Geist, Geist_Mono } from "next/font/google";
import "./globals.css";
import { Toaster } from "@/components/ui/sonner";
import { SideBarMenu } from "@/components/menu/side-bar-menu";
import { TagsArraySchema } from "@/types/tag.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";

export const metadata: Metadata = {
  title: "Charge",
  description: "Research knowledgebank by Charge",
};

export default async function RootLayout({
  children,
}: Readonly<{
  children: React.ReactNode;
}>) {

  const result = await FetchWithValidation(
    TagsArraySchema,
    "http://backend:8080/Tag/all-tags",
  );
  if(!result.success) {
      throw new Error("Data validation failed");
    }

  return (
    <html>
    <body className="h-screen w-screen flex">
      <SideBarMenu className="w-64 h-screen bg-gray-100 p-4" tags={result.data}/>
      
      {children}
      <Toaster />
    </body>
    </html>
  );
}
