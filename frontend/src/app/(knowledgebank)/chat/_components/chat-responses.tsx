import { Messages } from "@/types/chatbot.type";
import React from "react";
import ChatResponseUser from "./chat-response-user";
import ChatResponseSystem from "./chat-response-system";
import { useChat } from "@/context/chatbot-provider";

export default function ChatResponses({ chatMessages }: { chatMessages: Messages }) {
  const { messagesEndRef } = useChat();

  return (
    <div className="mx-auto w-full max-w-[800px] flex-1">
      {chatMessages.map((chat) => (chat.messageRole === "User" ? <ChatResponseUser key={chat.id} chat={chat} /> : <ChatResponseSystem key={chat.id} chat={chat} />))}
      <div ref={messagesEndRef} />
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


