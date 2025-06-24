import { ChatBotProvider } from "@/context/chatbot-provider";

export default function ChatPageLayout({ children }: { children: React.ReactNode }) {
  return <ChatBotProvider>{children}</ChatBotProvider>;
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


