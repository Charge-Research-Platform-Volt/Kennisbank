"use client";
import { createContext, useContext, useEffect, useState } from "react";
import { useHotkeys } from "react-hotkeys-hook";
import { usePathname } from "next/navigation";

// Types
import type { ResourceResponse } from "@/types/resource.type";

type SidebarContextType = {
  // Selected document
  selectedDocument: ResourceResponse | null;
  setSelectedDocument: (file: ResourceResponse | null) => void;

  // State and functions for the left sidebar
  leftSidebarState: "expanded" | "collapsed";
  leftSidebarOpen: boolean;
  setLeftSidebarOpen: (open: boolean) => void;
  toggleLeftSidebar: () => void;

  // State and functions for the right sidebar
  rightSidebarState: "expanded" | "collapsed";
  rightSidebarOpen: boolean;
  setRightSidebarOpen: (open: boolean) => void;
  toggleRightSidebar: (document: ResourceResponse | null) => void;
};

// This context is used to manage the state of the sidebar
const SidebarContent = createContext<SidebarContextType | undefined>(undefined);

// This hook is used to access the sidebar context
export const useSidebar = () => {
  const context = useContext(SidebarContent);

  if (!context) throw new Error("useSidebar must be used within a SidebarProvider");

  return context;
};

// This component provides the sidebar context to its children
export const SidebarProvider = ({ children }: { children: React.ReactNode }) => {
  const pathname = usePathname();

  // State for the selected document
  const [selectedDocument, setSelectedDocument] = useState<ResourceResponse | null>(null);

  // State and functions for the left sidebar
  const [leftSidebarOpen, setLeftSidebarOpen] = useState(false);
  const leftSidebarState = leftSidebarOpen ? "expanded" : "collapsed";
  const toggleLeftSidebar = () => setLeftSidebarOpen((prev) => !prev);

  // State and functions for the right sidebar
  const [rightSidebarOpen, setRightSidebarOpen] = useState(false);
  const rightSidebarState = rightSidebarOpen ? "expanded" : "collapsed";
  const toggleRightSidebar = (document: ResourceResponse | null) => {
    if (document) setSelectedDocument(document);

    if (document && selectedDocument && selectedDocument.id !== document.id) {
      setRightSidebarOpen(true);
    } else {
      setRightSidebarOpen((prev) => !prev);
    }
  };

  useEffect(() => {
    setRightSidebarOpen(false);
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
