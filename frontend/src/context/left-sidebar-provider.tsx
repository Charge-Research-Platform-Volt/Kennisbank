"use client";

import React from "react";

// --------------------------------------------------------
// TYPES AND ENUMS
// -----------------------------------------------------

export type LeftSidebarContextType =
{
    // State and functions for the left sidebar
    leftSidebarOpen: boolean;
    setLeftSidebarOpen: (open: boolean) => void;
    toggleLeftSidebar: () => void;
};

// --------------------------------------------------------
// CONTEXT AND PROVIDER
// --------------------------------------------------------

// This context is used to manage the state of the sidebar
const LeftSidebarContent = React.createContext<LeftSidebarContextType | undefined>(undefined);

// This hook is used to access the sidebar context
export const useLeftSidebar = () =>
{
    const context: LeftSidebarContextType | undefined = React.useContext(LeftSidebarContent);

    if (!context) throw new Error("useSidebar must be used within a LeftSidebarProvider");

    return context;
};

// This component provides the sidebar context to its children
export const LeftSidebarProvider = ({ leftSidebarDefaultState, children }: { leftSidebarDefaultState: boolean; children: React.ReactNode }) =>
{
    // Sidebar states
    const [leftSidebarOpen, setLeftSidebarOpen] = React.useState<boolean>(leftSidebarDefaultState);

    // Sidebar control functions
    const toggleLeftSidebar = React.useCallback(() =>
    {
        const newState = !leftSidebarOpen;
        setLeftSidebarOpen(newState);

        // set cookie for left sidebar state
        document.cookie = `leftSidebar:state=${newState}; path=/; max-age=31536000; SameSite=None; Secure`;
    }, [leftSidebarOpen]);

    return (
        <LeftSidebarContent.Provider
            value={{
                leftSidebarOpen,
                setLeftSidebarOpen,
                toggleLeftSidebar
            }}
        >
            {children}
        </LeftSidebarContent.Provider>
    );
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)