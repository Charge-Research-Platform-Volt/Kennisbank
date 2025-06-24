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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


