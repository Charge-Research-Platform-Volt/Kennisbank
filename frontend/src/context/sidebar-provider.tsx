"use client";

import React from "react";
import { useHotkeys } from "react-hotkeys-hook";
import { usePathname, useSearchParams, useRouter } from "next/navigation";
import Stack from "@/lib/stack";
import { ApiResponse } from "@/types/apiResponse.type";

// Enumerator for the different types of items to be displayed in the right sidebar
export enum MetadataTypeEnum { RESOURCE = "resource", PERSON = "person", ORGANISATION = "organisation" }

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

    // Creation date and Publication date (so they can be accessed in the Right Sidebar footer)
    creationDate: Date | null;
    setCreationDate: (date: Date | null) => void;
    publicationDate: Date | null;
    setPublicationDate : (date: Date | null) => void;
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

const FetchMetadataType = async (id: string): Promise<MetadataTypeEnum> => 
{
    if (id === '') return MetadataTypeEnum.RESOURCE;
    
    const response = await fetch(`/api/resources/${id}/metadata-type`,
        {
            method: 'GET',
            credentials: 'include'
        });
    
    const data: ApiResponse = await response.json();
    
    if (response.ok) 
    {
        switch (data.body) 
        {
            case 'resource': return MetadataTypeEnum.RESOURCE;
            case 'person': return MetadataTypeEnum.PERSON;
            case 'organisation': return MetadataTypeEnum.ORGANISATION;
            default: throw new Error("Invalid metadata type");
        }    
    }
    else 
    {
        throw new Error("Failed to fetch metadata type");
    }
}

