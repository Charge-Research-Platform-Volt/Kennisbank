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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


