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
