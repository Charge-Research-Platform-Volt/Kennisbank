'use client'

import React from 'react';
import { useTabSwitchWarning } from '@/hooks/useTabsSwitchWarning';
import { useNavigationWarning } from '@/hooks/useNavigationWarning';

// Create the context
type TabsContextType = {
    setFormChanged: (changed: boolean) => void;
    requestTabChange: (newTab: string) => void;
    hasUnsavedChanges: boolean;
};
    
const TabsContext = React.createContext<TabsContextType | undefined>(undefined);

interface TabsProviderProps {
    children: React.ReactNode;
    defaultTab: string;
    onTabChange: (tab: string) => void;
}

/**
 * @summary Provides a context for tabs with forms that require the UnsavedDialog to work.
 * @param children The children of this component
 * @param defaultTab The default tab to be opened
 * @param onTabChange This function is called when a different tab is opened
 */
export function TabsProvider({children, defaultTab, onTabChange}: TabsProviderProps) {
    const [activeTab, setActiveTab] = React.useState<string>(defaultTab);
    const [hasUnsavedChanges, setHasUnsavedChanges] = React.useState<boolean>(false);
    
    // Function to handle tab changes, which will clear unsaved changes state
    const handleTabChange = React.useCallback((tab: string) => {
        // First, update the active tab
        setActiveTab(tab);
        
        // Clear all the unsaved changes when switching tabs
        setHasUnsavedChanges(false);
        
        // Call the parent's onTabChange handler
        onTabChange(tab);
    }, [onTabChange]);
    
    // Use hook for tab change protection
    const { requestTabChange, dialog: tabSwitchDialog } = useTabSwitchWarning({
        hasNonDefaultValues: hasUnsavedChanges,
        activeTab,
        onTabChange: handleTabChange
    });
    
    // Use hook for page change protection
    const navigationDialog = useNavigationWarning(hasUnsavedChanges);
    
    // Context value
    const contextValue: TabsContextType = React.useMemo(() => ({
        setFormChanged: setHasUnsavedChanges,
        requestTabChange,
        hasUnsavedChanges
    }), [setHasUnsavedChanges, requestTabChange, hasUnsavedChanges]);
    
    return (
        <TabsContext.Provider value={contextValue}>
            {children}
            {tabSwitchDialog}
            {navigationDialog}
        </TabsContext.Provider>
    )
}

/**
 * @summary Hook that uses the TabsContext
 * @returns The current TabsContext
 */
export function useTabsContext() {
    const context = React.useContext(TabsContext);
    
    if (context === undefined) 
        throw new Error('useTabsContext must be used within a TabsProvider');
        
    return context;
}