"use client";

import { Button } from "@/components/ui/button";
import { useSidebar } from "@/context/sidebar-provider";
import React from "react";

export default function ChatHistory() {
  const { toggleRightSidebar, rightSidebarOpen } = useSidebar();

  return (
    <Button variant="outline" className={`fixed top-2 right-2 z-20 transition-all ${rightSidebarOpen ? "w-14" : "w-20"}`} onClick={toggleRightSidebar}>
      {rightSidebarOpen ? "Close" : "History"}
    </Button>
  );
}
