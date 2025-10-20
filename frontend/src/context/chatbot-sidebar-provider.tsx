"use client"

import React from "react";
import { useHotkeys } from "react-hotkeys-hook";
import { useLeftSidebar } from "./left-sidebar-provider";

// --------------------------------------------------------
// TYPES AND ENUMS
// --------------------------------------------------------

export type ChatbotSidebarContextType = 
{
    // State and its functions
    chatbotSidebarOpen: boolean;
    setChatbotSidebarOpen: (open: boolean) => void;
    openChatbotSidebar: () => void;
    closeChatbotSidebar: () => void;
    toggleChatbotSidebar: () => void;
}

// --------------------------------------------------------
// CONTEXT AND PROVIDER
// --------------------------------------------------------

// The context to be used to manage the state of the sidebar
const ChatbotSidebarContext = React.createContext<ChatbotSidebarContextType | undefined>(undefined);

// Hook to access the chatbot sidebar context
export const useChatbotSidebar = () => 
{
    const context: ChatbotSidebarContextType | undefined = React.useContext(ChatbotSidebarContext);
    
    if (!context) throw new Error("useChatbotSidebar must be used within a ChatbotSidebarProvider");
    
    return context;
}

// The provider provides the sidebar context to its children
export const ChatbotSidebarProvider = ({ chatbotSidebarDefaultState = false, children }: { chatbotSidebarDefaultState?: boolean; children: React.ReactNode }) => 
{
    const { leftSidebarOpen, setLeftSidebarOpen } = useLeftSidebar();
    
    // Sidebar states
    const [chatbotSidebarOpen, setChatbotSidebarOpen] = React.useState<boolean>(chatbotSidebarDefaultState);
    
    // --- CALLBACKS
    
    const toggleChatbotSidebar = React.useCallback(() => 
    {
        const newState = !chatbotSidebarOpen;
        setChatbotSidebarOpen(newState);
        
        // Close left sidebar on chatbot sidebar open
        if (newState)
            setLeftSidebarOpen(false);
    }, [chatbotSidebarOpen])
    
    const openChatbotSidebar = React.useCallback(() => 
    {
        // Close left sidebar and open chatbot sidebar
        setLeftSidebarOpen(false);
        setChatbotSidebarOpen(true);
    }, []);
    
    const closeChatbotSidebar = React.useCallback(() => 
    {
        setChatbotSidebarOpen(false);
    }, []);
    
    // --- EFFECTS
    
    // Close the chatbot sidebar when the left sidebar opens
    React.useEffect(() => 
    {
        if (leftSidebarOpen) 
        {
            setChatbotSidebarOpen(false);
        }
    }, [leftSidebarOpen]);
    
    // --- HOTKEYS
    
    // Closing hotkey
    useHotkeys('esc', closeChatbotSidebar);
    
    return (
        <ChatbotSidebarContext.Provider
            value = {
            {
                chatbotSidebarOpen,
                setChatbotSidebarOpen,
                openChatbotSidebar,
                closeChatbotSidebar,
                toggleChatbotSidebar
            }}
        >
            {children}
        </ChatbotSidebarContext.Provider>
    )
}