import ArchiveSidebar from "@/components/sidebars/archive-sidebar/archive-sidebar";
import { ArchiveProvider } from "@/context/archive-provider";
import { ArchiveSidebarProvider } from "@/context/archive-sidebar-provider";
import React from "react";


export default function ArchiveLayout({ children }: { children: React.ReactNode })
{
    return (
        <ArchiveSidebarProvider archiveSidebarDefaultState={false}>
            <ArchiveProvider>
                <div className="flex min-h-screen">
                    <div className="flex-1">
                        {children}
                    </div>
                    <ArchiveSidebar/>
                </div>
            </ArchiveProvider>
        </ArchiveSidebarProvider>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


