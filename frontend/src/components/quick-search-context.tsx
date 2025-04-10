"use client";

import React, { createContext, useContext, useState } from "react";

type QuickSearchContextType = {
    isOpen: boolean;
    setIsOpen: (open: boolean) => void;
};

const QuickSearchContext = createContext<QuickSearchContextType | undefined>(undefined);

export const QuickSearchProvider = ({ children } : { children: React.ReactNode }) => {
    const [isOpen, setIsOpen] = useState(false);

    return (
        <QuickSearchContext.Provider value={{ isOpen, setIsOpen }}>
            {children}
        </QuickSearchContext.Provider>
    );
}

export const useQuickSearch = (): QuickSearchContextType => {
    const context = useContext(QuickSearchContext);
    if (!context) {
        throw new Error("useQuickSearch must be used within a QuickSearchProvider");
    }
    return context;
}