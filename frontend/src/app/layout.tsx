import type { Metadata } from "next";
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
      <body className="h-screen w-screen flex overflow-hidden">
        <SideBarMenu className="w-64 h-screen bg-gray-100 p-4 sticky top-0" tags={result.data}/>
        <div className="flex-1 overflow-y-auto">
          {children}
        </div>
        <Toaster />
      </body>
    </html>
  );
}
