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
