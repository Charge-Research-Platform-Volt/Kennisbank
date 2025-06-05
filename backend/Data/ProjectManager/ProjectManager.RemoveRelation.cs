using KnowledgeBank.Models;

namespace KnowledgeBank.Data
{
    // This part is for removing relations
    public partial class ProjectManager
    {
        /// <summary>
        /// Removes a resource from the project or folder.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id to remove the resource from</param>
        /// <param name="resourceId">Resource id to remove from the project</param>
        /// <returns>Nothing, just updates the project</returns>
        public async Task<bool> RemoveResourceFromProject(Guid projectId, Guid resourceId)
        {
            await DeleteAsync(database.ProjectResourceRelations, relation => relation.ProjectId == projectId && relation.ResourceId == resourceId);
            return true;
        }

        /// <summary>
        /// Removes a resource from the project or folder.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id to remove the resource from</param>
        /// <param name="resourceId">Resource id to remove from the project</param>
        /// <returns>Nothing, just updates the project</returns>
        public async Task<bool> RemoveResourceFromProject(string projectId, string resourceId)
        {
            return await RemoveResourceFromProject(Guid.Parse(projectId), Guid.Parse(resourceId));
        }

        /// <summary>
        /// Removes all resources from the project or folder.
        /// THIS IS FOR SPECIFICALLY DELETING RESOURCES, NOT FOLDERS, AND THUS IT DOES NOT DO A CASCADING DELETE.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id to remove all resources from</param>
        /// <returns>Nothing, just updates the project</returns>
        public async Task<bool> RemoveAllResourcesFromProject(Guid projectId)
        {
            await DeleteAllWhereAsync(database.ProjectResourceRelations, relation => relation.ProjectId == projectId);
            return true;
        }

        /// <summary>
        /// Removes a folder from the project or folder.
        /// This deletes all resources and folders inside the folder associated with that id.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="folderId">Folder id to remove from the project or folder</param>
        /// <returns>Nothing, just updates the project</returns>
        public async Task<bool> RemoveFolderFromProject(Guid folderId)
        {
            Stack<Guid> foldersToRemove = new();
            foldersToRemove.Push(folderId);
            while (foldersToRemove.Count != 0)
            {
                Project? currentFolder = await GetProjectAsync(foldersToRemove.Pop());

                if (currentFolder != null)
                {
                    // Delete all resources in this folder
                    await RemoveAllResourcesFromProject(currentFolder.Id);

                    // Delete the folder itself
                    await DeleteAsync(database.Projects, project => project.Id == currentFolder.Id);

                    // Delete all references to this folder in the project folder relations
                    await DeleteAsync(database.ProjectFolderRelations, relation => relation.ChildId == currentFolder.Id);

                    // Get potential subfolders and add them to the remove list
                    List<Project?> newFolders = (await GetAllFolders(project => project.ParentId == currentFolder.Id)).Select(relation => relation.ChildFolder).ToList();

                    if (newFolders != null)
                    {
                        // Add new subfolders to the remove list
                        foreach (Project? folder in newFolders)
                        {
                            if (folder != null)
                            {
                                foldersToRemove.Push(folder.Id);
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Method for deleting all folders in a project. This is a cascading delete.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project to remove all folders from.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task<bool> RemoveAllFoldersFromProject(Guid projectId)
        {
            List<ProjectFolderRelation> rootProjects = (await GetAllFolders(project => project.ParentId == projectId)).ToList();
            await DeleteAsync(database.Projects, project => project.Id == projectId);
            foreach (ProjectFolderRelation p in rootProjects)
            {
                await RemoveFolderFromProject(p.ChildId);
            }

            return true;
        }

        /// <summary>
        /// Deletes a tag from a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Id of the project to remove the tag from.</param>
        /// <param name="tagId">Tag id of the tag to remove.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task<bool> RemoveTagFromProject(Guid projectId, Guid tagId)
        {
            await DeleteAsync(database.ProjectTagRelations, relation => relation.ProjectId == projectId && relation.TagId == tagId);

            return true;
        }

        /// <summary>
        /// Deletes all project-tag relations containing the tag.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="tagId">Id of the tag to delete</param>
        /// <returns>Boolean indicating whether or not the operation was successful</returns>
        public async Task<bool> RemoveTagFromAllProjects(Guid tagId)
        {
            await DeleteAllWhereAsync(database.ProjectTagRelations, relation => relation.TagId == tagId);

            return true;
        }

        /// <summary>
        /// Deletes all tags from a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Id of the project to remove the tags from.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task<bool> RemoveAllTagsFromProject(Guid projectId)
        {
            await DeleteAllWhereAsync(database.ProjectTagRelations, relation => relation.ProjectId == projectId);

            return true;
        }

        /// <summary>
        /// Deletes a creator from a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Id of the project to remove the creator from.</param>
        /// <param name="creatorId">Creator id of the creator to remove.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task<bool> RemoveCreatorFromProject(Guid projectId, string creatorId)
        {
            await DeleteAsync(database.ProjectCreatorRelations, relation => relation.ProjectId == projectId && relation.CreatorId == creatorId);
            return true;
        }

        /// <summary>
        /// Deletes all creators from a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Id of the project to remove all creators from.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task<bool> RemoveAllCreatorsFromProject(Guid projectId)
        {
            Queue<Guid> projectsToUpdate = new Queue<Guid>();
            projectsToUpdate.Enqueue(projectId);

            while (projectsToUpdate.Count > 0)
            {
                Guid id = projectsToUpdate.Dequeue();
                (await GetAllFolders(predicate: p => p.ParentId == id)).Select(r => r.ChildId).ToList().ForEach(projectsToUpdate.Enqueue);
                await DeleteAllWhereAsync(database.ProjectCreatorRelations, relation => relation.ProjectId == id);
            }
            return true;
        }


    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)