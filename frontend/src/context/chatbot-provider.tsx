"use client";
import { createContext, Dispatch, SetStateAction, useContext, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

// Types
import type { Messages } from "@/types/chatbot.type";
import { toast } from "sonner";
import { usePathname, useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";

type ChatBotContextType = {
  // * The userInput is the input provided by the user
  userInput: string;
  setUserInput: Dispatch<SetStateAction<string>>;

  // * The isLoading state indicates whether the chatbot is currently processing a request
  isLoading: boolean;
  setIsLoading: Dispatch<SetStateAction<boolean>>;

  // * The chatMessages is an array of messages exchanged between the user and the chatbot
  chatMessages: Messages;
  setChatMessages: Dispatch<SetStateAction<Messages>>;

  // * The handlePromptSubmit function is used to handle the submission of user input
  handlePromptSubmit: (e: React.FormEvent<HTMLFormElement>) => void;

  // * The messagesEndRef is a reference to the end of the chat messages, used for scrolling
  messagesEndRef: React.RefObject<HTMLDivElement | null>;

  // * The handleStreamForceStop function is used to stop the current streaming response
  handleStreamForceStop: () => void;

  // * The handleSubmit function is used to handle the form submission
  handleSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
  handleClearChat: () => void;

  // * The knowledgeBankContent state indicates whether the knowledge bank content is enabled
  knowledgeBankContent: boolean;
  setKnowledgeBankContent: Dispatch<SetStateAction<boolean>>;

  // * The currentChatId is the ID of the current chat session
  currentChatId: string;
  setCurrentChatId: Dispatch<SetStateAction<string>>;
};

// ------------------------------------------------------------------------------------

// This context is used to manage the state of the chatbot
const ChatBotContent = createContext<ChatBotContextType | undefined>(undefined);

// This hook is used to access the chatbot context
export const useChat = () => {
  const context: ChatBotContextType | undefined = useContext(ChatBotContent);

  if (!context) throw new Error("useChat must be used within a ChatBotProvider");

  return context;
};

// ------------------------------------------------------------------------------------

// This component provides the chatbot context to its children
export const ChatBotProvider = ({ children }: { children: React.ReactNode }) => {
  const router = useRouter();
  const pathname = usePathname();
  const queryClient = useQueryClient();

  const API_ENDPOINT = "/api/chat";
  const newChatSession = pathname === "/chat" ? true : false;

  // -- State -------------------------------------------------------------------------------------
  const [userInput, setUserInput] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [knowledgeBankContent, setKnowledgeBankContent] = useState<boolean>(true);
  const [chatMessages, setChatMessages] = useState<Messages>([]);
  const [currentChatId, setCurrentChatId] = useState<string>("");

  // This ref is used to scroll to the bottom of the chat messages
  const messagesEndRef = useRef<HTMLDivElement | null>(null);

  // -- SignalR connection ------------------------------------------------------------------------
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const currentStreamSubscription = useRef<signalR.ISubscription<string> | null>(null);

  // -- Functions ---------------------------------------------------------------------------------
  const handleSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();

    if (!isLoading) {
      handlePromptSubmit();
    } else {
      handleStreamForceStop();
    }
  };

  const handlePromptSubmit = () => {
    setIsLoading(() => true);

    // Check the user input
    if (!userInput.trim()) {
      toast.error("Please enter a message.");
      setIsLoading(() => false);
      return;
    }

    // Check if the connection is established
    if (!connection) {
      toast.error("Connection to the server is not established.");
      setIsLoading(() => false);
      return;
    }

    // If it's a new chat session, create a new chat ID
    // and navigate to the chat page
    if (newChatSession) {
      connection
        .invoke("CreateChat", userInput)
        .then((chatId: string) => {
          router.push(`/chat/${chatId}`);
          queryClient.invalidateQueries({ queryKey: ["chats-history"] });
        })
        .finally(() => {
          handleStream(userInput);
        });
    } else {
      // If not a new chat session, handle the streaming response
      handleStream(userInput);
    }
  };

  const handleStream = (userInput: string) => {
    // Check if the connection is established
    if (!connection) {
      toast.error("Connection to the server is not established.");
      setIsLoading(() => false);
      return;
    }

    const userMessageText = userInput;
    setUserInput("");

    // Add the user input to the chat messages
    const userId = Date.now().toString() + "-user";
    setChatMessages((prev) => {
      return [...prev, { id: userId, sender: "user", message: userMessageText }];
    });

    // Create a placeholder for the AI's streaming response
    const systemId = Date.now().toString() + "-system";
    setChatMessages((prev) => {
      return [...prev, { id: systemId, sender: "system", message: "" }];
    });

    // Start the streaming response
    try {
      currentStreamSubscription.current = connection.stream("StreamAiResponse", userMessageText, knowledgeBankContent, currentChatId).subscribe({
        next: (chunk) => {
          setChatMessages((prev) => prev.map((msg) => (msg.id === systemId ? { ...msg, message: msg.message + chunk } : msg)));
        },
        error: (error) => {
          setIsLoading(() => false);
          console.error("Error during streaming:", error);
          toast.error("Error during streaming");
        },
        complete: () => {
          setIsLoading(() => false);
        },
      });
    } catch (error) {
      setIsLoading(() => false);
      console.error("Error starting streaming:", error);
      toast.error("Error starting streaming");
      setChatMessages((prev) => prev.map((msg) => (msg.id === systemId ? { ...msg, message: msg.message + "\n\n[Error starting stream]" } : msg)));
    }
  };

  const handleStreamForceStop = () => {
    if (!isLoading) return;

    // Stop the current stream subscription
    if (currentStreamSubscription.current) {
      currentStreamSubscription.current.dispose();
      currentStreamSubscription.current = null;
    }

    // Set loading state to false
    setIsLoading(() => false);

    // Update the last message to indicate that the stream was stopped
    setChatMessages((prev) => {
      return prev.map((msg) => (msg.id === chatMessages[chatMessages.length - 1].id ? { ...msg, message: msg.message + "\n\n[Stream stopped]" } : msg));
    });
  };

  const handleClearChat = () => {
    setChatMessages([]);
    setUserInput("");
    setIsLoading(false);
    if (currentStreamSubscription.current) {
      currentStreamSubscription.current.dispose();
      currentStreamSubscription.current = null;
    }
  };

  // -- UseEffects --------------------------------------------------------------------------------

  // This useEffect is used to handle the SignalR connection
  useEffect(() => {
    if (typeof window !== "undefined") {
      try {
        // Create a new SignalR connection
        // For debugging purposes, you can remove the configureLogging(signalR.LogLevel.None) or set it to signalR.LogLevel.Debug
        const newConnection = new signalR.HubConnectionBuilder().withUrl(API_ENDPOINT).withAutomaticReconnect().build();
        setConnection(newConnection);
      } catch (error) {
        console.error("Error creating SignalR connection:", error);
        toast.error("Error creating to the server");
      }
    }
  }, []);

  useEffect(() => {
    if (connection) {
      connection
        .start()
        .then(() => console.log("SignalR Connected!"))
        .catch((e) => console.error("SignalR Connection Error: ", e));

      // Capture the current value of the ref
      const subscription = currentStreamSubscription.current;

      return () => {
        subscription?.dispose(); // Clean up any active subscription when connection is lost or component unmounts
        connection.stop().then(() => console.log("SignalR Disconnected."));
      };
    }
  }, [connection, router, queryClient]);

  // This useEffect is used to scroll to the bottom of the chat messages when they change
  useEffect(() => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: "smooth" });
    }
  }, [chatMessages]);

  // This useEffect is used to clear the chat when the pathname is "/chat"
  useEffect(() => {
    if (newChatSession) handleClearChat();
  }, [newChatSession]);

  return (
    <ChatBotContent.Provider
      value={{
        userInput,
        setUserInput,
        isLoading,
        setIsLoading,
        chatMessages,
        setChatMessages,
        handlePromptSubmit,
        messagesEndRef,
        handleStreamForceStop,
        handleSubmit,
        handleClearChat,
        knowledgeBankContent,
        setKnowledgeBankContent,
        currentChatId,
        setCurrentChatId,
      }}
    >
      {children}
    </ChatBotContent.Provider>
  );
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
