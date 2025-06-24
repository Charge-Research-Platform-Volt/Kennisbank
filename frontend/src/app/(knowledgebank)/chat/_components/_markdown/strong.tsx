import type React from "react";
import { cn } from "@/lib/utils";

interface StrongProps {
  children: React.ReactNode;
  className?: string;
}

export function Strong({ children, className, ...props }: StrongProps) {
  return (
    <strong className={cn("font-semibold", className)} {...props}>
      {children}
    </strong>
  );
}
