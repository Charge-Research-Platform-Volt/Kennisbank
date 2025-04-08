"use client";

import React, { useMemo } from "react";
import { useState } from "react";

// Table imports
import { AgGridReact } from "ag-grid-react";
import type { ColDef, RowSelectionOptions } from "ag-grid-community";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import { DocumentPageResponse, DocumentResponse } from "@/types/document.type";
import { tableTheme } from "@/lib/tableConfig";
import GetFileIcon from "./getFileIcon";
import { format, parseISO } from "date-fns";
import OpenFileButton from "./open-file-button";

// Register all modules
ModuleRegistry.registerModules([AllCommunityModule]);

export default function ListDocuments({ data }: { data: DocumentPageResponse }) {
  // Column definitions
  const columnDefs = useState<ColDef[]>([
    { field: "name", flex: 15, filter: true, cellRenderer: Render },
    { field: "description", flex: 15 },
    { field: "fileType", flex: 3, headerName: "Type" },
    { field: "createdAt", flex: 6, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm") },
    { field: "updatedAt", flex: 6, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm") },
    { field: "", flex: 1, cellRenderer: DownloadRenderer },
  ])[0];

  // const [defaultColDef, setDefaultColDef] = useState({
  //   resizable: true,
  //   sortable: true,
  //   filter: false,
  // });

  // Row selection
  const rowSelection = useMemo(() => {
    return {
      mode: "multiRow",
    };
  }, []);

  return (
    <div className="h-[calc(100vh-5.75rem)] w-full">
      <AgGridReact rowData={data.files} columnDefs={columnDefs} theme={tableTheme} rowSelection={rowSelection as RowSelectionOptions} />
    </div>
  );
}

/**
 * Renders a file entry for a list of documents.
 *
 * This component displays a file icon based on the file type and the file name.
 *
 * @param params - The parameters for rendering the file entry
 * @param params.data - Data object containing file information
 * @param params.data.fileType - The type of the file (used to determine the appropriate icon)
 * @param params.value - The display value (typically the file name)
 * @returns A React component showing the file icon and name
 */
export function Render(params: { data: { fileType: string }; value: string;  }) {
  return (
    <div className="flex items-center">
      <GetFileIcon fileType={params.data.fileType} />
      <span className="ml-2 overflow-hidden text-ellipsis whitespace-nowrap">{params.value}</span>
    </div>
  );
}

/**
 * 
 * @param params - The parameters for rendering the download button
 * @param params.data - Data object containing file information
 * @param params.data.id - The ID of the file (used to construct the download URL)
 * @param params.data.fileType - The type of the file (used to determine the appropriate action)
 * @param params.data.name - The name of the file (used for display purposes)
 * @param params.data.description - The description of the file (not used in this function)
 * @param params.data.hash - The hash of the file (not used in this function)
 * @param params.data.createdAt - The creation date of the file (not used in this function)
 * * @param params.data.updatedAt - The last update date of the file (not used in this function)
 * @returns 
 */

export function DownloadRenderer(params: { data: DocumentResponse }) {
  return (
    <div className="flex items-center justify-center">
      <OpenFileButton file={params.data} asIcon={true} />
    </div>
  );
}
