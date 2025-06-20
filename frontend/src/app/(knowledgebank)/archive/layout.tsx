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

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


