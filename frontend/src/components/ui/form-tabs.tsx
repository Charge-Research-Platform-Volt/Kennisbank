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