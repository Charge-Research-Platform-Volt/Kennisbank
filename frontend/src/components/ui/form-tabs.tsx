'use client'

import React from 'react';
import { Tabs, TabsTrigger } from "@/components/ui/tabs";
import { TabsProvider, useTabsContext } from "@/context/tabs-context";

interface FormTabsProps 
{
    defaultValue: string;
    className: string;
    children: React.ReactNode;
}

/**
 * @summary Custom tab component that uses the TabsProvider that makes it possible to show the Unsaved Dialog on navigation
 * @param defaultValue The default tab to have selected
 * @param className The CSS classes to apply
 * @param children The children within the tabs component
 * @returns 
 */
export function FormTabs({ defaultValue, className, children }: FormTabsProps) 
{
    const [activeTab, setActiveTab] = React.useState(defaultValue);
    
    return (
        <TabsProvider defaultTab={defaultValue} onTabChange={(tab) => setActiveTab(tab)}>
            <Tabs value={activeTab} className={className}>
                {children}
            </Tabs>
        </TabsProvider>
    )
}

interface FormTabsTriggerProps 
{
    value: string;
    children: React.ReactNode;
}

export function FormTabsTrigger({ value, children }: FormTabsTriggerProps) 
{
    const { requestTabChange } = useTabsContext();
    
    return (
        <TabsTrigger value={value} onClick={() => requestTabChange(value) }>
            {children}
        </TabsTrigger>
    )
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


