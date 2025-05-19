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
