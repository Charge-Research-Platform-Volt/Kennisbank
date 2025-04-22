"use client";

import React, { useLayoutEffect, useState } from "react";
import Search from "@/icons/search-icon";
import { Button } from "@/components/ui/button";
import Kbd from "@/components/kbd";
import FoldedButton from "./folded-button";
import { useQuickSearch } from "@/components/quick-search-context";

/**
 * 
 * @param asIcon - If true, the button is displayed as an icon. If false, the button is displayed as a text button.
 * @returns The a button for opening the quick search bar. The button is displayed as an icon if asIcon is true, and as a text button if asIcon is false.
 */
export default function QuickSearchButton( {asIcon: asIcon = false }: {asIcon?: boolean}) {
  const { isOpen, setIsOpen } = useQuickSearch();

  const [shortcut, setShortcut] = useState("Ctrl + K");

  useLayoutEffect(() => {
    const isMac = navigator.userAgent.includes("Mac");
    setShortcut(isMac ? "Cmd + K" : "Ctrl + K");
  }, []);

  return (
    <>
        { !asIcon && (
            <Button onClick={() => setIsOpen(!isOpen)} variant="outline" className="m-0 flex w-full items-center justify-between p-2">
                <div className="flex items-center gap-2">
                    <Search className="h-4 w-4" />
                    Search
                </div>
                <Kbd>{shortcut}</Kbd>
            </Button>
        )}
        { asIcon && (
            <FoldedButton action={() => setIsOpen(!isOpen)} icon={<Search className="h-4 w-4" />} className="mb-[2vh] bg-white pt-2 pb-2 shadow-xs" />
        )}
    </>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


