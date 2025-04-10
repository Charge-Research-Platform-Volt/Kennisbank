"use client";

import React, { useMemo } from "react";
import { useState } from "react";

// Table imports
import { AgGridReact } from "ag-grid-react";
import type { ColDef, RowSelectionOptions } from "ag-grid-community";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import { ResourcePageResponse, ResourceResponse } from "@/types/resource.type";
import { tableTheme } from "@/lib/tableConfig";
import GetFileIcon from "./getFileIcon";
import { format, parseISO } from "date-fns";
import OpenFileButton from "./open-file-button";

// Register all modules
ModuleRegistry.registerModules([AllCommunityModule]);

export default function ListResources({ data }: { data: ResourcePageResponse }) {
  // Column definitions
  const columnDefs = useState<ColDef[]>([
    { field: "title", flex: 3, filter: true, cellRenderer: Render },
    { field: "description", flex: 2 },
    { field: "fileType", width: 70, headerName: "Type" },
    { field: "creationDate", width: 160, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm") },
    { field: "publicationDate", width: 160, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm") },
    { field: "", width: 30, cellRenderer: DownloadRenderer },
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
    <div className="h-[calc(100vh-1.25rem)] w-full">
      <AgGridReact rowData={data.resources} columnDefs={columnDefs} theme={tableTheme} rowSelection={rowSelection as RowSelectionOptions} />
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

export function DownloadRenderer(params: { data: ResourceResponse }) {
  return (
    <div className="flex items-center justify-center">
      <OpenFileButton file={params.data} asIcon={true} />
    </div>
  );
}

