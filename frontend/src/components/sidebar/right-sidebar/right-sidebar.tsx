"use client";

import React from "react";
import { Sidebar } from "@/components/ui/sidebar";
import RightSidebarInfo from "./right-sidebar-info";
import { useSidebar } from "@/context/sidebar-provider";
import RightSidebarHistory from "./right-sidebar-history";

/**
 *
 * @returns The right sidebar visible when clicked on an item in the archive. Displays useful information such as metadata and related files (soon).
 */
export default function RightSidebar() {
  const { sidebarMode } = useSidebar();

  const isChatHistoryMode = sidebarMode === "chat-history";
  
  return (
    <Sidebar side="right" width={isChatHistoryMode ? "300px" : "600px"} collapsible="offcanvas">
      {isChatHistoryMode ? <RightSidebarHistory /> : <RightSidebarInfo />}
    </Sidebar>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
