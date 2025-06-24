import type React from "react";
import { cn } from "@/lib/utils";

interface InlineCodeProps {
  children: React.ReactNode;
  className?: string;
}

export function CodeInline({ children, className, ...props }: InlineCodeProps) {
  return (
    <code className={cn("text-primary rounded-sm bg-purple-100 px-1.5 py-0.5 font-mono font-semibold text-purple-800", className)} {...props}>
      {children}
    </code>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


