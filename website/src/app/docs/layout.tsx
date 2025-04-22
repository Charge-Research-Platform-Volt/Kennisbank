import React, { type ReactNode } from "react";
import { baseOptions } from "@/app/layout.config";
import { docsSource } from "@/lib/source";
import { DocsLayout } from "fumadocs-ui/layouts/docs";

export default function Layout({ children }: { children: ReactNode }) {
    return (
        <DocsLayout tree={docsSource.pageTree} {...baseOptions}>
            {children}
        </DocsLayout>
    );
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


