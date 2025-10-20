import type React from "react";
import { cn } from "@/lib/utils";
import { ExternalLink } from "lucide-react";
import Link from "next/link";

interface LinkProps {
  href: string;
  children: React.ReactNode;
  className?: string;
}

export function CustomLink({ href, children, className, ...props }: LinkProps) {
  const isExternal = href.startsWith("http");
  console.log("CustomLink", { href, isExternal });

  if (isExternal) {
    return (
      <a href={href} className={cn("text-primary hover:text-primary/80 font-medium underline underline-offset-4 transition-colors", className)} target="_blank" rel="noopener noreferrer" {...props}>
        {children}
        <ExternalLink className="mb-1 ml-1 inline h-4 w-4" />
      </a>
    );
  }

  return (
    <Link href={href} className={cn("text-primary hover:text-primary/80 font-medium underline underline-offset-4 transition-colors", className)} {...props}>
      {children}
    </Link>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


