"use client";

import React, { useEffect, useMemo, useRef } from "react";
import { useState } from "react";

// Table imports
import { AgGridReact } from "ag-grid-react";
import type { ColDef, GridApi, GridReadyEvent, RowClickedEvent, RowSelectionOptions } from "ag-grid-community";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import { ResourcePageWithTagsResponse, ResourceResponse } from "@/types/resource.type";
import { tableTheme } from "@/lib/tableConfig";
import GetFileIcon from "./getFileIcon";
import { format, parseISO } from "date-fns";
import OpenFileButton from "./open-file-button";
import { useSidebar } from "@/context/sidebar-provider";

// Register all modules
ModuleRegistry.registerModules([AllCommunityModule]);

/**
 * 
 * @param data - Data to display in the table, this is a page of the archive, possibly filtered through a search query or other means
 * @returns A table representation of the data
 */
export default function ListResources({ data }: { data: ResourcePageWithTagsResponse }) {
  // Column definitions
  const columnDefs = useState<ColDef[]>([
    { field: "title", flex: 3, cellRenderer: Render, resizable: false, minWidth: 200 },
    { field: "description", flex: 2, resizable: false, minWidth: 200 },
    { field: "fileType", width: 70, headerName: "Type", resizable: false },
    { field: "creationDate", width: 160, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm"), resizable: false },
    { field: "publicationDate", width: 160, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm"), resizable: false },
    { field: "", width: 30, cellRenderer: DownloadRenderer, resizable: false },
  ])[0];

  const gridApiRef = useRef<GridApi | null>(null);

  // deselect the row when the sidebar is closed
  const onCloseSidebar = () => {
    gridApiRef.current?.deselectAll();
  };

  // use sidebar context
  const { rightSidebarOpen, toggleRightSidebar, setOnCloseClicked } = useSidebar();

  //when grid is ready, set the gridApi and onCloseClicked function
  const onGridReady = (params: GridReadyEvent) => {
    gridApiRef.current = params.api;
    setOnCloseClicked(() => onCloseSidebar); 
  };

  // Row selection
  const rowSelection = useMemo(() => {
    return {
      checkboxes: false,
      mode: "singleRow",
    };
  }, []);

  // if on row clicked, deselect all and select the clicked row (if sidebar was closed)
  const onRowClicked = (e: RowClickedEvent) => {
    // do nothing if the download button is clicked
    if((e.event?.target as HTMLElement)?.closest(".download-button")) return;

    toggleRightSidebar(e.data)

    e.node.setSelected(true);
  };

  // unselect all rows when the sidebar is closed
  useEffect(() => {
    if (!rightSidebarOpen) {
      gridApiRef.current?.deselectAll();
    }
  }
  , [rightSidebarOpen]);

  return (
    <div className="h-[calc(100vh-6rem)] w-full">
      <AgGridReact suppressMovableColumns={true} suppressCellFocus={true} rowData={data.resources} columnDefs={columnDefs} theme={tableTheme} onGridReady={onGridReady} rowSelection={rowSelection as RowSelectionOptions} onRowClicked={onRowClicked} />
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
export function Render(params: { data: { fileType: string }; value: string }) {
  return (
    <div className="flex items-center" data-testid="file-entry">
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
    <div className="flex items-center justify-center download-button">
      <OpenFileButton file={params.data} asIcon={true} />
    </div>
  );
}



// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


