import React from "react";
import { UserRoleProvider } from "@/context/user-role-context";

export default function ArchiveLayout({ children }: { children: React.ReactNode }) {
  return <UserRoleProvider>{children}</UserRoleProvider>;
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


