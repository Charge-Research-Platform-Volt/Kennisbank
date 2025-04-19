"use client";

import React, { createContext, useContext, useState } from "react";

// Define the shape of the QuickSearch context
type QuickSearchContextType = {
    isOpen: boolean;
    setIsOpen: (open: boolean) => void;
};

// Create the QuickSearch context with an initial undefined value
const QuickSearchContext = createContext<QuickSearchContextType | undefined>(undefined);

// Provide the QuickSearch context to child components
export const QuickSearchProvider = ({ children } : { children: React.ReactNode }) => {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <QuickSearchContext.Provider value={{ isOpen, setIsOpen }}>
            {children}
        </QuickSearchContext.Provider>
    );
}

// Custom hook to use the QuickSearch context
export const useQuickSearch = (): QuickSearchContextType => {
    const context = useContext(QuickSearchContext);
    if (!context) {
        throw new Error("useQuickSearch must be used within a QuickSearchProvider");
    }
    return context;
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


