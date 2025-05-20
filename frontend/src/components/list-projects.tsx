"use client";

import React, { useEffect, useMemo, useRef } from "react";
import { useState } from "react";

// Table imports
import { AgGridReact } from "ag-grid-react";
import type { ColDef, GridApi, GridReadyEvent, RowClickedEvent, RowSelectionOptions } from "ag-grid-community";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import { ResourcePageWithTagsResponse, ResourceResponse } from "@/types/resource.type";
import { ProjectPageResponse, Project } from "@/types/project.type";
import { tableTheme } from "@/lib/tableConfig";
import GetFileIcon from "./getFileIcon";
import { format, parseISO } from "date-fns";
import OpenFileButton from "./open-file-button";
import { useSidebar } from "@/context/sidebar-provider";
import { FolderIcon } from "lucide-react";

// Register all modules
ModuleRegistry.registerModules([AllCommunityModule]);

// Define a type to represent the combined structure of projects and resources
interface ProjectOrResource {
  id: string;
  title: string; 
  description: string;
  itemType: "project" | "resource"; // To differentiate between projects and resources
  creationDate?: string;
  publicationDate?: string;
  fileType?: string;
  // Add other common properties as needed
}

interface ListProjectsProps {
  data: ProjectPageResponse | { resources: ResourceResponse[], projects: ProjectPageResponse[] };
  level: number;
  onNavigateToProject?: (projectId: string) => void;
}

/**
 * Component for listing projects and resources in a table
 * @param data - Data to display in the table, either from project list API or project info API
 * @param level - Current navigation level (0 for root level, >0 for subfolder levels)
 * @param onNavigateToProject - Callback function when a project/folder is clicked
 * @returns A table representation of the data
 */
export default function ListProjects({ data, level, onNavigateToProject }: ListProjectsProps) {
  // Transform the data into a unified format for the table
  const tableData = useMemo(() => {
    const items: ProjectOrResource[] = [];
    
    // Add projects/folders if they exist
    if ('projects' in data && Array.isArray(data.projects)) {
      (data.projects as Project[]).forEach((project) => {
        items.push({
          id: project.id,
          title: project.title,
          description: project.description || '',
          itemType: 'project',
          creationDate: project.creationDate
        });
      });
    }
    
    // Add resources if they exist
    if ('resources' in data && Array.isArray(data.resources)) {
      data.resources.forEach(resource => {
        items.push({
          id: resource.id,
          title: resource.title,
          description: resource.description || '',
          fileType: resource.fileType,
          itemType: 'resource',
          creationDate: resource.creationDate,
          publicationDate: resource.publicationDate
        });
      });
    }
    
    return items;
  }, [data]);

  // Column definitions
  const columnDefs = useMemo<ColDef[]>(() => [
    { 
      field: "title", 
      cellRenderer: (params: any) => <ItemRenderer {...params} />,
      minWidth: 500, 
      flex: 3, 
      resizable: true 
    },
    { field: "description", minWidth: 300, flex: 2, resizable: true },
    { 
      field: "itemType", 
      headerName: "Type", 
      minWidth: 70, 
      flex: 1, 
      resizable: true,
      valueFormatter: (params) => params.value === 'project' ? 'Folder' : (params.data.fileType || '')
    },
    { 
      field: "creationDate", 
      minWidth: 160, 
      valueFormatter: (params) => params.value ? format(parseISO(params.value), "yyyy-MM-dd HH:mm") : '', 
      resizable: true 
    },
    { 
      field: "publicationDate", 
      minWidth: 160, 
      valueFormatter: (params) => params.value ? format(parseISO(params.value), "yyyy-MM-dd HH:mm") : '', 
      resizable: true 
    },
    { 
      field: "", 
      minWidth: 30, 
      maxWidth: 50, 
      cellRenderer: (params: any) => params.data.itemType === 'resource' ? <DownloadRenderer data={params.data} /> : null, 
      resizable: true 
    },
  ], []);

  const gridApiRef = useRef<GridApi | null>(null);

  // Use sidebar context
  const { rightSidebarOpen, toggleRightSidebar } = useSidebar();

  // When grid is ready, set the gridApi
  const onGridReady = (params: GridReadyEvent) => {
    gridApiRef.current = params.api;
  };

  // Row selection
  const rowSelection = useMemo<RowSelectionOptions>(() => {
    return {
      checkboxes: false,
      mode: "singleRow",
    };
  }, []);

  // If on row clicked, handle navigation for projects or toggle sidebar for resources
  const onRowClicked = (e: RowClickedEvent) => {
    // Do nothing if the download button is clicked
    if ((e.event?.target as HTMLElement)?.closest(".download-button")) return;

    const rowData = e.data as ProjectOrResource;
    
    if (rowData.itemType === 'project' && onNavigateToProject) {
      // Navigate to the project/folder
      onNavigateToProject(rowData.id);
    } else if (rowData.itemType === 'resource') {
      // Toggle sidebar for resources
      toggleRightSidebar(e.data);
      e.node.setSelected(true);
    }
  };

  // Unselect all rows when the sidebar is closed
  useEffect(() => {
    if (!rightSidebarOpen) {
      gridApiRef.current?.deselectAll();
    }
  }, [rightSidebarOpen]);

  return (
    <div className="h-[calc(100vh-4rem)] w-full">
      <AgGridReact
        suppressMovableColumns={true}
        suppressCellFocus={true}
        rowData={tableData}
        columnDefs={columnDefs}
        theme={tableTheme}
        onGridReady={onGridReady}
        rowSelection={rowSelection}
        onRowClicked={onRowClicked}
      />
    </div>
  );
}

/**
 * Renders either a folder or file entry depending on the item type
 * @param params - The parameters for rendering the item
 * @returns A React component showing the appropriate icon and name
 */
function ItemRenderer(params: { data: ProjectOrResource; value: string }) {
  return (
    <div className="flex items-center" data-testid="item-entry">
      {params.data.itemType === 'project' ? (
        <FolderIcon size={20} className="text-yellow-500" />
      ) : (
        <GetFileIcon fileType={params.data.fileType || ''} />
      )}
      <span className="ml-2 overflow-hidden text-ellipsis whitespace-nowrap">{params.value}</span>
    </div>
  );
}

/**
 * Renders a download button for resource items
 * @param params - The parameters for rendering the download button
 * @returns A download button component or null
 */
function DownloadRenderer({ data }: { data: any }) {
  return (
    <div className="download-button flex items-center justify-center">
      <OpenFileButton file={data} asIcon={true} />
    </div>
  );
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)