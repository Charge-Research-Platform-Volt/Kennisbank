"use client";

import { useChat } from "@/context/chatbot-provider";
import React from "react";

export default function ChatIntro() {
  const { setUserInput } = useChat();

  const questions = [
    "What is Charge?",
    "What are the main political parties in the European Union?",
    "How does the European Parliament work?",
    "What is Brexit and how did it affect Europe?",
    "Who are the current leaders of major European countries?",
    "What is the Schengen Agreement?",
    "How does the European Union make decisions?",
    "What is the role of the European Commission?",
  ];

  return (
    <div className="mx-auto w-full max-w-[800px] flex-1">
      <div className="fade-in-animaiton mt-14 flex h-full w-full flex-col justify-center">
        <h1 className="text-2xl font-bold text-gray-800">How can I help you?</h1>
        <ul className="mt-4 list-disc pl-5 text-gray-600">
          {questions.map((question, index) => (
            <li
              key={index}
              className="mb-2 cursor-pointer hover:underline"
              onClick={() => {
                setUserInput(question);
              }}
            >
              {question}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


