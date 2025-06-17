import React from "react";
import { UserRoleProvider } from "@/context/user-role-context";
import RightSidebar from "@/components/sidebar/right-sidebar/right-sidebar";

export default function ArchiveLayout({ children }: { children: React.ReactNode }) 
{
    return (
        <UserRoleProvider>
            <div className="flex">
                <div className="h-full flex-1">
                    {children}
                </div>
                <RightSidebar />
            </div>
        </UserRoleProvider>
    )
}