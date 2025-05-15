"use client";

import React, { useEffect, useMemo, useRef } from "react";
import { useState } from "react";

// Table imports
import { AgGridReact } from "ag-grid-react";
import type { CellContextMenuEvent, ColDef, GridApi, GridReadyEvent, RowClickedEvent, RowSelectionOptions } from "ag-grid-community";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import { ResourcePageWithTagsResponse, ResourceResponse } from "@/types/resource.type";
import { tableTheme } from "@/lib/tableConfig";
import GetFileIcon from "./getFileIcon";
import { format, parseISO } from "date-fns";
import OpenFileButton from "./open-file-button";
import { useSidebar } from "@/context/sidebar-provider";
import { handleOpenFile } from "@/actions/openFileActions";
import { ArchiveResource, UnarchiveResource } from "@/actions/archiveResourceActions";
import { toast } from "sonner";
import { useUserRole } from "@/context/user-role-context";
import { ArchiveRestore } from "lucide-react";
import { Button } from "./ui/button";
import { DeleteResource } from "@/actions/deleteActions";

// Register all modules
ModuleRegistry.registerModules([AllCommunityModule]);

/**
 *
 * @param data - Data to display in the table, this is a page of the archive, possibly filtered through a search query or other means
 * @returns A table representation of the data
 */
export default function ListResources({ data, initialLoadingComplete }: { data: ResourcePageWithTagsResponse, initialLoadingComplete: boolean }) {
  const { userRole } = useUserRole();

  // Column definitions
  const columnDefs = useState<ColDef[]>([
    { field: "title", cellRenderer: Render, minWidth: 500, flex: 3, resizable: true },
    { field: "description", minWidth: 300, flex: 2, resizable: true },
    { field: "fileType", minWidth: 70, flex: 1, headerName: "Type", resizable: true },
    { field: "creationDate", minWidth: 160, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm"), resizable: true },
    { field: "publicationDate", minWidth: 160, valueFormatter: (params) => format(parseISO(params.value), "yyyy-MM-dd HH:mm"), resizable: true },
    { field: "", minWidth: 30, maxWidth: 50, cellRenderer: DownloadRenderer, resizable: true },
  ])[0];

  const gridApiRef = useRef<GridApi | null>(null);

  // use sidebar context
  const { rightSidebarOpen, toggleRightSidebar } = useSidebar();

  //when grid is ready, set the gridApi and onCloseClicked function
  const onGridReady = (params: GridReadyEvent) => {
    gridApiRef.current = params.api;
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
    if ((e.event?.target as HTMLElement)?.closest(".download-button")) return;

    toggleRightSidebar(e.data);
    e.node.setSelected(true);
  };

  // unselect all rows when the sidebar is closed
  useEffect(() => {
    if (!rightSidebarOpen) {
      gridApiRef.current?.deselectAll();
    }
  }, [rightSidebarOpen]);

  // Context menu on right click
  const [contextMenuPosition, setContextMenuPosition] = useState<{ mouseX: number; mouseY: number } | null>(null);
  const [selectedRowData, setSelectedRowData] = useState<ResourceResponse | null>(null);

  const onCellContextMenu = (event: CellContextMenuEvent) => {
    event.event?.preventDefault();

    const mouseEvent = event.event as MouseEvent;

    setSelectedRowData(event.data);
    setContextMenuPosition({
      mouseX: mouseEvent.clientX,
      mouseY: mouseEvent.clientY,
    });
  };

  const contextMenuRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
  const handleClickOutside = (event: MouseEvent) => {
    if (
        contextMenuRef.current &&
        !contextMenuRef.current.contains(event.target as Node)
      ) {
        setContextMenuPosition(null);
      }
    };

    if (contextMenuPosition) {
      document.addEventListener("mousedown", handleClickOutside);
    }

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, [contextMenuPosition]);

  const HandleArchive = async () => {
    const result = await ArchiveResource(selectedRowData ? selectedRowData.id : "");

    if(result.success){
      toast.success(result.message);
      window.dispatchEvent(new Event("resourceListUpdated"));
    }
    else{
      toast.error(result.message);
    }
  }

  const HandleRestore = async () => {
    const result = await UnarchiveResource(selectedRowData ? selectedRowData.id : "");

    if(result.success){
      toast.success(result.message);
      window.dispatchEvent(new Event("resourceListUpdated"));
    }
    else{
      toast.error(result.message);
    }
  }

  const HandlePermanentDelte = async () => {
    const result = await DeleteResource(selectedRowData ? selectedRowData.id : "");

    if(result.success){
      toast.success(result.message);
      window.dispatchEvent(new Event("resourceListUpdated"));
    }
    else{
      toast.error(result.message);
    }
  }

  return (
    <>
      {contextMenuPosition && (
        <div
          ref={contextMenuRef}
          className="absolute z-50 bg-white border shadow-md rounded-md"
          style={{ top: contextMenuPosition.mouseY, left: contextMenuPosition.mouseX }}
          onClick={() => setContextMenuPosition(null)}
        >
          <ul>
            {!selectedRowData?.archived && (<li onClick={() => selectedRowData ? handleOpenFile(selectedRowData) : () => {}} className="hover:bg-gray-100 cursor-pointer px-4">{selectedRowData?.fileType == "pdf" || selectedRowData?.fileType == "website" ? "Open" : "Dowload"}</li>)}
            {userRole == "admin" && !selectedRowData?.archived ? (<li onClick={HandleArchive} className="hover:bg-gray-100 cursor-pointer px-4">Move to trash</li>) : null}
            {userRole == "admin" && selectedRowData?.archived ? (<li onClick={HandleRestore} className="hover:bg-gray-100 cursor-pointer px-4">Restore file</li>) : null}
            {userRole == "admin" && selectedRowData?.archived ? (<li onClick={HandlePermanentDelte} className="hover:bg-gray-100 cursor-pointer px-4">Delete permanently</li>) : null}
          </ul>
        </div>
      )}
      <div className="h-[calc(100vh-4rem)] w-full" >
        <AgGridReact
          suppressMovableColumns={true}
          suppressCellFocus={true}
          rowData={data.resources}
          columnDefs={columnDefs}
          theme={tableTheme}
          onGridReady={onGridReady}
          rowSelection={rowSelection as RowSelectionOptions}
          onRowClicked={onRowClicked}
          suppressContextMenu={true}
          preventDefaultOnContextMenu={true}
          onCellContextMenu={onCellContextMenu}
          localeText={{
            noRowsToShow: initialLoadingComplete ? "No results found" : "Loading...",
          }}
        />
      </div>
    </>
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

export function DownloadRenderer(params: { data: ResourceResponse }) {
  const handleUnarchiveResource = async (id: string) => {
    const result = await UnarchiveResource(id);

    if(result.success){
      toast.success(result.message);
      window.dispatchEvent(new Event("resourceListUpdated"));
    }
    else{
      toast.error(result.message);
    }
  }
  return (
    <div className="download-button flex items-center justify-center">
      {params.data.archived ? (
        <Button
          className="bg-transparent hover:bg-gray-200 shadow-none text-muted-foreground"
          variant="default"
          type="button"
          title="Restore"
          onClick={() => handleUnarchiveResource(params.data.id)}
        >
          <ArchiveRestore />
        </Button>
      ) : (<OpenFileButton file={params.data} asIcon={true} />)}
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
