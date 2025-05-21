"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
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
import { ArrowLeftIcon, FolderIcon, HomeIcon, SparklesIcon } from "lucide-react";
import { Button } from "@/components/ui/button";

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
  projectType?: string;
}

interface BreadcrumbItem {
  id: string;
  title: string;
}

interface ListProjectsProps {
  initialResources: Resource[];
  initialProjects: Project[];
  // Function to fetch data for a specific project ID
  fetchProjectContent: (projectId: string) => Promise<{ resources: Resource[], projects: Project[] }>;
}

/**
 * Component for listing projects and resources in a table with navigation capabilities
 * @param initialResources - Initial array of resources to display
 * @param initialProjects - Initial array of projects to display
 * @param fetchProjectContent - Function to fetch content of a project when navigating into it
 * @returns A navigable table representation of projects and resources
 */
export default function ListProjects({initialResources, initialProjects, fetchProjectContent}: ListProjectsProps) {
  // State for current projects and resources being displayed
  const [resources, setResources] = useState<Resource[]>(initialResources);
  const [projects, setProjects] = useState<Project[]>(initialProjects);
  const [currentLevel, setCurrentLevel] = useState<number>(0);
  const [isLoading, setIsLoading] = useState<boolean>(false);
  
  // Navigation history/breadcrumbs
  const [navigationPath, setNavigationPath] = useState<BreadcrumbItem[]>([]);
  const [currentProjectId, setCurrentProjectId] = useState<string | null>(null);

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
        projectType: project.projectType,
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


  // #region Page Navigation

  /**
   * Navigate to a specific project and load its contents
   * @param projectId - ID of the project to navigate to
   * @param projectTitle - Title of the project for breadcrumb
   */
  const navigateToProject = async (projectId: string, projectTitle: string) => {
    try {
      setIsLoading(true);

      const { resources: newResources, projects: newProjects } = await fetchProjectContent(projectId);

      // Clear focus before changing data, otherwise throws error
      if (gridApiRef.current) {
        gridApiRef.current.clearFocusedCell(); 
      }
      setNavigationPath(prev => [...prev, { id: projectId, title: projectTitle }]);
      setCurrentProjectId(projectId);
      setResources(newResources);
      setProjects(newProjects);
      setCurrentLevel(prev => prev + 1);
      setIsLoading(false); 

    } catch (error) {
      console.error("Error navigating to project:", error);
      setIsLoading(false); 
    }
  };


  /**
   * Navigate back to the parent project
   */
  const navigateBack = async () => {
    // Can't go back if at root level
    if (navigationPath.length <= 1) {
      return navigateToRoot();
    }
    
    try {
      setIsLoading(true);
      
      // Get the parent project ID
      const newPath = [...navigationPath];
      newPath.pop(); // Remove current
      const parentItem = newPath[newPath.length - 1];
      
      // Fetch the parent project content
      const { resources: newResources, projects: newProjects } = await fetchProjectContent(parentItem.id);
      
      // Update state
      setNavigationPath(newPath);
      setCurrentProjectId(parentItem.id);
      setResources(newResources);
      setProjects(newProjects);
      setCurrentLevel(prev => prev - 1);

    } catch (error) {
      console.error("Error navigating back:", error);
      // Handle error
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Navigate to a specific level in the breadcrumb path
   * @param index - Index in the navigation path to navigate to
   */
  const navigateToBreadcrumb = async (index: number) => {
    try {
      setIsLoading(true);
      
      // Get the target project
      const targetPath = navigationPath.slice(0, index + 1);
      const targetItem = targetPath[targetPath.length - 1];
      
      // Fetch the project content
      const { resources: newResources, projects: newProjects } = await fetchProjectContent(targetItem.id);
      
      // Update state
      setNavigationPath(targetPath);
      setCurrentProjectId(targetItem.id);
      setResources(newResources);
      setProjects(newProjects);
      setCurrentLevel(index + 1);

    } catch (error) {
      console.error("Error navigating to breadcrumb:", error);
      // Handle error
    } finally {
      setIsLoading(false);
    }
  };

  /**
   * Navigate back to the root level
   */
  const navigateToRoot = () => {
    setResources(initialResources);
    setProjects(initialProjects);
    setNavigationPath([]);
    setCurrentProjectId(null);
    setCurrentLevel(0);
  };

  // If on row clicked, handle navigation for projects or toggle sidebar for resources
  const onRowClicked = (e: RowClickedEvent) => {
    // Do nothing if the download button is clicked
    if ((e.event?.target as HTMLElement)?.closest(".download-button")) return;

    const rowData = e.data as ProjectOrResource;
    
    if ((rowData.itemType === 'project' || rowData.itemType === 'folder')) {
      // Navigate to the project/folder
      navigateToProject(rowData.id, rowData.title);
    } else if (rowData.itemType === 'resource') {
      // Toggle sidebar for resources
      toggleRightSidebar(e.data);
      e.node.setSelected(true);
    }
  };

  // #region Page Rendering

  // Unselect all rows when the sidebar is closed
  useEffect(() => {
    if (!rightSidebarOpen) {
      gridApiRef.current?.deselectAll();
    }
  }, [rightSidebarOpen]);

  return (
    <div className="flex flex-col h-full w-full">
      {/* Navigation Controls */}
      <div className="mb-4 flex items-center space-x-1">
        <Button 
          variant="outline" 
          size="sm" 
          onClick={navigateToRoot}
          disabled={currentLevel === 0 || isLoading}
        >
          <HomeIcon size={16} className="mr-1" />
          Home
        </Button>
        
        {currentLevel > 0 && (
          <Button 
            variant="outline" 
            size="sm" 
            onClick={navigateBack}
            disabled={isLoading}
          >
            <ArrowLeftIcon size={16} className="mr-1" />
            Back
          </Button>
        )}
        
        {/* Breadcrumbs, used to navigate back */}
        {navigationPath.length > 0 && (
          <div className="flex items-center overflow-x-auto px-2">
            {navigationPath.map((item, index) => (
              <React.Fragment key={item.id}>
                {index > 0 && <span className="mx-1 text-gray-500">/</span>}
                <button
                  onClick={() => navigateToBreadcrumb(index)}
                  disabled={isLoading || index === navigationPath.length - 1}
                  className={`text-sm hover:underline ${
                    index === navigationPath.length - 1 
                      ? 'font-semibold text-blue-600 cursor-default'
                      : 'text-blue-500'
                  }`}
                >
                  {item.title}
                </button>
              </React.Fragment>
            ))}
          </div>
        )}
        
        {/* Loading indicator */}
        {isLoading && (
          <div className="ml-2 text-sm text-gray-500">Loading...</div>
        )}
      </div>
      
      {/* Table */}
      <div className="h-[calc(100vh-6rem)] w-full">
        <AgGridReact
          suppressMovableColumns={true}
          suppressCellFocus={true}
          rowData={tableData}
          columnDefs={columnDefs}
          theme={tableTheme}
          onGridReady={onGridReady}
          rowSelection={rowSelection}
          onRowClicked={onRowClicked}
          overlayLoadingTemplate="<span class='ag-overlay-loading-center'>Loading content...</span>"
          overlayNoRowsTemplate="<span class='ag-overlay-no-rows-center'>No items found in this location</span>"
          loading={isLoading}
        />
      </div>
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