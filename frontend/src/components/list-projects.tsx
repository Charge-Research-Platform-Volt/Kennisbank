"use client";

import React, { useEffect, useMemo, useRef } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, GridApi, GridReadyEvent, RowClickedEvent, RowSelectionOptions } from "ag-grid-community";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import { Resource } from "@/types/resource.type";
import { Project } from "@/types/project.type";
import { tableTheme } from "@/lib/tableConfig";
import GetFileIcon from "./getFileIcon";
import { format, parseISO } from "date-fns";
import OpenFileButton from "./open-file-button";
import { useSidebar } from "@/context/sidebar-provider";
import { FolderIcon, SparklesIcon } from "lucide-react";

// Register all modules
ModuleRegistry.registerModules([AllCommunityModule]);

// Define a type to represent the combined structure of projects and resources
interface ProjectOrResource {
  id: string;
  title: string; 
  description: string;
  creationDate?: string;
  itemType?: 'project' | 'folder' | 'resource';
  publicationDate?: string;
  fileType?: string;
}

interface ListProjectsProps {
  resources: Resource[];
  projects: Project[];
  level: number;
  onNavigateToProject?: (projectId: string) => void;
}

/**
 * Component for listing projects and resources in a table
 * @param resources - Array of resources to display
 * @param projects - Array of projects to display
 * @param level - Current navigation level (0 for root level, >0 for subfolder levels)
 * @param onNavigateToProject - Callback function when a project/folder is clicked
 * @returns A table representation of the data
 */
export default function ListProjects({ resources, projects, level, onNavigateToProject }: ListProjectsProps) {
  // Transform the data into a unified format for the table
  const tableData = useMemo(() => {
  const items: ProjectOrResource[] = [];
  
  // Add projects/folders
  projects.forEach((project) => {
    items.push({
      id: project.id,
      title: project.title,
      description: project.description || '',
      creationDate: project.creationDate,
      itemType: project.projectType === 'root' ? 'project' : 'folder',
    });
  });
  
  // Add resources
  resources.forEach(resource => {
    items.push({
      id: resource.id,
      title: resource.title,
      description: resource.description || '',
      itemType: 'resource',
    });
  });
  
  return items;
  }, [projects, resources]);

  // Column definitions
  const columnDefs = useMemo<ColDef[]>(() => [
    { 
      field: "title", 
      cellRenderer: (params: any) => <ItemRenderer {...params} />,
      minWidth: 200,
      flex: 1, 
      resizable: true 
    },
    { field: "description", minWidth: 500, flex: 3, resizable: true },
    { 
      field: "itemType", 
      headerName: "Type", 
      minWidth: 80, 
      resizable: true,
      valueFormatter: (params) => 
        params.value === 'project' ? 'Project'
        : params.value === 'folder' ? 'Folder'
        : params.value === 'resource' ? 
            params.data.fileType : ''
    },
    { 
      field: "creationDate", 
      minWidth: 150, 
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
    
    if (rowData.itemType === "project" && onNavigateToProject) {
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
        <SparklesIcon size={20} className="text-yellow-500" />
      ) : params.data.itemType === 'folder' ? (
        <FolderIcon size={20} className="text-blue-500" />
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