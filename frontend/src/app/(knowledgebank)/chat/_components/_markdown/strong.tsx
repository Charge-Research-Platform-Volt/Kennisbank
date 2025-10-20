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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


