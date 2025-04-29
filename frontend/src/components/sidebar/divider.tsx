import { cn } from "@/lib/utils";
import React from "react";

/**
 *
 * @param name - Divider name
 * @param className - Divider styling from root
 * @returns Divider of parts of the sidebar, displays the name of a specific part + a line
 */
export default function Divider({ name, className, minimize }: { name?: string; className?: string; minimize?: boolean }) {
  return (
    <div className={cn(`flex w-full flex-row items-center ${minimize ? "gap-2" : "gap-0"}`, className)}>
      {name && <div className={`text-xs font-medium text-gray-400 uppercase ${!minimize ? "max-w-0 opacity-0" : "max-w-full opacity-100"} transition-all duration-300`}>{name}</div>}
      <div className="h-px w-full bg-gray-300"></div>
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
