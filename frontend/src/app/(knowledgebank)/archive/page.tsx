"use client";

import { UserRoleProvider } from "@/context/user-role-context";
import ArchivePage from "./ArchivePage";

export default function Page() {
  return (
    <UserRoleProvider>
      <ArchivePage />
    </UserRoleProvider>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)