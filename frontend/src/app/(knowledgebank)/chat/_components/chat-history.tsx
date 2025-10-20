"use client";

import { Button } from "@/components/ui/button";
import { useChatbotSidebar } from "@/context/chatbot-sidebar-provider";
import { useRouter } from "next/navigation";
import React from "react";

export default function ChatHistory() {
  const route = useRouter();
  const { toggleChatbotSidebar, chatbotSidebarOpen } = useChatbotSidebar();

  return (
    <div className="fixed top-2 right-2 z-20">
      <Button variant="outline" className={`${chatbotSidebarOpen ? "mr-32" : "mr-2"} transition-all`} onClick={() => route.push("/chat")}>
        New Chat
      </Button>

      <Button variant="outline" className={`transition-all ${chatbotSidebarOpen ? "w-14" : "w-20"}`} onClick={toggleChatbotSidebar}>
        {chatbotSidebarOpen ? "Close" : "History"}
      </Button>
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


