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
