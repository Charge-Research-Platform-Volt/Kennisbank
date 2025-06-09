import React from "react";
import ChatInput from "./_components/chat-input";
import ChatHistory from "./_components/chat-history";
import ChatIntro from "./_components/chat-intro";

export default function ChatPage() {
  return (
    <div className="relative flex h-full min-h-screen flex-col gap-14 overflow-y-auto px-2 pt-2">
      <ChatIntro />
      <ChatInput />
      <ChatHistory />
    </div>
  );
}
