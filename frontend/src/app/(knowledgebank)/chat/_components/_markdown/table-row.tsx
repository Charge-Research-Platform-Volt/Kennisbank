import type React from "react";
import { cn } from "@/lib/utils";

interface TableRowProps {
  children: React.ReactNode;
  className?: string;
}

export function TableRow({ children, className, ...props }: TableRowProps) {
  return (
    <tr className={cn("even:bg-sidebar m-0 border border-t p-0", className)} {...props}>
      {children}
    </tr>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


