import React from "react";
import { UserRoleProvider } from "@/context/user-role-context";
import { ArchiveProvider } from "@/context/archive-provider";
import RightSidebar from "@/components/sidebar/right-sidebar/right-sidebar";

export default function ArchiveLayout({ children }: { children: React.ReactNode }) 
{
    return (
        <UserRoleProvider>
            <ArchiveProvider>
                {children}
                <RightSidebar />
            </ArchiveProvider>
        </UserRoleProvider>
    )
}