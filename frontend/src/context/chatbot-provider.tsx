"use client";
import { createContext, useContext, useEffect, useRef, useState } from "react";
import * as signalR from "@microsoft/signalr";

// Types
import type { Messages } from "@/types/chatbot.type";
import { toast } from "sonner";

// Sidebar types
// type SidebarType = QuickViewType | ChatHistoryType;

// interface QuickViewType {
//   id: string;
// }

// interface ChatHistoryType {
//   id: string;
// }

type ChatBotContextType = {
  // * The userInput is the input provided by the user
  userInput: string;
  setUserInput: React.Dispatch<React.SetStateAction<string>>;

  // * The isLoading state indicates whether the chatbot is currently processing a request
  isLoading: boolean;
  setIsLoading: React.Dispatch<React.SetStateAction<boolean>>;

  // * The chatMessages is an array of messages exchanged between the user and the chatbot
  chatMessages: Messages;
  setChatMessages: React.Dispatch<React.SetStateAction<Messages>>;

  // * The handlePromptSubmit function is used to handle the submission of user input
  handlePromptSubmit: (e: React.FormEvent<HTMLFormElement>) => void;

  // * The messagesEndRef is a reference to the end of the chat messages, used for scrolling
  messagesEndRef: React.RefObject<HTMLDivElement | null>;

  // * The handleStreamForceStop function is used to stop the current streaming response
  handleStreamForceStop: () => void;

  handleSubmit: (e: React.FormEvent<HTMLFormElement>) => void;
  handleClearChat: () => void;

  knowledgeBankContent: boolean;
  setKnowledgeBankContent: React.Dispatch<React.SetStateAction<boolean>>;
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
  const API_ENDPOINT = "http://localhost:8080/chat";

  // -- State -------------------------------------------------------------------------------------
  const [userInput, setUserInput] = useState<string>("");
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [knowledgeBankContent, setKnowledgeBankContent] = useState<boolean>(true);
  const [chatMessages, setChatMessages] = useState<Messages>([]);

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
      currentStreamSubscription.current = connection.stream("StreamAiResponse", userMessageText, knowledgeBankContent).subscribe({
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
        const newConnection = new signalR.HubConnectionBuilder().withUrl(API_ENDPOINT).withAutomaticReconnect().configureLogging(signalR.LogLevel.None).build();
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
  }, [connection]);

  useEffect(() => {
    if (messagesEndRef.current) {
      messagesEndRef.current.scrollIntoView({ behavior: "smooth" });
    }
  }, [chatMessages]);

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
      }}
    >
      {children}
    </ChatBotContent.Provider>
  );
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)

// [
//     {
//       id: Date.now().toString() + "-system",
//       sender: "system",
//       message: `# Heading 1

// ## Heading 2

// ### Heading 3

// #### Heading 4

// ##### Heading 5

// ###### Heading 6

// This is a paragraph with some **bold text**, some *italic text*, and some ~~strikethrough text~~.
// Here is a [link to OpenAI](https://www.openai.com).

// ---

// > This is a blockquote.
// > It can span multiple lines.

// ---
// ## List Example

// - Unordered list item 1
// - Unordered list item 2
//   - Nested unordered item
// - Unordered list item 3

// ## Ordered List Example

// 1. Ordered list item 1
// 2. Ordered list item 2
//    1. Nested ordered item
// 3. Ordered list item 3

// ---
// ## Code Example

// Here is an inline code example: \`console.log('Hello, world!');\`

// \`\`\`python
// # This is a code block
// def hello():
//     print("Hello, world!")
// \`\`\`

// ## Table Example

// | Header 1 | Header 2 | Header 3 |
// |----------|----------|----------|
// | Row 1    | Data     | More     |
// | Row 2    | Data     | More     |
// | Row 3    | Data     | More     |
// `,
//     },
//   ]
