import type React from "react";
import { cn } from "@/lib/utils";
interface ParagraphProps {
  children: React.ReactNode;
  className?: string;
}

export function Paragraph({ children, className, ...props }: ParagraphProps) {
  return (
    <p className={cn("leading-7", className)} {...props}>
      {children}
    </p>
  );
}
