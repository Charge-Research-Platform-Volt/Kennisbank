import { ChatMessage } from "@/types/chatbot.type";
import type React from "react";

export default function ChatResponseUser({ chat }: { chat: ChatMessage }) {
  return (
    <div className="fade-in-animaiton mt-12 flex w-full justify-end">
      <div className="bg-sidebar w-full max-w-[700px] rounded-md border p-5 font-medium">{chat.content}</div>
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


