"use client";

import React from "react";
import { Sidebar } from "../../ui/new-sidebar";
import { useSidebar } from "@/context/sidebar-provider";
import { Button } from "../../ui/button";
import OpenFileButton from "../../open-file-button";
import Divider from "../divider";
import Kbd from "@/components/kbd";

export default function RightSidebar() {
  const { selectedDocument, toggleRightSidebar } = useSidebar();

  return (
    <Sidebar side="right" width="400px">
      {selectedDocument ? (
        <>
          <div className="flex items-center justify-between p-2">
            <OpenFileButton file={selectedDocument} variant="outline" />
            <Button onClick={() => toggleRightSidebar(null)} variant="outline">
              Close
              <Kbd>ESC</Kbd>
            </Button>
          </div>

          <Divider className="mb-6 px-2" />

          <h2 className="mb-6 px-2 text-xl font-semibold">{selectedDocument.name}</h2>

          <div className={"flex min-h-0 flex-1 flex-col gap-2 overflow-auto px-2"}>
            {selectedDocument.description && <p className="mb-6 text-sm font-medium">{selectedDocument.description}</p>}

            {selectedDocument.tags && selectedDocument.tags.length > 0 && (
              <>
                <Divider name="tags" />
                <div className="flex flex-wrap gap-2">
                  {selectedDocument.tags.map((tag) => (
                    <span key={tag.id} className="rounded-md bg-gray-200 px-2 py-1 text-xs font-medium">
                      {tag.name}
                    </span>
                  ))}
                </div>
              </>
            )}
          </div>

          <Divider className="px-2" />

          <div data-slot="sidebar-footer" data-sidebar="footer" className={"flex flex-col gap-1 p-2 text-xs font-medium text-gray-500"}>
            <p>Created At: {selectedDocument.createdAt ? new Date(selectedDocument.createdAt).toLocaleString() : "Not available"}</p>
            <p>Updated At: {selectedDocument.updatedAt ? new Date(selectedDocument.updatedAt).toLocaleString() : "Not available"}</p>
          </div>
        </>
      ) : (
        <NoDocumentSelected />
      )}
    </Sidebar>
  );
}

export function NoDocumentSelected() {
  return (
    <div className="flex h-full flex-col items-center justify-center">
      <h2 className="text-lg font-semibold">No Document Selected</h2>
      <p className="text-gray-500">Please select a document to view details.</p>
    </div>
  );
}
