import type React from "react";
import { cn } from "@/lib/utils";

interface TableCellProps {
  isHeader: boolean;
  children: React.ReactNode;
  className?: string;
}

export function TableCell({ isHeader, children, className, ...props }: TableCellProps) {
  const Component = isHeader ? "th" : "td";

  return (
    <Component className={cn("border px-4 py-2 text-left", isHeader ? "bg-sidebar font-semibold" : "", className)} {...props}>
      {children}
    </Component>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


