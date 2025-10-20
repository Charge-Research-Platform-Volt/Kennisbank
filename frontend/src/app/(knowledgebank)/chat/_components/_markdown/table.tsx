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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


