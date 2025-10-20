"use client"

import React from "react";
import { useHotkeys } from "react-hotkeys-hook";
import { usePathname, useSearchParams, useRouter } from "next/navigation";
import Stack from "@/lib/stack";
import { ApiResponse } from "@/types/apiResponse.type";
import { useLeftSidebar } from "./left-sidebar-provider";
import { toast } from "sonner"

// --------------------------------------------------------
// TYPES AND ENUMS
// --------------------------------------------------------

// Enumerator for the different types of items to be displayed in the archive sidebar
export enum MetadataTypeEnum
{
    RESOURCE = "resource",
    PERSON = "person",
    ORGANISATION = "organisation",
}

export type ArchiveSidebarContextType = 
{
    // Info about the currently selected item (DB ID and type)
    currentId: string;
    currentType: MetadataTypeEnum;
    openArchiveSidebar: (id: string, type: MetadataTypeEnum) => void;
    
    // Navigation
    navigateForward: () => void;
    navigateBack: () => void;
    navigate: (id: string, type: MetadataTypeEnum) => void;
    
    isEmptyPrevs: () => boolean;
    isEmptyNexts: () => boolean;
    
    // State and its functions
    archiveSidebarOpen: boolean;
    setArchiveSidebarOpen: (open: boolean) => void;
    toggleArchiveSidebar: () => void;
    
    // Editting mode
    editMode: boolean;
    setEditMode: (toggle: boolean) => void;
    
    // Creation date and Publication date
    creationDate: Date | null;
    setCreationDate: (date: Date | null) => void;
    publicationDate: Date | null;
    setPublicationDate: (date: Date | null) => void;
}

// --------------------------------------------------------
// CONTEXT AND PROVIDER
// --------------------------------------------------------

// The context to be used to manage the state of the sidebar
const ArchiveSidebarContext = React.createContext<ArchiveSidebarContextType | undefined>(undefined);

// Hook to access the archive sidebar context
export const useArchiveSidebar = () => 
{
    const context: ArchiveSidebarContextType | undefined = React.useContext(ArchiveSidebarContext);
    
    if (!context) throw new Error("useArchiveSidebar must be used within a ArchiveSidebarProvider");
    
    return context;
}

// Fetches the metadata type for the given ID
export const FetchMetadataType = async (id: string): Promise<MetadataTypeEnum> => 
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
            case 'resource':        return MetadataTypeEnum.RESOURCE;
            case 'person':          return MetadataTypeEnum.PERSON;
            case 'organisation':    return MetadataTypeEnum.ORGANISATION;
            default:                throw new Error("Invalid metadata type");
        }
    }
    else 
    {
        throw new Error("Failed to fetch metadata type");
    }
}

// The provider provides the sidebar context to its children
export const ArchiveSidebarProvider = ({ archiveSidebarDefaultState = false, children }: { archiveSidebarDefaultState?: boolean; children: React.ReactNode}) => 
{
    const pathname = usePathname();
    const searchParams = useSearchParams();
    const router = useRouter();
    const { leftSidebarOpen, setLeftSidebarOpen } = useLeftSidebar();
    
    // State for the selected item
    const [currentId, setCurrentId] = React.useState<string>('');
    const [currentType, setCurrentType] = React.useState<MetadataTypeEnum>(MetadataTypeEnum.RESOURCE);
    
    // Creation Date and Publication Date
    const [creationDate, setCreationDate] = React.useState<Date | null>(null);
    const [publicationDate, setPublicationDate] = React.useState<Date | null>(null);
    
    // States for prevs and nexts stacks
    const [prevs] = React.useState(() => new Stack<[string, MetadataTypeEnum]>());
    const [nexts] = React.useState(() => new Stack<[string, MetadataTypeEnum]>());
    
    const isEmptyPrevs = (): boolean => { return prevs.isEmpty(); }
    const isEmptyNexts = (): boolean => { return nexts.isEmpty(); }
    
    // Sidebar states
    const [archiveSidebarOpen, setArchiveSidebarOpen] = React.useState<boolean>(archiveSidebarDefaultState);
    const [editMode, setEditMode] = React.useState<boolean>(false);
    
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
    
    const toggleArchiveSidebar = React.useCallback(() => 
    {
        const newState = !archiveSidebarOpen;
        setArchiveSidebarOpen(newState);
        
        // Close left sidebar on archive sidebar open
        if (newState)
            setLeftSidebarOpen(false);
        // Disable edit mode when closing archive sidebar
        else
            setEditMode(false);
    }, [archiveSidebarOpen]);
    
    const openArchiveSidebar = React.useCallback((id: string, type: MetadataTypeEnum) => 
    {
        // Clear navigation history
        prevs.clear();
        nexts.clear();
        
        // Set current item
        setCurrentId(id);
        setCurrentType(type);
        
        // Close left sidebar and open archive sidebar
        setLeftSidebarOpen(false);
        setArchiveSidebarOpen(true);
        
        // Update the URL
        updateUrlWithId(id);
        
        // Disable edit mode
        setEditMode(false);
    }, [prevs, nexts, updateUrlWithId]);
    
    const closeArchiveSidebar = React.useCallback(() => 
    {
        setArchiveSidebarOpen(false);
        setCurrentId('');
        setEditMode(false);
        
        // Clear navigation history
        prevs.clear();
        nexts.clear();
        
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
        setEditMode(false);
        
        // Update URL
        updateUrlWithId(id);
    }, [currentId, currentType, nexts, prevs, updateUrlWithId]);
    
    const navigateForward = React.useCallback(() => 
    {
        const next = nexts.pop();
        setEditMode(false);
        
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
        setEditMode(false);
        
        if (prev) 
        {
            nexts.push([currentId, currentType]);
            setCurrentId(prev[0]);
            setCurrentType(prev[1]);
            updateUrlWithId(prev[0]);
        }
    }, [nexts, prevs, currentId, currentType, updateUrlWithId]);
    
    // Closes the archive sidebar when the left sidebar opens
    React.useEffect(() => 
    {
        if (leftSidebarOpen) 
        {
            setArchiveSidebarOpen(false);
            setEditMode(false);
        }
    }, [leftSidebarOpen]);
    
    // Initialize from URL on mount (only runs once)
    React.useEffect(() => 
    {
        const initializeFromUrl = async () => 
        {
            const id = searchParams.get('id');
            
            if (id) 
            {
                try 
                {
                    const type = await FetchMetadataType(id);
                    setCurrentId(id);
                    setCurrentType(type);
                    setArchiveSidebarOpen(true);
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
    
    // Handle right sidebar clearnup when closed
    React.useEffect(() => 
    {
        if (!archiveSidebarOpen && currentId) 
        {
            setCurrentId('');
            prevs.clear();
            nexts.clear();
            clearUrlId();
        }
    }, [archiveSidebarOpen, currentId, prevs, nexts, clearUrlId]);
    
    // Hotkeys
    useHotkeys('esc', closeArchiveSidebar);
    
    return (
        <ArchiveSidebarContext.Provider
            value = {
            {
                currentId,
                currentType,
                openArchiveSidebar,
                navigateForward,
                navigateBack,
                navigate,
                isEmptyPrevs,
                isEmptyNexts,
                archiveSidebarOpen,
                setArchiveSidebarOpen,
                toggleArchiveSidebar,
                editMode,
                setEditMode,
                creationDate,
                setCreationDate,
                publicationDate,
                setPublicationDate
            }}
        >
            {children}
        </ArchiveSidebarContext.Provider>
    )
}