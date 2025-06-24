import { ChatBotProvider } from "@/context/chatbot-provider";

export default function ChatPageLayout({ children }: { children: React.ReactNode }) {
  return <ChatBotProvider>{children}</ChatBotProvider>;
}
