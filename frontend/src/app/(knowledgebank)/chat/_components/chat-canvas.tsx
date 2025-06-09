"use client";

import React, { useEffect } from "react";
import { useChat } from "@/context/chatbot-provider";
import ChatResponses from "./chat-responses";
import { NewApiResponse } from "@/types/apiResponse.type";
import { Messages } from "@/types/chatbot.type";
import { toast } from "sonner";
import { useQuery } from "@tanstack/react-query";

export default function ChatCanvas({ chatId }: { chatId?: string }) {
  const { chatMessages, setChatMessages } = useChat();

  const { data, isError } = useQuery({
    queryKey: ["chat-messages", chatId],
    queryFn: async () => {
      if (!chatId) {
        throw new Error("Chat ID is required to fetch messages.");
      }

      const response = await fetch(`/api/AI/messages/${chatId}`, {
        method: "GET",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
      });

      if (!response.ok) {
        throw new Error("Failed to fetch chat messages");
      }

      const result: NewApiResponse<{ messages: Messages }> = await response.json();

      if (!result.success) {
        throw new Error("Failed to fetch chat messages");
      }

      return result.body.messages;
    },
    enabled: !!chatId,
    refetchOnWindowFocus: false,
    refetchOnReconnect: false,
  });

  useEffect(() => {
    if (data) {
      setChatMessages(data);
    }
  }, [data, setChatMessages]);

  useEffect(() => {
    if (isError) {
      toast.error("An error occurred while fetching chat messages.");
    }
  }, [isError]);

  return <ChatResponses chatMessages={chatMessages} />;
}
