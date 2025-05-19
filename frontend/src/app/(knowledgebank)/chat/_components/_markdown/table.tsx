import type React from "react";
import { cn } from "@/lib/utils";

interface TableProps {
  children: React.ReactNode;
  className?: string;
}

export function Table({ children, className, ...props }: TableProps) {
  return (
    <div className="my-6 w-full overflow-y-auto">
      <table className={cn("w-full border-collapse text-sm", className)} {...props}>
        {children}
      </table>
    </div>
  );
}
