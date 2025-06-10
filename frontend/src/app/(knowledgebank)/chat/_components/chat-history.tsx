"use client";

import { Button } from "@/components/ui/button";
import { useSidebar } from "@/context/sidebar-provider";
import { useRouter } from "next/navigation";
import React from "react";

export default function ChatHistory() {
  const route = useRouter();
  const { toggleRightSidebar, rightSidebarOpen } = useSidebar();

  return (
    <div className="fixed top-2 right-2 z-20">
      <Button variant="outline" className={`${rightSidebarOpen ? "mr-32" : "mr-2"} transition-all`} onClick={() => route.push("/chat")}>
        New Chat
      </Button>

      <Button variant="outline" className={`transition-all ${rightSidebarOpen ? "w-14" : "w-20"}`} onClick={toggleRightSidebar}>
        {rightSidebarOpen ? "Close" : "History"}
      </Button>
    </div>
  );
}
