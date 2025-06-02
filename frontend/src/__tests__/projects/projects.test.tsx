import { expect, test, vi, beforeEach, describe } from 'vitest'
import { render, screen, fireEvent, waitFor, act } from '@testing-library/react'
import React from 'react';
import { createNewProject, deleteProject, getProjectContentById, updateProject } from '@/actions/projectActions';
import ListProjects from '@/components/projects/list-projects';
import { SidebarProvider } from '@/context/sidebar-provider';
import userEvent from '@testing-library/user-event';

vi.mock('@/actions/projectActions', () => ({
    ListProjectsPaged: vi.fn().mockResolvedValue({ success: true, message: 'Projects fetched successfully.', body: { pageCount: 1, pageIndex: 1, pageSize: 50, projects: [{ creationDate: "2025-05-29T10:32:11.319683Z", deletionDate: "2025-05-29T10:32:11.319683Z", description: null, id: "89279395-9c59-4ede-9039-70cf15d8a958", projectType: "root", title: "test project" }]} }),
    getProjectContentById: vi.fn().mockResolvedValue({success: true, message: 'Content fetched successfully', body: { resources: [], tags: [], projects: [], creators: []}}),
    createNewProject: vi.fn().mockResolvedValue({success: true, message: 'Project created successfully'}),
    createFolder: vi.fn().mockResolvedValue({success: true, message: 'Folder created successfully'}),
    fetchAllResources: vi.fn().mockResolvedValue({success: true, message: 'Resources fetched successfully', body: {
        resources: [
            {
                id: "89279395-9c59-4ede-9039-70cf15d8a959",
                title: "some resource",
                description: "",
                typeId: "pdf",
                languageCode: "NL",
                publicationDate: new Date("2025-05-29T10:32:11.319683Z"),
            }
    ]}
    }),
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

    test('Test if adding projects calls the correct function', async() => {
        render(<SidebarProvider leftSidebarDefaultState={true}><ListProjects 
            initialProjects={[]} 
            initialResources={[]} 
            fetchProjectAction={getProjectContentById}
            currentUserId=''
            userRole="admin"/></SidebarProvider>)
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
    });

    test('Test if deleting projects calls the correct function', async() => {
        const user = userEvent.setup();

        render(<SidebarProvider leftSidebarDefaultState={true}><ListProjects 
            initialProjects={
                [{  addedBy: "some dude",
                    folder: {
                        creationDate: "2025-05-29T10:32:11.319683Z",
                        deletionDate: "2025-05-29T10:32:11.319683Z",
                        description: null,
                        id: "89279395-9c59-4ede-9039-70cf15d8a958",
                        projectType: "root",
                        title: "test project" },
                    creatorRelations: []}]} 
            initialResources={[]} 
            fetchProjectAction={getProjectContentById}
            currentUserId=''
            userRole="admin"/>
            </SidebarProvider>)

        // DELETE PROJECT
        const delButton = await screen.findByTestId("delete-project-or-resource");
        user.click(delButton);
        await waitFor(() => expect(deleteProject).toHaveBeenCalledTimes(1));
    });

    test('Test if updating projects calls the correct function', async() => {
        const user = userEvent.setup();

        render(<SidebarProvider leftSidebarDefaultState={true}><ListProjects 
            initialProjects={
                [{  addedBy: "some dude",
                    folder: {
                        creationDate: "2025-05-29T10:32:11.319683Z",
                        deletionDate: "2025-05-29T10:32:11.319683Z",
                        description: null,
                        id: "89279395-9c59-4ede-9039-70cf15d8a958",
                        projectType: "root",
                        title: "test project" },
                    creatorRelations: []}]} 
            initialResources={[]} 
            fetchProjectAction={getProjectContentById}
            currentUserId=''
            userRole="admin"/>
            </SidebarProvider>)

        // CLICK BUTTON TO OPEN PROJECT UPDATER
        const editButton = await screen.findByTestId("edit-project-or-folder");
        user.click(editButton);

        // FILL IN ONLY TITLE TO JUST TEST CALL
        const titleInput = await screen.findByTestId("change-project-title");
        act(() => {
            fireEvent.change(titleInput, {target: {value:"test title"} });
        });

        // CLICK ON UPDATE
        const updateButton = await screen.findByTestId("project-submit-update");
        user.click(updateButton);

        await waitFor(() => expect(updateProject).toHaveBeenCalledTimes(1));
    });
    // This test does not work due to ag grid not really supporting tests

    // test('Test if linking / removing resources calls the correct functions', async() => {
    //     const user = userEvent.setup();

    //     render(<SidebarProvider leftSidebarDefaultState={true}><ListProjects 
    //         initialProjects={
    //             [{  addedBy: "some dude",
    //                 folder: {
    //                     creationDate: "2025-05-29T10:32:11.319683Z",
    //                     deletionDate: "2025-05-29T10:32:11.319683Z",
    //                     description: null,
    //                     id: "89279395-9c59-4ede-9039-70cf15d8a958",
    //                     projectType: "root",
    //                     title: "test project" }}]} 
    //         initialResources={[]} 
    //         fetchProjectAction={getProjectContentById}/>
    //         </SidebarProvider>)
        
    //     // CLICK ON THE ROW TO OPEN THE CONTENTS
    //     act(() => screen.getByText("test project").click());

    //     // NOW CLICK ON ADD RESOURCE
    //     const addButton = await screen.findByTestId("add");
    //     user.click(addButton);
    //     const addResourceButton = await screen.findByTestId("add-resource");
    //     user.click(addResourceButton);

    //     // ADD THE RESOURCE => CANNOT FETCH DIRECTLY => USE TAB + SPACE
    //     user.keyboard("{Tab}");
    //     user.keyboard("{Tab}");
    //     user.keyboard("{Space}");

    //     const confirmButton = await screen.findByTestId("confirm-add");
    //     user.click(confirmButton);
    //     await waitFor(() => expect(addResourceToProject).toHaveBeenCalledTimes(1));
    // })
})