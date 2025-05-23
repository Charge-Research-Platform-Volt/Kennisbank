import React from "react";
import { UserRoleProvider } from "@/context/user-role-context";

export default function ArchiveLayout({ children }: { children: React.ReactNode }) 
{
    return (
        <UserRoleProvider>
            {children}
        </UserRoleProvider>
    )
}