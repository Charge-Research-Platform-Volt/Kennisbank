"use client";

import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
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
import { ArrowLeftIcon, FolderIcon, HomeIcon, SparklesIcon, CirclePlusIcon, CircleXIcon, EditIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { addResourceToProject, fetchAllResources, ListProjectsPaged, removeResourceFromProject, deleteProject } from "@/actions/projectActions";
import { ApiResponse } from "@/types/apiResponse.type";
import CreateProjectModal from "@/app/(knowledgebank)/projects/components/create-project-modal";
import { DropdownMenu, DropdownMenuContent, DropdownMenuItem, DropdownMenuTrigger } from "@radix-ui/react-dropdown-menu";
import { ProjectActionsDropdown } from "../app/(knowledgebank)/projects/components/projects-dropdown";
import CreateFolderModal from "@/app/(knowledgebank)/projects/components/create-folder-modal";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import EditProjectModal from "@/app/(knowledgebank)/projects/components/edit-project-modal";

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

interface NavigationState {
  currentProjectId: string | null;
  currentLevel: number;
  navigationPath: BreadcrumbItem[];
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
  // Query for searching
  const [currentQuery, setCurrentQuery] = useState<string>("");
  // State for current projects and resources being displayed
  const [resources, setResources] = useState<Resource[]>(initialResources);
  const [projects, setProjects] = useState<Project[]>(initialProjects);
  const [isLoading, setIsLoading] = useState<boolean>(false);

  // State for navigation - current project ID, level, and breadcrumb path
  const [navigationState, setNavigationState] = useState<NavigationState>({
    currentProjectId: null,
    currentLevel: 0,
    navigationPath: []
  });
  // Ref to keep track of the current navigation state
  const navigationRef = useRef<NavigationState>(navigationState);

  // State for modal visibility
  const [isCreateProjectModalOpen, setIsCreateProjectModalOpen] = useState(false);
  const [isCreateFolderModalOpen, setIsCreateFolderModalOpen] = useState(false);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [selectedProjectForEdit, setSelectedProjectForEdit] = useState<Project | null>(null);

  // State for resource add mode
  const [isAddResourceMode, setIsAddResourceMode] = useState(false);
  const [allResources, setAllResources] = useState<Resource[]>([]);
  const [selectedResourceIds, setSelectedResourceIds] = useState<Set<string>>(new Set());


  // Handles filtering the projects and resources based on the current query
  const filteredItems = useMemo(() => {
    const lowerCaseQuery = currentQuery.toLowerCase();
    
    if (isAddResourceMode) {
      // In add resource mode, only show filtered resources from allResources
      const filteredResources = (allResources ?? []).filter(resource =>
        resource.title.toLowerCase().includes(lowerCaseQuery) ||
        (resource.description && resource.description.toLowerCase().includes(lowerCaseQuery))
      );
      return { projects: [], resources: filteredResources };
    }
    
    // Normal mode - filter current projects and resources
    const filteredProjects = (projects ?? []).filter(project =>
      project.title.toLowerCase().includes(lowerCaseQuery) ||
      (project.description && project.description.toLowerCase().includes(lowerCaseQuery))
    );
    const filteredResources = (resources ?? []).filter(resource =>
      resource.title.toLowerCase().includes(lowerCaseQuery) ||
      (resource.description && resource.description.toLowerCase().includes(lowerCaseQuery))
    );
    return { projects: filteredProjects, resources: filteredResources };
  }, [projects, resources, allResources, currentQuery, isAddResourceMode]);


  useEffect(() => {
    navigationRef.current = navigationState;
  }, [navigationState]);


  const tableData = useMemo(() => {
    const items: ProjectOrResource[] = [];
    
    // Add filtered projects/folders
    filteredItems.projects.forEach((project) => {
      items.push({
        id: project.id,
        title: project.title,
        description: project.description || '',
        creationDate: project.creationDate,
        itemType: project.projectType === 'root' ? 'project' : 'folder',
        projectType: project.projectType,
      });
    });
    
    // Add filtered resources
    filteredItems.resources.forEach(resource => {
      items.push({
        id: resource.id,
        title: resource.title,
        description: resource.description || '',
        itemType: 'resource',
      });
    });
    
    return items;
  }, [filteredItems]);

  // Column definitions
  const columnDefs = useMemo<ColDef[]>(() => {
    const baseColumns: ColDef[] = [
      { 
        field: "title", 
        cellRenderer: (params: any) => <ItemRenderer {...params} />,
        minWidth: 200,
        flex: 1, 
        resizable: true 
      },
      { field: "description", minWidth: 500, flex: 3, resizable: true },
    ];

    // Add type and date columns only when not in add resource mode
    if (!isAddResourceMode) {
      baseColumns.push(
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
        }
      );
    }

    // Add download column only when not in add resource mode
    if (!isAddResourceMode) {
      baseColumns.push({
      field: "", 
      minWidth: 30, 
      maxWidth: 50, 
      cellRenderer: (params: any) => {
        if (params.data.itemType === 'resource') {
        return <DownloadRenderer data={params.data} />;
        }
        if (params.data.itemType === 'folder' || params.data.itemType === 'project') {
        return <EditRenderer data={params.data} onEdit={handleEdit} />;
        }
        return null;
      }, 
      resizable: true 
      });
    }

    baseColumns.push(
      {
      field: "",
      minWidth: 30,
      maxWidth: 50,
      cellRenderer: (params: any) => (<RemoveRenderer data={params.data} onRemove={handleRemove} isAddResourceMode={isAddResourceMode} />),
      cellStyle: { display: "flex", alignItems: "center", justifyContent: "center", padding: 0 },
      resizable: true
      }
    )

    return baseColumns;
  }, [isAddResourceMode]);

  const gridApiRef = useRef<GridApi | null>(null);

  // Use sidebar context
  const { rightSidebarOpen, toggleRightSidebar } = useSidebar();

  // When grid is ready, set the gridApi
  const onGridReady = (params: GridReadyEvent) => {
    gridApiRef.current = params.api;
  };

  // Row selection
  const rowSelection = useMemo<RowSelectionOptions>(() => {
    if (isAddResourceMode) {
      return {
        checkboxes: true,
        mode: "multiRow",
      };
    }
    
    return {
      checkboxes: false,
      mode: "singleRow",
    };
  }, [isAddResourceMode]);

  // Add handler for selection changes in add resource mode:
  const onSelectionChanged = (event: any) => {
    if (isAddResourceMode) {
      const selectedNodes = event.api.getSelectedNodes();
      const newlySelectedIds = new Set<string>(selectedNodes.map((node: any) => node.data.id));

      setSelectedResourceIds(prev => {
        // Start with the previous set
        const updated = new Set(prev);

        // Get all visible node IDs
        const visibleNodeIds = new Set<string>();
        event.api.forEachNode((node: any) => {
          visibleNodeIds.add(node.data.id);
        });

        // Remove any visible IDs that are no longer selected
        for (const id of visibleNodeIds) {
          if (!newlySelectedIds.has(id)) {
            updated.delete(id);
          }
        }

        // Add all newly selected IDs
        for (const id of newlySelectedIds) {
          updated.add(id);
        }

        return updated;
      });
    }
  };
  
  // Handler to restore selection after grid data changes (e.g., filtering/searching)
  const restoreSelection = () => {
    if (isAddResourceMode && gridApiRef.current) {
      gridApiRef.current.forEachNode((node) => {
        node.setSelected(selectedResourceIds.has(node.data.id), false);
      });
    }
  };

  // Restore selection whenever tableData or selectedResourceIds changes in add resource mode
  useEffect(() => {
    restoreSelection();
  }, [tableData, selectedResourceIds, isAddResourceMode]);

  // #region Page Navigation

  // Function to refresh the current list of projects, for the root level
  const navigateToRoot = async () => {
    setIsLoading(true);
    try {

      setNavigationState({
        currentProjectId: null,
        currentLevel: 0,
        navigationPath: []
      });

      const projectFetch: ApiResponse = await ListProjectsPaged(1, ""); 
      if (projectFetch.success && projectFetch.body?.projects) {

        // Clear focus before changing data, otherwise throws error
        if (gridApiRef.current) {
          gridApiRef.current.clearFocusedCell(); 
        }

        setProjects(projectFetch.body.projects);
        setResources(initialResources); 
      } else {
        console.error("Failed to fetch root projects:", projectFetch.message);
      }
    } catch (error) {
      console.error("Error fetching root projects:", error);
    } finally {
      setIsLoading(false);
    }
  };

  // Function to refresh the current project contents
  const refreshCurrentProject = async () => {
    if (!navigationState.currentProjectId) return;
    
    try {
      setIsLoading(true);
      const { resources: newResources, projects: newProjects } = await fetchProjectContent(navigationState.currentProjectId);
      
      // Clear focus
      if (gridApiRef.current) {
        gridApiRef.current.clearFocusedCell();
      }
      
      setResources(newResources);
      setProjects(newProjects);
    } catch (error) {
      console.error("Error refreshing current project:", error);
    } finally {
      setIsLoading(false);
    }
  };


  /**
   * Navigate to a specific project and load its contents
   * @param projectId - ID of the project to navigate to
   * @param projectTitle - Title of the project for breadcrumb
   */
  const navigateToProject = async (projectId: string, projectTitle: string) => {
    try {
      setIsLoading(true);

      setNavigationState(prev => ({
        currentProjectId: projectId,
        currentLevel: prev.currentLevel + 1,
        navigationPath: [...prev.navigationPath, { id: projectId, title: projectTitle }]
      }));

      const { resources: newResources, projects: newProjects } = await fetchProjectContent(projectId);

      // Clear focus before changing data, otherwise throws error
      if (gridApiRef.current) {
        gridApiRef.current.clearFocusedCell(); 
      }
      setResources(newResources);
      setProjects(newProjects);
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
    if (navigationState.navigationPath.length <= 1) {
      return navigateToRoot();
    }
    
    try {
      setIsLoading(true);
      
      // Get the parent project ID
      const newPath = [...navigationState.navigationPath];
      newPath.pop(); // Remove current
      const parentItem = newPath[newPath.length - 1];

      setNavigationState({
        currentProjectId: parentItem.id,
        currentLevel: newPath.length,
        navigationPath: newPath
      })

      if (gridApiRef.current) {
        gridApiRef.current.clearFocusedCell(); 
      }

      // Fetch the parent project content
      const { resources: newResources, projects: newProjects } = await fetchProjectContent(parentItem.id);
      
      // Update state
      setResources(newResources);
      setProjects(newProjects);

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
      const targetPath = navigationState.navigationPath.slice(0, index + 1);
      const targetItem = targetPath[targetPath.length - 1];
      
      setNavigationState({
        currentProjectId: targetItem.id,
        currentLevel: index + 1,
        navigationPath: targetPath
      });

      if (gridApiRef.current) {
        gridApiRef.current.clearFocusedCell(); 
      }

      // Fetch the project content
      const { resources: newResources, projects: newProjects } = await fetchProjectContent(targetItem.id);
      
      // Update state
      setResources(newResources);
      setProjects(newProjects);

    } catch (error) {
      console.error("Error navigating to breadcrumb:", error);
      // Handle error
    } finally {
      setIsLoading(false);
    }
  };


  // If on row clicked, handle navigation for projects or toggle sidebar for resources
  const onRowClicked = async (e: RowClickedEvent) => {
    // Do nothing if in add resource mode, let checkboxes handle selection
    if (isAddResourceMode) return;
    
    // Do nothing if the download button is clicked
    if ((e.event?.target as HTMLElement)?.closest(".download-button")) return;

    // Do nothing if the remove button is clicked
    if ((e.event?.target as HTMLElement)?.closest(".remove-button")) return;
    
    // Do nothing if the edit button is clicked
    if ((e.event?.target as HTMLElement)?.closest(".edit-button")) return;

    const rowData = e.data as ProjectOrResource;
    
    if ((rowData.itemType === 'project' || rowData.itemType === 'folder')) {
      // Navigate to the project/folder
      await navigateToProject(rowData.id, rowData.title);
    } else if (rowData.itemType === 'resource') {
      // Toggle sidebar for resources
      toggleRightSidebar(e.data);
      e.node.setSelected(true);
    }
  };

  // #region Creation / Deletion

  // Project creation handlers
  const handleOpenCreateProjectModal = () => {
    setIsCreateProjectModalOpen(true);
  };
  
  const handleCloseCreateProjectModal = () => {
    setIsCreateProjectModalOpen(false);
  };
  
  const handleProjectCreationSuccess = async () => {
    handleCloseCreateProjectModal();
    await navigateToRoot();
  };

  // Folder creation handlers
  const handleOpenCreateFolderModal = () => {
    setIsCreateFolderModalOpen(true);
  };
  
  const handleCloseCreateFolderModal = () => {
    setIsCreateFolderModalOpen(false);
  };
  
  const handleFolderCreationSuccess = async () => {
    handleCloseCreateFolderModal();
    await refreshCurrentProject();
  };

  // Update the handleOpenAddResourceModal function:
  const handleAddResources = async () => {
    setIsAddResourceMode(true);
    setIsLoading(true);
    const newResources: Resource[] = await fetchAllResources();
    setIsLoading(false);
    setAllResources(newResources);
  };

  // Add new handler functions:
  const handleCancelAddResource = () => {
    setIsAddResourceMode(false);
    setSelectedResourceIds(new Set());
    setAllResources([]);
  };

  const handleConfirmAddResource = async () => {
    try {
      setIsLoading(true);
      
      // Convert selected IDs to array
      const selectedIds = Array.from(selectedResourceIds);
      
      if (selectedIds.length === 0) {
        handleCancelAddResource();
        return;
      }

      const projectId: string = navigationState.currentProjectId || "";
      
      for (const id of selectedIds) {
        await addResourceToProject(projectId, id);
      }
      
      // Exit add resource mode and refresh
      setIsAddResourceMode(false);
      setSelectedResourceIds(new Set());
      setAllResources([]);
      
      // Refresh current project content
      if (navigationState.currentProjectId) {
        await refreshCurrentProject();
      } else {
        await navigateToRoot();
      }
      
    } catch (error) {
      console.error("Error adding resources:", error);
    } finally {
      setIsLoading(false);
    }
  };

  // Remove handler
  const handleRemove = async (item: ProjectOrResource) => {
    const projectId = navigationRef.current.currentProjectId;

    if (!projectId) {
      return;
    }

    setIsLoading(true);

    // Clear any selection and focus before removing
    if (gridApiRef.current) {
      gridApiRef.current.deselectAll();
      gridApiRef.current.clearFocusedCell();
    }
    
    try {
      (item.itemType == "resource") 
        ? await removeResourceFromProject(projectId, item.id)
        : await deleteProject(item.id);
        
      const { resources, projects } = await fetchProjectContent(projectId);
      setResources(resources);
      setProjects(projects);
    } catch (err) {
      console.error("Remove failed:", err);
    } finally {
      setIsLoading(false);
    }
  };

  const handleEdit = async (item: ProjectOrResource) => {
    setSelectedProjectForEdit({
      id: item.id,
      title: item.title,
      description: item.description || '',
      projectType: item.projectType
    } as Project);
    setIsEditModalOpen(true);
  };

  const handleCloseEditModal = () => {
    setIsEditModalOpen(false);
    setSelectedProjectForEdit(null);
  };

  const handleEditSuccess = async () => {
    handleCloseEditModal();
    // Refresh the current view
    if (navigationState.currentProjectId) {
      await refreshCurrentProject();
    } else {
      await navigateToRoot();
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
      {/* Search bar */}
      <div className="mb-4">
        <div className="relative w-full mb-2"> 
          <Input
            className="peer h-10 ps-9"
            placeholder="Search"
            type="text"
            value={currentQuery}
            onChange={(e) => setCurrentQuery(e.target.value)}
          />
          <div className="text-muted-foreground/80 pointer-events-none absolute inset-y-0 start-0 flex items-center justify-center ps-3 peer-disabled:opacity-50">
            <Search className="h-4 w-4" aria-hidden="true" fill="currentColor" />
          </div>
        </div>

        {/* Controls section - buttons, dropdown, breadcrumbs */}
        <div className="flex items-center space-x-2 flex-wrap">
          {isAddResourceMode ? (
            // Add Resource Mode Controls
            <>
              <Button 
                variant="outline" 
                size="sm" 
                onClick={handleCancelAddResource}
                disabled={isLoading}
              >
                Cancel
              </Button>
              <Button 
                variant="default" 
                size="sm" 
                onClick={handleConfirmAddResource}
                disabled={isLoading || selectedResourceIds.size === 0}
              >
                Add Selected ({selectedResourceIds.size})
              </Button>
              {isLoading && (
                <div className="ml-2 text-sm text-gray-500">Loading resources...</div>
              )}
            </>
          ) : (
            // Normal Mode Controls
            <>
              {/* Dropdown to create projects etc */}
              <ProjectActionsDropdown 
                currentLevel={navigationState.currentLevel} 
                isLoading={isLoading}
                onCreateProject={handleOpenCreateProjectModal}
                onCreateFolder={handleOpenCreateFolderModal}
                onAddResource={handleAddResources}
              />

              {/* Navigation Controls */}
              <Button 
                variant="outline" 
                size="sm" 
                onClick={navigateToRoot}
                disabled={navigationState.currentLevel === 0 || isLoading}
              >
                <HomeIcon size={16} className="mr-1" />
                Projects
              </Button>
              
              {navigationState.currentLevel > 0 && (
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
              {navigationState.navigationPath.length > 0 && (
                <div className="flex items-center overflow-x-auto px-2">
                  {navigationState.navigationPath.map((item, index) => (
                    <React.Fragment key={item.id}>
                      {index > 0 && <span className="mx-1 text-gray-500">/</span>}
                      <button
                        onClick={() => navigateToBreadcrumb(index)}
                        disabled={isLoading || index === navigationState.navigationPath.length - 1}
                        className={`text-sm hover:underline ${
                          index === navigationState.navigationPath.length - 1 
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
            </>
          )}
        </div>
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
          onSelectionChanged={onSelectionChanged}
          overlayLoadingTemplate="<span class='ag-overlay-loading-center'>Loading content...</span>"
          overlayNoRowsTemplate={isAddResourceMode ? 
            "<span class='ag-overlay-no-rows-center'>No resources found</span>" : 
            "<span class='ag-overlay-no-rows-center'>No items found in this location</span>"
          }
          loading={isLoading}
        />
      </div>

      {/* Project creation modal */}
      <CreateProjectModal 
        isOpen={isCreateProjectModalOpen}
        onClose={handleCloseCreateProjectModal}
        onSuccess={handleProjectCreationSuccess}
      />

      {/* Folder creation modal - only enabled when inside a project */}
      <CreateFolderModal 
        isOpen={isCreateFolderModalOpen}
        onClose={handleCloseCreateFolderModal}
        onSuccess={handleFolderCreationSuccess}
        parentProjectId={navigationState.currentProjectId}
      />

      {/* Project edit modal - only enabled when clicking edit button */}
      <EditProjectModal 
        isOpen={isEditModalOpen}
        onClose={handleCloseEditModal}
        onSuccess={handleEditSuccess}
        project={selectedProjectForEdit}
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


function RemoveRenderer({ data, onRemove, isAddResourceMode }: { 
  data: any; 
  onRemove: (item: ProjectOrResource) => void; 
  isAddResourceMode: boolean; 
}) {
  if (isAddResourceMode) return null;
  return (
    <div className="remove-button flex items-center justify-center w-full h-full">
      <Button
        variant="ghost"
        size="icon"
        className="flex items-center justify-center hover:bg-gray-200 text-muted-foreground"
        title="Remove"
        onClick={() => onRemove(data)}
        aria-label="Remove"
      >
        <CircleXIcon size={18} />
      </Button>
    </div>
  );
}

function EditRenderer({ data, onEdit }: { data: any, onEdit: (item: ProjectOrResource) => void }) {
  return (
    <div className="edit-button flex items-center justify-center w-full h-full">
      <Button
        variant="ghost"
        size="icon"
        className="flex items-center justify-center hover:bg-gray-200 text-muted-foreground"
        title="Edit"
        onClick={() => onEdit(data)}
        aria-label="Edit"
      >
        <EditIcon size={18} />
      </Button>
    </div>
  );
}
// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)