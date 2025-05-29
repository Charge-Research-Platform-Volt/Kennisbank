import { expect, test, vi, beforeEach, describe } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import React from 'react';
import { addResourceToProject, createFolder, createNewProject, deleteProject, fetchAllResources, getProjectContentById, ListProjectsPaged, removeResourceFromProject, updateProject } from '@/actions/projectActions';
import ProjectsPage from '@/app/(knowledgebank)/projects/page';
import ListProjects from '@/components/projects/list-projects';
import { SidebarProvider } from '@/context/sidebar-provider';
import userEvent from '@testing-library/user-event';

// test if project deletion is called
// test if project add is called
// test if project update is called
// test if project link/unlink is called
// test if folder addition is called

vi.mock('@/actions/projectActions', () => ({
    ListProjectsPaged: vi.fn().mockResolvedValue({ success: true, message: 'Projects fetched successfully.', body: { pageCount: 1, pageIndex: 1, pageSize: 50, projects: [{ creationDate: "2025-05-29T10:32:11.319683Z", deletionDate: "2025-05-29T10:32:11.319683Z", description: null, id: "89279395-9c59-4ede-9039-70cf15d8a958", projectType: "root", title: "test project" }]} }),
    getProjectContentById: vi.fn().mockResolvedValue({success: true, message: 'Content fetched successfully'}),
    createNewProject: vi.fn().mockResolvedValue({success: true, message: 'Project created successfully'}),
    createFolder: vi.fn().mockResolvedValue({success: true, message: 'Folder created successfully'}),
    fetchAllResources: vi.fn().mockResolvedValue({success: true, message: 'Resources fetched successfully'}),
    addResourceToProject: vi.fn().mockResolvedValue({success: true, message: 'Resource added to project successfully'}),
    removeResourceFromProject: vi.fn().mockResolvedValue({success: true, message: 'Resource removed from project successfully'}),
    deleteProject: vi.fn().mockResolvedValue({success: true, message: 'Project deleted successfully'}),
    updateProject: vi.fn().mockResolvedValue({success: true, message: 'Updated project successfully'}),
}));

describe('Projects page', () =>{
    beforeEach(() => {
        vi.clearAllMocks();
});
    beforeAll(() => {
        window.PointerEvent = MouseEvent as typeof PointerEvent;
    });

    test('Test if projects are fetched correctly', async() => {
        render(<ProjectsPage/>)

        // EXPECT THE LISTPROJECTSPAGED ACTION TO HAVE BEEN CALLED ONCE
        await waitFor(() => expect(ListProjectsPaged).toHaveBeenCalledTimes(1));
    });

    test('Test if adding projects calls the correct function', async() => {
        render(<SidebarProvider leftSidebarDefaultState={true}><ListProjects initialProjects={[]} initialResources={[]} fetchProjectAction={getProjectContentById}/></SidebarProvider>)
        const user = userEvent.setup();

        // CLICK "CREATE NEW PROJECT"
        const addButton = await screen.findByTestId("add");
        user.click(addButton);
        const createProjectButton = await screen.findByTestId("add-project");
        user.click(createProjectButton);

        // FILL IN ONLY TITLE TO JUST TEST CALL
        const titleInput = await screen.findByTestId("input-project-title");
        act(() => {
            fireEvent.change(titleInput, {target: {value:"test title"} });
        });

        // CLICK ON CREATE
        const createButton = await screen.findByTestId("create-project");
        user.click(createButton);
        await waitFor(() => expect(createNewProject).toHaveBeenCalledTimes(1));
    })
})