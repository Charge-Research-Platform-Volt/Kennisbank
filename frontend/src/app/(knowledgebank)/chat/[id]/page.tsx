import React from "react";
import ChatCanvas from "../_components/chat-canvas";
import ChatInput from "../_components/chat-input";
import ChatHistory from "../_components/chat-history";

export const dynamic = "force-dynamic";
export default async function ChatPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;

  return (
    <div className="relative flex h-full min-h-screen flex-col gap-14 overflow-y-auto px-2 pt-2">
      <ChatCanvas chatId={id} />
      <ChatInput chatId={id} />
      <ChatHistory />
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


