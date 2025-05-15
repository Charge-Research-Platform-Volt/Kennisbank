import { Messages } from "@/types/chatbot.type";
import React from "react";
import ChatResponseUser from "./chat-response-user";
import ChatResponseSystem from "./chat-response-system";
import { useChat } from "@/context/chatbot-provider";

export default function ChatResponses({ chatMessages }: { chatMessages: Messages }) {
  const { messagesEndRef } = useChat();

  return (
    <>
      {chatMessages.map((chat) => (chat.sender === "user" ? <ChatResponseUser key={chat.id} chat={chat} /> : <ChatResponseSystem key={chat.id} chat={chat} />))}
      <div ref={messagesEndRef} />
    </>
  );
}
