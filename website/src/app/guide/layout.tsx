import { DocsLayout } from "fumadocs-ui/layouts/docs";
import React, { type ReactNode } from "react";
import { baseOptions } from "@/app/layout.config";
import { guideSource } from "@/lib/source";

export default function Layout({ children }: { children: ReactNode }) {
  return (
    <DocsLayout tree={guideSource.pageTree} {...baseOptions}>
      {children}
    </DocsLayout>
  );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