// This component provides the sidebar context to its children
export const SidebarProvider = ({ leftSidebarDefaultState, children }: { leftSidebarDefaultState: boolean; children: React.ReactNode }) =>
{
    const pathname: string = usePathname();
    const searchParams = useSearchParams();
    const router = useRouter();
    
    // State for the selected item
    const [currentId, setCurrentId] = React.useState<string>('');
    const [currentType, setCurrentType] = React.useState<MetadataTypeEnum>(MetadataTypeEnum.RESOURCE);

    // Creation Date and Publication Date (so they can be accessed in the Right Sidebar footer)
    const [creationDate, setCreationDate] = React.useState<Date | null>(null)
    const [publicationDate, setPublicationDate] = React.useState<Date | null>(null)
    
    // States for prevs and nexts stacks
    const [prevs] = React.useState(() => new Stack<[string, MetadataTypeEnum]>());
    const [nexts] = React.useState(() => new Stack<[string, MetadataTypeEnum]>());
    
    const isEmptyPrevs = (): boolean => { return prevs.isEmpty(); }
    const isEmptyNexts = (): boolean => { return nexts.isEmpty(); }

    // Sidebar states
    const [leftSidebarOpen, setLeftSidebarOpen] = React.useState<boolean>(leftSidebarDefaultState);
    const [rightSidebarOpen, setRightSidebarOpen] = React.useState<boolean>(false);
    
    const leftSidebarState = leftSidebarOpen ? "expanded" : "collapsed";
    const rightSidebarState = rightSidebarOpen ? "expanded" : "collapsed";
    
    // URL Management
    const updateUrlWithId = React.useCallback((id: string) => 
    {
        const params = new URLSearchParams(searchParams.toString());
        params.set('id', id);
        const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
        router.replace(newUrl);
    }, [searchParams, pathname, router]);
    
    const clearUrlId = React.useCallback(() => 
    {
        if (searchParams?.get('id')) 
        {
            const params = new URLSearchParams(searchParams.toString());
            params.delete('id');
            const newUrl = params.toString() ? `${pathname}?${params.toString()}` : pathname;
            router.replace(newUrl);
        }
    }, [searchParams, pathname, router]);
    
    // Sidebar control functions
    const toggleLeftSidebar = React.useCallback(() =>
    {
        const newState = !leftSidebarOpen;
        setLeftSidebarOpen(newState);
        
        // Close right sidebar when opening left
        if (newState)
            setRightSidebarOpen(false);

        // set cookie for left sidebar state
        document.cookie = `leftSidebar:state=${newState}; path=/; max-age=31536000; SameSite=None; Secure`;
    }, [leftSidebarOpen]);
    
    const toggleRightSidebar = React.useCallback(() => 
    {
        const newState = !rightSidebarOpen;
        setRightSidebarOpen(newState);
        
        // Close left sidebar on right sidebar open
        if (newState)
            setLeftSidebarOpen(false);
    }, [rightSidebarOpen]);

    const openRightSidebar = React.useCallback((id: string, type: MetadataTypeEnum) => 
    {
        // Clear navigation history
        prevs.clear();
        nexts.clear();
        
        // Set current item
        setCurrentId(id);
        setCurrentType(type);
        
        // Close left sidebar and open right
        setLeftSidebarOpen(false);
        setRightSidebarOpen(true);
        
        // Update the URL
        updateUrlWithId(id);
    }, [prevs, nexts, updateUrlWithId]);
    
    const closeRightSidebar = React.useCallback(() => 
    {
        setRightSidebarOpen(false);
        setCurrentId('');
        
        // Clear navigation history
        prevs.clear()
        nexts.clear()
        
        // Clear the URL
        clearUrlId();
    }, [prevs, nexts, clearUrlId]);
    
    // Navigation functions
    const navigate = React.useCallback((id: string, type: MetadataTypeEnum) => 
    {
        nexts.clear();
        prevs.push([currentId, currentType]);
        
        setCurrentId(id);
        setCurrentType(type);
        
        // Update URL
        updateUrlWithId(id);
    }, [currentId, currentType, nexts, prevs, updateUrlWithId]);
    
    const navigateForward = React.useCallback(() => 
    {
        const next = nexts.pop();
        
        if (next) 
        {
            prevs.push([currentId, currentType]);
            setCurrentId(next[0]);
            setCurrentType(next[1]);
            updateUrlWithId(next[0]);
        }
    }, [nexts, prevs, currentId, currentType, updateUrlWithId]);
    
    const navigateBack = React.useCallback(() => 
    {
        const prev = prevs.pop();
        
        if (prev) 
        {
            nexts.push([currentId, currentType]);
            setCurrentId(prev[0]);
            setCurrentType(prev[1]);
            updateUrlWithId(prev[0]);
        }
    }, [nexts, prevs, currentId, currentType, updateUrlWithId]);
    
    // Initialize from URL on mount (only runs once)
    React.useEffect(() => 
    {
        const initializeFromUrl = async () => 
        {
            const id = searchParams.get('id');
            
            if (id && pathname === '/archive') 
            {
                try 
                {
                    const type = await FetchMetadataType(id);
                    setCurrentId(id);
                    setCurrentType(type);
                    setRightSidebarOpen(true);
                    setLeftSidebarOpen(false);
                }
                catch (error) 
                {
                    console.error('Failed to fetch metadata type: ', error);
                    clearUrlId();
                }
            }
        }
        
        initializeFromUrl();
    }, [clearUrlId]); // eslint-disable-line react-hooks/exhaustive-deps
    
    // Handle page navigation (pathname changes)
    React.useEffect(() => 
    {
        if (pathname !== '/archive')
            closeRightSidebar();
    }, [pathname, closeRightSidebar]);
    
    // Handle right sidebar cleanup when closed
    React.useEffect(() => 
    {
        if (!rightSidebarOpen && currentId) 
        {
            setCurrentId('');
            prevs.clear();
            nexts.clear();
            clearUrlId();
        }
    }, [rightSidebarOpen, currentId, prevs, nexts, clearUrlId]);
    
    // Hotkeys
    useHotkeys('esc', closeRightSidebar);

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

                //Creation & Publication Date
                creationDate,
                publicationDate,
                setCreationDate,
                setPublicationDate,
            }}
        >
        {children}
        </SidebarContent.Provider>
    );
};

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
