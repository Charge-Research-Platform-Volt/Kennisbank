import { RootProvider } from "fumadocs-ui/provider";
import "./globals.css";
import type { Metadata } from "next";
import { Inter } from "next/font/google";

// Metadata
export const metadata: Metadata = {
    title: "Ohmega Help",
    description: "Guides and Docs for Ohmega project",
};

// Font Inter
const inter = Inter({ subsets: ["latin"] });

export default function RootLayout({
    children,
}: Readonly<{ children: React.ReactNode }>) {
    return (
        <html lang="en" suppressHydrationWarning>
            <body className={inter.className}>
                <RootProvider
                    search={{
                        enabled: false,
                    }}
                >
                    {children}
                </RootProvider>
                <label className="fixed right-1 bottom-1 text-xs">
                    <i>© Utrecht University (ICS)</i>
                </label>
            </body>
        </html>
    );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)