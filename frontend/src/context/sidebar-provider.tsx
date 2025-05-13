"use client";

import React from "react";
import { useHotkeys } from "react-hotkeys-hook";
import { usePathname } from "next/navigation";
import Stack from "@/lib/stack";

// Enumerator for the different types of items to be displayed in the right sidebar
export enum MetadataTypeEnum { RESOURCE = "resource", PERSON = "person", ORGANISTATION = "organisation" }

export type SidebarContextType =
{
    // Info about currently selected item (The ID in the database and the type of it)
    currentId: string;
    currentType: MetadataTypeEnum;
    openRightSidebar: (id: string, type: MetadataTypeEnum) => void;
    
    // Navigation
    navigateForward: () => void;
    navigateBack: () => void;
    navigate: (id: string, type: MetadataTypeEnum) => void;
    
    isEmptyPrevs: () => boolean;
    isEmptyNexts: () => boolean;

    // State and functions for the left sidebar
    leftSidebarState: "expanded" | "collapsed";
    leftSidebarOpen: boolean;
    setLeftSidebarOpen: (open: boolean) => void;
    toggleLeftSidebar: () => void;

    // State and functions for the right sidebar
    rightSidebarState: "expanded" | "collapsed";
    rightSidebarOpen: boolean;
    setRightSidebarOpen: (open: boolean) => void;
    toggleRightSidebar: () => void;
};

// This context is used to manage the state of the sidebar
const SidebarContent = React.createContext<SidebarContextType | undefined>(undefined);

// This hook is used to access the sidebar context
export const useSidebar = () =>
{
    const context: SidebarContextType | undefined = React.useContext(SidebarContent);

    if (!context) throw new Error("useSidebar must be used within a SidebarProvider");

    return context;
};

// This component provides the sidebar context to its children
export const SidebarProvider = ({ leftSidebarDefaultState, children }: { leftSidebarDefaultState: boolean; children: React.ReactNode }) =>
{
    const pathname: string = usePathname();

    // State for the selected item
    const [currentId, setCurrentId] = React.useState<string>("");
    const [currentType, setCurrentType] = React.useState<MetadataTypeEnum>(MetadataTypeEnum.RESOURCE);
    
    // States for prevs and nexts stacks
    const [prevs] = React.useState(() => new Stack<[string, MetadataTypeEnum]>());
    const [nexts] = React.useState(() => new Stack<[string, MetadataTypeEnum]>());
    
    const isEmptyPrevs = (): boolean => { return prevs.isEmpty(); }
    const isEmptyNexts = (): boolean => { return nexts.isEmpty(); }

    // - State and functions for the left sidebar
    const [leftSidebarOpen, setLeftSidebarOpen] = React.useState<boolean>(leftSidebarDefaultState);
    const leftSidebarState = leftSidebarOpen ? "expanded" : "collapsed";
    const toggleLeftSidebar = () =>
    {
        setLeftSidebarOpen((prev) => !prev);

        // set cookie for left sidebar state
        document.cookie = `leftSidebar:state=${!leftSidebarOpen}; path=/; max-age=31536000; SameSite=None; Secure`;
    };

    // - State and functions for the right sidebar
    const [rightSidebarOpen, setRightSidebarOpen] = React.useState<boolean>(false);
    const rightSidebarState = rightSidebarOpen ? "expanded" : "collapsed";
    const toggleRightSidebar = () =>
    {
        setRightSidebarOpen((prev) => !prev);
    };

    const openRightSidebar = (id: string, type: MetadataTypeEnum) => 
    {
        setCurrentId(id);
        setCurrentType(type);
        
        prevs.clear();
        nexts.clear();
        
        setRightSidebarOpen(true);
    }
    
    const navigate = (id: string, type: MetadataTypeEnum) =>
    {
        nexts.clear();
        
        prevs.push([currentId, currentType])
        
        setCurrentId(id);
        setCurrentType(type);
    }
    
    const navigateForward = (): void => 
    {
        const next: [string, MetadataTypeEnum] | undefined = nexts.pop();
        
        if (next) 
        {
            // Push current state to prevs
            prevs.push([currentId, currentType]);
        
            // Set current state as next
            setCurrentId(next[0]);
            setCurrentType(next[1]);
        }
    }
    
    const navigateBack = (): void => 
    {
        const prev: [string, MetadataTypeEnum] | undefined = prevs.pop();
        
        if (prev) 
        {
            // Push current state to nexts
            nexts.push([currentId, currentType]);
            
            // Set current state as prev
            setCurrentId(prev[0]);
            setCurrentType(prev[1]);
        }
    }
    
    // Effect for right sidebar open
    React.useEffect(() => 
    {
        // On right sidebar open
        if (rightSidebarOpen) 
        {
            // Close the right sidebar when no current ID is present
            if (currentId === "")
                setRightSidebarOpen(false);
                
            // Close the left sidebar on right sidebar open
            else
                setLeftSidebarOpen(false);
        }
    }, [rightSidebarOpen])
    
    // Effect for left sidebar open
    React.useEffect(() => 
    {
        // On left sidebar open
        if (leftSidebarOpen)
            setRightSidebarOpen(false);
    }, [leftSidebarOpen])

    // Effect for pathname
    React.useEffect(() => {
        if (pathname !== "/archive")
            setRightSidebarOpen(false);
    }, [pathname]);

    // Hotkey definitions
    useHotkeys("esc", () => {
        setRightSidebarOpen(false);
    });

    return (
        <SidebarContent.Provider
            value={
            {
                currentId,
                currentType,
                openRightSidebar,
                
                navigateForward,
                navigateBack,
                navigate,
                
                isEmptyPrevs,
                isEmptyNexts,

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
