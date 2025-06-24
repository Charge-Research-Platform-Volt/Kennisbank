"use client";

import React, { useEffect, useMemo, useRef, useState } from "react";
import { AgGridReact } from "ag-grid-react";
import type { ColDef, GridApi, GridReadyEvent, RowClickedEvent, RowSelectionOptions } from "ag-grid-community";
import { AllCommunityModule, ModuleRegistry } from "ag-grid-community";
import { Resource, ResourceProject } from "@/types/resource.type";
import { FolderProject, Project } from "@/types/project.type";
import { tableTheme } from "@/lib/tableConfig";
import GetFileIcon from "../getFileIcon";
import { format, parseISO } from "date-fns";
import OpenFileButton from "../open-file-button";
import { MetadataTypeEnum, useSidebar } from "@/context/sidebar-provider";
import { ArrowLeftIcon, FolderIcon, HomeIcon, SparklesIcon, CircleXIcon, TagsIcon, Users, EditIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { addResourceToProject, fetchAllResources, ListProjectsPaged, removeResourceFromProject, deleteProject } from "@/actions/projectActions";
import { ApiResponse } from "@/types/apiResponse.type";
import CreateProjectModal from "@/app/(knowledgebank)/projects/components/create-project-modal";
import { ProjectActionsDropdown } from "../../app/(knowledgebank)/projects/components/projects-dropdown";
import CreateFolderModal from "@/app/(knowledgebank)/projects/components/create-folder-modal";
import { Input } from "@/components/ui/input";
import Search from "@/icons/search-icon";
import GeneratePopup from "./popup";
import { Tag } from "@/types/tag.type";
import EditProjectModal from "@/app/(knowledgebank)/projects/components/edit-project-modal";
import { User } from "@/types/user.type";
import { FetchWithValidation } from "@/lib/fetchWithValidation";
import { z } from "zod";
import { toast } from "sonner";


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
  addedBy? : string;
  creatorRelations?: any[];
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
  initialResources: ResourceProject[];
  initialProjects: FolderProject[];
  // Function to fetch data for a specific project ID
  fetchProjectAction: (projectId: string) => Promise<{ resources: ResourceProject[], projects: FolderProject[], creators: User[], tags: Tag[] }>;
  currentUserId: string;
  userRole: string | null;
}

/**
 * Component for listing projects and resources in a table with navigation capabilities
 * 
 * @author Jelle v.h. Schut, Justin Liem
 * @param {ResourceProject[]} initialResources - Initial array of resources to display
 * @param {FolderProject[]} initialProjects - Initial array of projects to display
 * @param {string => Promise<{resources: ResourceProject, projects: FolderProject, creators: User[], tags: Tag[]}>}fetchProjectContent - Function to fetch content of a project when navigating into it
 * @param {string} currentUserId - Current user Id
 * @returns A navigable table representation of projects and resources
 */
