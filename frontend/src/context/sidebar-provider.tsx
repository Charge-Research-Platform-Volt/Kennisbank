"use client";
import { createContext, useContext, useEffect, useState } from "react";
import { useHotkeys } from "react-hotkeys-hook";
import { usePathname } from "next/navigation";

// Types
import type { ResourceWithTagsResponse } from "@/types/resource.type";

type SidebarContextType = {
  // Selected document
  selectedDocument: ResourceWithTagsResponse | null;
  setSelectedDocument: (file: ResourceWithTagsResponse | null) => void;

  // State and functions for the left sidebar
  leftSidebarState: "expanded" | "collapsed";
  leftSidebarOpen: boolean;
  setLeftSidebarOpen: (open: boolean) => void;
  toggleLeftSidebar: () => void;

  // State and functions for the right sidebar
  rightSidebarState: "expanded" | "collapsed";
  rightSidebarOpen: boolean;
  setRightSidebarOpen: (open: boolean) => void;
  toggleRightSidebar: (document: ResourceWithTagsResponse | null) => void;
};

// This context is used to manage the state of the sidebar
const SidebarContent = createContext<SidebarContextType | undefined>(undefined);

// This hook is used to access the sidebar context
export const useSidebar = () => {
  const context: SidebarContextType | undefined = useContext(SidebarContent);

  if (!context) throw new Error("useSidebar must be used within a SidebarProvider");

  return context;
};

// This component provides the sidebar context to its children
export const SidebarProvider = ({ leftSidebarDefaultState, children }: { leftSidebarDefaultState: boolean; children: React.ReactNode }) => {
  const pathname: string = usePathname();

  // State for the selected document
  const [selectedDocument, setSelectedDocument] = useState<ResourceWithTagsResponse | null>(null);

  // - State and functions for the left sidebar
  const [leftSidebarOpen, setLeftSidebarOpen] = useState<boolean>(leftSidebarDefaultState);
  const leftSidebarState = leftSidebarOpen ? "expanded" : "collapsed";
  const toggleLeftSidebar = () => {
    setLeftSidebarOpen((prev) => !prev);

    // set cookie for left sidebar state
    document.cookie = `leftSidebar:state=${!leftSidebarOpen}; path=/; max-age=31536000; SameSite=None; Secure`;
  };

  // - State and functions for the right sidebar
  const [rightSidebarOpen, setRightSidebarOpen] = useState<boolean>(false);
  const rightSidebarState = rightSidebarOpen ? "expanded" : "collapsed";
  const toggleRightSidebar = (document: ResourceWithTagsResponse | null) => {
    if (document) setSelectedDocument(document);

    if (document && selectedDocument && selectedDocument.id !== document.id) {
      setRightSidebarOpen(true);
    } else {
      setRightSidebarOpen((prev) => !prev);
    }
  };

  useEffect(() => {
    if (pathname !== "/archive") {
      setRightSidebarOpen(false);
    }
  }, [pathname]);

  useHotkeys("esc", () => {
    setRightSidebarOpen(false);
  });

  return (
    <SidebarContent.Provider
      value={{
        selectedDocument,
        setSelectedDocument,

        // Left sidebar
        leftSidebarState,
        leftSidebarOpen,
        setLeftSidebarOpen,
        toggleLeftSidebar,

        // Right sidebar
        rightSidebarState,
        rightSidebarOpen,
        setRightSidebarOpen,
        toggleRightSidebar,
      }}
    >
      {children}
    </SidebarContent.Provider>
  );
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
