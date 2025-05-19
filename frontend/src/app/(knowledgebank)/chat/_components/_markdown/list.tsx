import type React from "react";
import { cn } from "@/lib/utils";

interface ListProps {
  ordered: boolean;
  children: React.ReactNode;
  className?: string;
}

export function List({ ordered, children, className, ...props }: ListProps) {
  const Component = ordered ? "ol" : "ul";

  return (
    <Component className={cn("my-2 ml-6 marker:text-purple-800", ordered ? "list-decimal" : "list-disc", className)} {...props}>
      {children}
    </Component>
  );
}
