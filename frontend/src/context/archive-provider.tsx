'use client'

import React from 'react';

export type ArchiveContextType = {

    // search settings
    tagFilters: string[];
    setTagFilters: (filters: string[]) => void;
};

const ArchiveContext = React.createContext<ArchiveContextType | undefined>(undefined);

export const useArchive = (): ArchiveContextType =>
{
    const context = React.useContext(ArchiveContext);

    if (!context) throw new Error("useArchive must be used within a ArchiveProvider");

    return context;
}

export const ArchiveProvider = ({children}: {children: React.ReactNode}) =>
{
    const [tagFilters, setTagFilters] = React.useState<string[]>([]);


    return (
        <ArchiveContext.Provider 
            value={{
                tagFilters,
                setTagFilters,
            }}>
        {children}
        </ArchiveContext.Provider>
    );
}


