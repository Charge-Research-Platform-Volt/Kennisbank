import ChatbotSidebar from "@/components/sidebars/chatbot-sidebar/chatbot-sidebar";
import { ChatBotProvider } from "@/context/chatbot-provider";
import { ChatbotSidebarProvider } from "@/context/chatbot-sidebar-provider";

export default function ChatPageLayout({ children }: { children: React.ReactNode }) {
    return (
        <ChatbotSidebarProvider>
            <ChatBotProvider>
                <div className="flex min-h-screen">
                    <div className="flex-1">
                        {children}
                    </div>
                    <ChatbotSidebar />
                </div>
            </ChatBotProvider>
        </ChatbotSidebarProvider>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


