import { DocsLayout } from "fumadocs-ui/layouts/docs";
import React, { type ReactNode } from "react";
import { baseOptions } from "@/app/layout.config";
import { guideSource } from "@/lib/source";
import { SearchWrapper } from "@/components/search-wrapper";

export default function Layout({ children }: { children: ReactNode }) {
  return (
    <>
      <DocsLayout tree={guideSource.pageTree} {...baseOptions} sidebar={{
          banner: (
              <div className="py-3 px-1 border-b border-border/40">
                  <SearchWrapper />
              </div>
          ),
      }}>
          {children}
      </DocsLayout>
    </>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)