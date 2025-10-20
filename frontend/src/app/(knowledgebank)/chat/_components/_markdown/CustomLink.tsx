import type React from "react";
import { cn } from "@/lib/utils";
import { ExternalLink, FileText } from "lucide-react";
import Link from "next/link";

interface LinkProps {
  href: string;
  children: React.ReactNode;
  className?: string;
}

// Truncate text to a maximum length with ellipsis
function truncateText(text: string, maxLength: number): string {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength) + "...";
}

export function CustomLink({ href, children, className, ...props }: LinkProps) {
  const isExternal = href.startsWith("http");
  const isArchiveLink = href.startsWith("/archive");

  // Archive links (citations) - show as compact badges
  if (isArchiveLink) {
    const childText = typeof children === "string" ? children : String(children);
    const truncatedText = truncateText(childText, 40);

    return (
      <a
        href={href}
        className={cn(
          "inline-flex items-center gap-1 px-2 py-0.5 mx-0.5",
          "bg-primary/10 hover:bg-primary/20",
          "text-primary text-sm font-medium",
          "rounded-md border border-primary/20",
          "transition-colors no-underline",
          className
        )}
        target="_blank"
        rel="noopener noreferrer"
        title={childText} // Full title on hover
        {...props}
      >
        <FileText className="h-3 w-3 flex-shrink-0" />
        <span className="truncate">{truncatedText}</span>
      </a>
    );
  }

  // External links - show with icon
  if (isExternal) {
    return (
      <a
        href={href}
        className={cn("text-primary hover:text-primary/80 font-medium underline underline-offset-4 transition-colors", className)}
        target="_blank"
        rel="noopener noreferrer"
        {...props}
      >
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


