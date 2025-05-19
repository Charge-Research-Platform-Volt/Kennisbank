import type React from "react";
import { cn } from "@/lib/utils";
import { ExternalLink } from "lucide-react";

interface LinkProps {
  href: string;
  children: React.ReactNode;
  className?: string;
}

export function Link({ href, children, className, ...props }: LinkProps) {
  const isExternal = href.startsWith("http");

  return (
    <a
      href={href}
      className={cn("text-primary hover:text-primary/80 font-medium underline underline-offset-4 transition-colors", className)}
      {...(isExternal ? { target: "_blank", rel: "noopener noreferrer" } : {})}
      {...props}
    >
      {children}
      {isExternal && <ExternalLink className="mb-1 ml-1 inline h-4 w-4" />}
    </a>
  );
}
