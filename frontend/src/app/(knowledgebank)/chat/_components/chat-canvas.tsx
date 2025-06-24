"use client";

import React, { useCallback, useEffect, useState } from "react";
import { useChat } from "@/context/chatbot-provider";
import ChatResponses from "./chat-responses";
import { NewApiResponse } from "@/types/apiResponse.type";
import { Messages } from "@/types/chatbot.type";
import { toast } from "sonner";
import ChatLoading from "./chat-loading";
import { useRouter } from "next/navigation";

export default function ChatCanvas({ chatId }: { chatId?: string }) {
  const route = useRouter();
  const { chatMessages, isLoading, setChatMessages } = useChat();
  const [fetchLoading, setFetchLoading] = useState<boolean>(false);

  const fetchChatMessages = useCallback(async () => {
    if (isLoading || !chatId) return;

    setChatMessages([]);
    try {
      setFetchLoading(true);
      const response = await fetch(`/api/AI/messages/${chatId}`, {
        method: "GET",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
      });

      if (!response.ok) throw new Error("Failed to fetch chat messages");

      const result: NewApiResponse<{ messages: Messages }> = await response.json();

      if (result.success) {
        setChatMessages(result.body.messages);
      } else {
        throw new Error("Failed to fetch chat messages");
      }
    } catch {
      toast.error("Failed to fetch chat messages");
      route.push("/chat");
    } finally {
      setFetchLoading(false);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [chatId, setChatMessages]);

  useEffect(() => {
    fetchChatMessages();
  }, [fetchChatMessages]);

  return fetchLoading ? <ChatLoading /> : <ChatResponses chatMessages={chatMessages} />;
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