export default function ListProjects({initialResources, initialProjects, fetchProjectAction: fetchProjectContent, currentUserId, userRole}: ListProjectsProps) {
  // Query for searching
  const [currentQuery, setCurrentQuery] = useState<string>("");
  // State for current projects and resources being displayed
  const [resources, setResources] = useState<ResourceProject[]>(initialResources);
  const [projects, setProjects] = useState<FolderProject[]>(initialProjects);
  const [creators, setCreators] = useState<User[]>([]);
  const [tags, setTags] = useState<Tag[]>([]);
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
  const [allResources, setAllResources] = useState<ResourceProject[]>([]);
  const [selectedResourceIds, setSelectedResourceIds] = useState<Set<string>>(new Set());

  // state for project tags and creators popup
  const [isProjectsTagsModalOpen, setIsProjectTagsModalOpen] = useState(false);
  const [isProjectCreatorsModalOpen, setIsProjectCreatorsModalOpen] = useState(false);
  const [creatorsEdit, setCreatorsEdit] = useState<User[]>([]);
  const [tagsEdit, setTagsEdit] = useState<Tag[]>([]);

  // Handles filtering the projects and resources based on the current query
  const filteredItems = useMemo(() => {
    const lowerCaseQuery = currentQuery.toLowerCase();
    
    if (isAddResourceMode) {
      // In add resource mode, only show filtered resources from allResources
      const filteredResources = (allResources ?? []).filter(resource =>
        resource.resource.title.toLowerCase().includes(lowerCaseQuery) ||
        (resource.resource.description && resource.resource.description.toLowerCase().includes(lowerCaseQuery))
      );
      return { projects: [], resources: filteredResources };
    }

    // Normal mode - filter current projects and resources
    const filteredProjects = (projects ?? []).filter(project =>
      project.folder.title.toLowerCase().includes(lowerCaseQuery) ||
      (project.folder.description && project.folder.description.toLowerCase().includes(lowerCaseQuery))
    );
    const filteredResources = (resources ?? []).filter(resource =>
      resource.resource.title.toLowerCase().includes(lowerCaseQuery) ||
      (resource.resource.description && resource.resource.description.toLowerCase().includes(lowerCaseQuery))
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
        id: project.folder.id,
        title: project.folder.title,
        description: project.folder.description || '',
        creationDate: project.folder.creationDate,
        addedBy: project.addedBy || '',
        itemType: project.folder.projectType === 'root' ? 'project' : 'folder',
        projectType: project.folder.projectType,
        creatorRelations: project.creatorRelations || [],
      });
    });
    
    // Add filtered resources
    filteredItems.resources.forEach(resource => {
      items.push({
        id: resource.resource.id,
        title: resource.resource.title,
        description: resource.resource.description || '',
        addedBy: resource.addedBy || '',
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
          minWidth: 45, 
          resizable: true,
          valueFormatter: (params) => 
            params.value === 'project' ? 'Project'
            : params.value === 'folder' ? 'Folder'
            : params.value === 'resource' ? 
                params.data.fileType : ''
        },
        { field: "addedBy",
          headerName: "Added By",
          minWidth: 50,
          resizable: true
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
        return <EditRenderer data={params.data} onEdit={handleEdit} currentUserId={currentUserId} userRole={userRole} />;
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
      cellRenderer: (params: any) => (<RemoveRenderer data={params.data} onRemove={handleRemove} isAddResourceMode={isAddResourceMode} currentUserId={currentUserId} userRole={userRole} />),
      cellStyle: { display: "flex", alignItems: "center", justifyContent: "center", padding: 0 },
      resizable: true
      }
    )

    return baseColumns;
  }, [isAddResourceMode]);

  const gridApiRef = useRef<GridApi | null>(null);

  // Use sidebar context
  const { rightSidebarOpen, openRightSidebar } = useSidebar();

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
  // Modal handlers for opening / closing them
  const handleOpenProjectTagsModal = () => {
    setIsProjectTagsModalOpen(true);
  };
  
  const handleCloseProjectTagsModal = () => {
    setIsProjectTagsModalOpen(false);
  };

  const handleOpenProjectCreatorsModal = () => {
    setIsProjectCreatorsModalOpen(true);
  };
  
  const handleCloseProjectCreatorsModal = () => {
    setIsProjectCreatorsModalOpen(false);
  };
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
        const projectsWithCreators = projectFetch.body.projects.map((p: any) => {          
          return { folder: p, addedBy: "", creatorRelations: p.projectCreatorRelations || [] };
        });

        // Clear focus before changing data, otherwise throws error
        if (gridApiRef.current) {
          gridApiRef.current.clearFocusedCell(); 
        }

        setProjects(projectsWithCreators);
        setResources(initialResources); 
        setTags([]);
        setCreators([]);
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
    if (!navigationRef.current.currentProjectId) return;
    
    try {
      setIsLoading(true);
      const { resources: newResources, projects: newProjects, creators: newCreators, tags: newTags } = await fetchProjectContent(navigationRef.current.currentProjectId);
      // Clear focus
      if (gridApiRef.current) {
        gridApiRef.current.clearFocusedCell();
      }
      setResources(newResources);
      setProjects(newProjects);
      setTags(newTags);
      setCreators(newCreators);
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

      const { resources: newResources, projects: newProjects, creators: newCreators, tags: newTags } = await fetchProjectContent(projectId);

      // Clear focus before changing data, otherwise throws error
      if (gridApiRef.current) {
        gridApiRef.current.clearFocusedCell(); 
      }
      setResources(newResources);
      setProjects(newProjects);
      setTags(newTags);
      setCreators(newCreators);
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
      const { resources: newResources, projects: newProjects, creators: newCreators, tags: newTags } = await fetchProjectContent(parentItem.id);
      
      // Update state
      setResources(newResources);
      setProjects(newProjects);
      setTags(newTags);
      setCreators(newCreators);

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
      const { resources: newResources, projects: newProjects, creators: newCreators, tags: newTags  } = await fetchProjectContent(targetItem.id);
      
      // Update state
      setResources(newResources);
      setProjects(newProjects);
      setTags(newTags);
      setCreators(newCreators);

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
      
      openRightSidebar(e.data.id, MetadataTypeEnum.RESOURCE);
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
    try {
      await navigateToRoot();
      toast.success("Project created successfully.");
    } catch (error) {
      toast.error("Failed to refresh project list after creation.");
    }
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
    try {
      await refreshCurrentProject();
      toast.success("Folder created successfully.");
    } catch (error) {
      toast.error("Failed to refresh folder list after creation.");
    }
  };

  // Update the handleOpenAddResourceModal function:
  const handleAddResources = async () => {
    setIsAddResourceMode(true);
    setIsLoading(true);
    try {
      const newResources: Resource[] = await fetchAllResources();
      setAllResources(newResources.map(r => {return {resource: r, addedBy: ""}}));
      toast.success("Fetched all resources.");
    } catch (error) {
      toast.error("Failed to fetch resources.");
    } finally {
      setIsLoading(false);
    }
  };

  // Add new handler functions:
  const handleCancelAddResource = () => {
    setIsAddResourceMode(false);
    setSelectedResourceIds(new Set());
    setAllResources([]);
    toast.success("Add resource mode cancelled.");
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
        const response = await addResourceToProject(projectId, id);
        if (response?.success) {
          toast.success(response.message || "Resource added successfully.");
        } else {
          toast.error(response?.message || "Failed to add resource.");
        }
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
      toast.error("Error adding resources.");
      console.error("Error adding resources:", error);
    } finally {
      setIsLoading(false);
    }
  };

  // Remove handler
  const handleRemove = async (item: ProjectOrResource) => {
    const projectId = navigationRef.current.currentProjectId;
    console.log("delete")

    setIsLoading(true);

    // Clear any selection and focus before removing
    if (gridApiRef.current) {
      gridApiRef.current.deselectAll();
      gridApiRef.current.clearFocusedCell();
    }
    
    try {
      if (item.itemType == "resource") {
        const response: ApiResponse = await removeResourceFromProject(projectId ? projectId : "", item.id);
        if (response.success) {
          toast.success(response.message);
        } else {
          toast.error(response.message);
        }

        const { resources, projects, creators, tags } = await fetchProjectContent(projectId ? projectId : "");
        setResources(resources);
        setProjects(projects);
        setTags(tags);
        setCreators(creators);
      }
      else {
        // If we delete a project, navigate to the full projects overview, otherwise reload the folder you are in
        const response: ApiResponse = await deleteProject(item.id);
        if (response.success) {
          toast.success(response.message);
        } else {
          toast.error(response.message);
        }

        if(navigationRef.current.currentLevel >= 1) {
          await refreshCurrentProject();
        }
        else {
          await navigateToRoot()};
      }
    } catch (err) {
      toast.error("Remove failed.");
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

  // For excluding current creators of a project when updating
  useEffect(() => {
    const loadProjectContent = async () => {
      if (selectedProjectForEdit && selectedProjectForEdit.id) {
        try {
          const { resources, projects, creators, tags } = await fetchProjectContent(selectedProjectForEdit.id);
          setCreatorsEdit(creators ? creators : []);
          setTagsEdit(tags ? tags : [])
        } catch (error) {
          toast.error("Failed to fetch project creators.");
        }
      }
    };

    if (isEditModalOpen) { // Only fetch when the modal is open and selectedProjectForEdit is set
      loadProjectContent();
    }
  }, [selectedProjectForEdit, isEditModalOpen]);

  const handleCloseEditModal = () => {
    setIsEditModalOpen(false);
    setSelectedProjectForEdit(null);
    setCreatorsEdit([]);
    setTagsEdit([]);
  };

  const handleEditSuccess = async () => {
    handleCloseEditModal();
    // Refresh the current view
    try {
      if (navigationState.currentProjectId) {
        await refreshCurrentProject();
      } else {
        await navigateToRoot();
      }
      toast.success("Project updated successfully.");
    } catch (error) {
      toast.error("Failed to refresh after editing project.");
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
      <div className="mb-2">
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
                data-testid="confirm-add"
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
              {/* Back button */}
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
              {/* Project Tags Button*/}
              { navigationState.currentLevel === 1 &&
                <Button
                  variant="outline"
                  size ="sm"
                  onClick={handleOpenProjectTagsModal}
                  disabled={isLoading}>
                  <TagsIcon size={16} className= "mr-1"></TagsIcon>
                  Project Tags
                </Button>
              }
              {/* Project Creators Button*/}
              { navigationState.currentLevel === 1 &&
                <Button
                  variant="outline"
                  size ="sm"
                  onClick={handleOpenProjectCreatorsModal}
                  disabled={isLoading}>
                  <Users size={16} className= "mr-1"></Users>
                  Project Creators
                </Button>
              }
              {/* Breadcrumbs, used to navigate back */}
              {navigationState.navigationPath.length > 0 && (
                <div className="flex items-center overflow-x-auto px-2">
                  {navigationState.navigationPath.map((item, index) => (
                    <React.Fragment key={item.id}>
                      {index > 0 && <span className="mx-1 text-gray-500">/</span>}
                      <button
                        onClick={() => navigateToBreadcrumb(index)}
                        disabled={isLoading || index === navigationState.navigationPath.length - 1}
                        className={`text-sm hover:underline hover:cursor-pointer ${
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
      <div className="h-full w-full">
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
        currentUserId={currentUserId}
      />

      {/* Folder creation modal - only enabled when inside a project */}
      <CreateFolderModal 
        isOpen={isCreateFolderModalOpen}
        onClose={handleCloseCreateFolderModal}
        onSuccess={handleFolderCreationSuccess}
        parentProjectId={navigationState.currentProjectId}
      />

      {/* Project Tag Modal */}
      <GeneratePopup
        title="Tags"
        description="These are the tags of this project"
        content={tags.map(tag => tag.name)}
        open={isProjectsTagsModalOpen}
        onClose={handleCloseProjectTagsModal}/>

      {/* Project Creators Modal */}
      <GeneratePopup
        title="Creators"
        description="These are the creators of this project / folder"
        content={creators.map(creator => creator.email)}
        open={isProjectCreatorsModalOpen}
        onClose={handleCloseProjectCreatorsModal}/>

      {/* Project edit modal - only enabled when clicking edit button */}
      <EditProjectModal 
        isOpen={isEditModalOpen}
        onClose={handleCloseEditModal}
        onSuccess={handleEditSuccess}
        project={selectedProjectForEdit}
        currentCreators={creatorsEdit}
        currentTags={tagsEdit}
      />
    </div>
  );
}

/**
 * Renders either a folder or file entry depending on the item type
 * 
 * @author Jelle v.h. Schut
 * @param {{ data: ProjectOrResource, value: string }} params - The parameters for rendering the item
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
 * 
 * @author Jelle v.h. Schut
 * @param {{data: any}} params - The parameters for rendering the download button
 * @returns A download button component or null
 */
function DownloadRenderer({ data }: { data: any }) {
  return (
    <div className="download-button flex items-center justify-center">
      <OpenFileButton id={data.id} fileType={data.fileType} asIcon={true} />
    </div>
  );
}

/**
 * Renders a remove button
 * 
 * @author Jelle v.h. Schut
 * @param {any} data - What to remove
 * @param {(item: ProjectOrResource) => void} onRemove - What to execute after removing
 * @param {boolean} isAddResourceMode - Whether we are adding resources or not
 * @param {string} currentUserId - Id of the current user
 * @param {string | null} userRole - Role of the current user
 * @returns A remove button component or null
 */
function RemoveRenderer({ data, onRemove, isAddResourceMode, currentUserId, userRole }: { 
  data: any; 
  onRemove: (item: ProjectOrResource) => void; 
  isAddResourceMode: boolean; 
  currentUserId: string;
  userRole: string | null;
}) {
  if (isAddResourceMode) return null;
  return (
    <div className="remove-button flex items-center justify-center w-full h-full">
      <Button
        data-testid = "delete-project-or-resource"
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

/**
 * Renders an edit button
 *
 * @author Jelle v.h. Schut
 * @param {any} data - What to edit
 * @param {(item: ProjectOrResource) => void} onEdit - What to execute after editing
 * @param {boolean} isAddResourceMode - Whether we are adding resources or not
 * @param {string} currentUserId - Id of the current user
 * @param {string | null} userRole - Role of the current user
 * @returns A edit button component or null
 */
function EditRenderer({ data, onEdit, currentUserId, userRole }: { data: any, onEdit: (item: ProjectOrResource) => void, currentUserId: string, userRole: string | null }) {
  const creatorRelations = data.creatorRelations || [];

  // Check if current user is a creator
  const hasPermission = creatorRelations.some(
    (rel: any) => rel.creatorId === currentUserId
  );
  if (!hasPermission && userRole != "admin") return null;
  
  return (
    <div className="edit-button flex items-center justify-center w-full h-full">
      <Button
        data-testid= "edit-project-or-folder"
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