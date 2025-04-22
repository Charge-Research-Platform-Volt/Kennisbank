"use client";

import React, { useLayoutEffect, useState } from "react";
import Search from "@/icons/search-icon";
import { Button } from "@/components/ui/button";
import Kbd from "@/components/kbd";
import FoldedButton from "./folded-button";
import { useQuickSearch } from "@/components/quick-search-context";

/**
 * 
 * @returns QuickSearch bar in the top left corner of the screen. Users can then quickly search through the archive and open files / visit websites.
 */
export default function QuickSearchButton( {isIcon = false }: {isIcon?: boolean}) {
  const { isOpen, setIsOpen } = useQuickSearch();

  const [shortcut, setShortcut] = useState("Ctrl + K");

  useLayoutEffect(() => {
    const isMac = navigator.userAgent.includes("Mac");
    setShortcut(isMac ? "Cmd + K" : "Ctrl + K");
  }, []);

  return (
    <>
        { !isIcon && (
            <Button onClick={() => setIsOpen(!isOpen)} variant="outline" className="m-0 flex w-full items-center justify-between p-2">
                <div className="flex items-center gap-2">
                    <Search className="h-4 w-4" />
                    Search
                </div>
                <Kbd>{shortcut}</Kbd>
            </Button>
        )}
        { isIcon && (
            <FoldedButton action={() => setIsOpen(!isOpen)} icon={<Search className="h-4 w-4" />} className="mb-[2vh] bg-white pt-2 pb-2 shadow-xs" />
        )}
    </>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


