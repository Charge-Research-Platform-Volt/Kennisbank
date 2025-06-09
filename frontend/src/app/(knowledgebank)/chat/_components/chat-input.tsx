"use client";

import type React from "react";
import { Textarea } from "@/components/ui/textarea";
import { Button } from "@/components/ui/button";
import { useChat } from "@/context/chatbot-provider";
import LoadingSpin from "@/components/loading-spin";
import { Toggle } from "@/components/ui/toggle";
import Link from "next/link";

export default function ChatInput() {
  const { handleSubmit, userInput, setUserInput, isLoading, setKnowledgeBankContent, knowledgeBankContent } = useChat();

  // Handle keyboard events
  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      const form = e.currentTarget.form;
      if (form) form.requestSubmit();
    }
  };

  return (
    <div className="sticky right-0 bottom-0 left-0 z-10 w-full">
      <form className="bg-sidebar mx-auto w-full max-w-[800px] rounded-t-lg border p-1.5" onSubmit={handleSubmit}>
        <Textarea
          value={userInput}
          onChange={(e) => {
            if (!isLoading) {
              setUserInput(e.target.value);
            }
          }}
          onKeyDown={handleKeyDown}
          className="mb-2 max-h-96 min-h-16 resize-none rounded-sm border-0 p-2 shadow-none focus-visible:ring-2"
          placeholder="Type your message here..."
          rows={2}
          autoFocus
          autoComplete="off"
          autoCorrect="off"
          spellCheck="false"
          disabled={isLoading}
        />

        <div className="flex items-center justify-between border-t pt-1.5">
          <div className="text-muted-foreground flex items-center gap-4 text-xs">
            <div className="flex items-center gap-1">
              <kbd className="rounded border border-gray-200 bg-gray-100 px-1.5 py-0.5 font-mono text-xs">Shift</kbd>
              <span>+</span>
              <kbd className="rounded border border-gray-200 bg-gray-100 px-1.5 py-0.5 font-mono text-xs">Enter</kbd>
              <span className="ml-1">For new line</span>
            </div>
            <div className="flex items-center gap-1">
              <kbd className="rounded border border-gray-200 bg-gray-100 px-1.5 py-0.5 font-mono text-xs">Enter</kbd>
              <span className="ml-1">To send</span>
            </div>
          </div>

          <div className="flex items-center gap-2">
            <Link type="button" className="text-xs" href={"/chat"}>
              <span>Clear</span>
            </Link>

            <Toggle
              variant="outline"
              aria-label="Toggle KnowledgeBank content"
              className="h-8 cursor-pointer px-3 py-0 text-xs font-medium transition-all data-[state=on]:border-purple-300 data-[state=on]:bg-purple-100 data-[state=on]:hover:bg-purple-200"
              onPressedChange={(checked) => setKnowledgeBankContent(checked)}
              pressed={knowledgeBankContent}
              disabled={isLoading}
            >
              <span className="flex items-center gap-1">KB</span>
            </Toggle>

            <Button type="submit" className="h-8 px-3 py-0 transition-all">
              {isLoading ? (
                <div className="flex items-center gap-2">
                  <span>Cancel</span>
                  <LoadingSpin />
                </div>
              ) : (
                <span>Send</span>
              )}
            </Button>
          </div>
        </div>
      </form>
    </div>
  );
}
