"use client";

import React, { useEffect } from "react";
import { useChat } from "@/context/chatbot-provider";
import ChatIntro from "./chat-intro";
import ChatResponses from "./chat-responses";

export default function ChatCanvas({ showIntro, id }: { showIntro: boolean; id?: string }) {
  const { chatMessages, setCurrentChatId } = useChat();

  // If an ID is provided, set it as the current chat ID
  useEffect(() => {
    if (id) setCurrentChatId(id);
  }, [id, setCurrentChatId]);

  return <div className="mx-auto w-full max-w-[800px] flex-1">{showIntro ? <ChatIntro /> : <ChatResponses chatMessages={chatMessages} />}</div>;
}
