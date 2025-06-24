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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


