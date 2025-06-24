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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


