import React from "react";
import ChatInput from "./_components/chat-input";
import ChatCanvas from "./_components/chat-canvas";
import ChatHistory from "./_components/chat-history";

export default function ChatPage() {
  return (
    <div className="relative flex h-full min-h-screen flex-col gap-14 overflow-y-auto px-2 pt-2">
      <ChatCanvas showIntro={true} />
      <ChatInput />
      <ChatHistory />
    </div>
  );
}
