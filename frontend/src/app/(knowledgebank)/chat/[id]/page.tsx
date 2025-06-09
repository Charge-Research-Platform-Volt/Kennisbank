import React from "react";
import ChatCanvas from "../_components/chat-canvas";
import ChatInput from "../_components/chat-input";
import ChatHistory from "../_components/chat-history";

export default async function ChatPage({ params }: Readonly<{ params: { id: string } }>) {
  const { id } = await params;
  return (
    <div className="relative flex h-full min-h-screen flex-col gap-14 overflow-y-auto px-2 pt-2">
      <ChatCanvas chatId={id} />
      <ChatInput chatId={id} />
      <ChatHistory />
    </div>
  );
}
