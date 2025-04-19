import { cn } from "@/lib/utils";
import React from "react";

export default function Divider({ name, className }: { name?: string; className?: string }) {
  return (
    <div className={cn("flex w-full flex-row items-center gap-2", className)}>
      {name && <div className="text-xs font-medium text-gray-400 uppercase">{name}</div>}
      <div className="h-px w-full bg-gray-300"></div>
    </div>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


