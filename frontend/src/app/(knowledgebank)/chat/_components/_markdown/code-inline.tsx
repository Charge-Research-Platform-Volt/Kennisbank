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
