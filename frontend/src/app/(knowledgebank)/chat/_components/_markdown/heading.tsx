import { cn } from "@/lib/utils";
import React from "react";

interface HeadingProps {
  as: "h1" | "h2" | "h3" | "h4" | "h5" | "h6";
  children: React.ReactNode;
  className?: string;
}

const headingStyles = {
  h1: "scroll-m-20 text-4xl font-extrabold tracking-tight lg:text-5xl mt-8 mb-4",
  h2: "scroll-m-20 text-3xl font-semibold tracking-tight mt-8 mb-4",
  h3: "scroll-m-20 text-2xl font-semibold tracking-tight mt-6 mb-3",
  h4: "scroll-m-20 text-xl font-semibold tracking-tight mt-4 mb-2",
  h5: "scroll-m-20 text-lg font-semibold tracking-tight mt-4 mb-2",
  h6: "scroll-m-20 text-base font-semibold tracking-tight mt-4 mb-2",
};

export function Heading({ as, children, className, ...props }: HeadingProps) {
  const Component = as;
  return (
    <Component className={cn(headingStyles[as], className)} {...props}>
      {children}
    </Component>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


