import type React from "react";
import { cn } from "@/lib/utils";

interface DeleteProps {
  children: React.ReactNode;
  className?: string;
}

export function Delete({ children, className, ...props }: DeleteProps) {
  return (
    <del className={cn("line-through", className)} {...props}>
      {children}
    </del>
  );
}
