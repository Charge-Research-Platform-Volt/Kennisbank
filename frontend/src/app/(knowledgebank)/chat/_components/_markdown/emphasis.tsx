import type React from "react";
import { cn } from "@/lib/utils";

interface EmphasisProps {
  children: React.ReactNode;
  className?: string;
}

export function Emphasis({ children, className, ...props }: EmphasisProps) {
  return (
    <em className={cn("italic", className)} {...props}>
      {children}
    </em>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


