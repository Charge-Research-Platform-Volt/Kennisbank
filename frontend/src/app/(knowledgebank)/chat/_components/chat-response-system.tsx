import { ChatMessage } from "@/types/chatbot.type";
import type React from "react";
import { MemoizedMarkdown } from "./markdown";

export default function ChatResponseSystem({ chat }: { chat: ChatMessage }) {
  return (
    <div className="fade-in-animaiton mt-12 flex w-full justify-start">
      <div className={`w-full max-w-[700px]`}>
        <MemoizedMarkdown id={chat.id} content={chat.content} />
      </div>
    </div>
  );
}
