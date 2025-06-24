import type React from "react";
import { cn } from "@/lib/utils";

interface BlockQuoteProps {
  children: React.ReactNode;
  className?: string;
}

export function BlockQuote({ children, className, ...props }: BlockQuoteProps) {
  return (
    <blockquote className={cn("border-primary text-muted-foreground mt-6 border-l-2 pl-6 italic", className)} {...props}>
      {children}
    </blockquote>
  );
}
