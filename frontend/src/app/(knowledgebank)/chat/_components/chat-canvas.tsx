"use client";

import React from "react";
import { useChat } from "@/context/chatbot-provider";
import ChatIntro from "./chat-intro";
import ChatResponses from "./chat-responses";

export default function ChatCanvas() {
  const { chatMessages } = useChat();
  return <div className="mx-auto w-full max-w-[800px] flex-1">{chatMessages.length === 0 ? <ChatIntro /> : <ChatResponses chatMessages={chatMessages} />}</div>;
}
