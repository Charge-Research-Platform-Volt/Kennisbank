using KnowledgeBank.Models;

namespace KnowledgeBank.Data
{
    // This part is for removing relations
    public partial class ProjectManager
    {
        /// <summary>
        /// Removes a single item (resource, person, or organisation) from a project or folder.
        /// </summary>
        public async Task<bool> RemoveItemFromProject(Guid projectId, Guid itemId)
        {
            await DeleteAsync(database.ProjectItemRelations, relation => relation.ProjectId == projectId && relation.ItemId == itemId);
            return true;
        }

        /// <summary>
        /// Removes a single item (resource, person, or organisation) from a project or folder.
        /// </summary>
        public async Task<bool> RemoveItemFromProject(string projectId, string itemId)
            => await RemoveItemFromProject(Guid.Parse(projectId), Guid.Parse(itemId));

        /// <summary>
        /// Removes all items from a project or folder.
        /// </summary>
        public async Task<bool> RemoveAllItemsFromProject(Guid projectId)
        {
            await DeleteAllWhereAsync(database.ProjectItemRelations, relation => relation.ProjectId == projectId);
            return true;
        }

        /// <summary>
        /// Removes a folder from the project or folder.
        /// This deletes all resources and folders inside the folder associated with that id.
        ///
        /// </summary>
        /// <param name="folderId">Folder id to remove from the project or folder</param>
        /// <returns>Boolean indicating whether or not deletion was successful.</returns>
        public async Task<bool> RemoveFolderFromProject(Guid folderId)
        {
            Stack<Guid> foldersToRemove = new();
            foldersToRemove.Push(folderId);
            while (foldersToRemove.Count != 0)
            {
                Project? currentFolder = await GetProjectAsync(foldersToRemove.Pop());

                if (currentFolder != null)
                {
                    // Get subfolders BEFORE deleting, as deletion cascades project-folder relations
                    List<Guid> subfolderIds = (await GetAllFolders(project => project.ParentId == currentFolder.Id))
                        .Select(relation => relation.ChildId)
                        .ToList();

                    foreach (Guid subfolderId in subfolderIds)
                        foldersToRemove.Push(subfolderId);

                    // Delete all items in this folder
                    await RemoveAllItemsFromProject(currentFolder.Id);

                    // Delete all references to this folder as a child
                    await DeleteAsync(database.ProjectFolderRelations, relation => relation.ChildId == currentFolder.Id);

                    // Delete the folder itself (cascades project-folder relations where ParentId == currentFolder.Id)
                    await DeleteAsync(database.Projects, project => project.Id == currentFolder.Id);
                }
            }

            return true;
        }

        /// <summary>
        /// Method for deleting all folders in a project. This is a cascading delete.
        /// 
        /// </summary>
        /// <param name="projectId">Project to remove all folders from.</param>
        /// <returns>Boolean indicating whether or not deletion was successful.</returns>
        public async Task<bool> RemoveAllFoldersFromProject(Guid projectId)
        {
            List<ProjectFolderRelation> rootProjects = (await GetAllFolders(predicate: project => project.ParentId == projectId)).ToList();
            await DeleteAsync(database.Projects, project => project.Id == projectId);
            foreach (ProjectFolderRelation p in rootProjects)
                await RemoveFolderFromProject(p.ChildId);

            return true;
        }

        /// <summary>
        /// Deletes a tag from a project.
        /// 
        /// </summary>
        /// <param name="projectId">Id of the project to remove the tag from.</param>
        /// <param name="tagId">Tag id of the tag to remove.</param>
        /// <returns>Boolean indicating whether or not deletion was successful.</returns>
        public async Task<bool> RemoveTagFromProject(Guid projectId, Guid tagId)
        {
            await DeleteAsync(database.ProjectTagRelations, relation => relation.ProjectId == projectId && relation.TagId == tagId);

            return true;
        }

        /// <summary>
        /// Deletes all project-tag relations containing the tag.
        /// 
        /// </summary>
        /// <param name="tagId">Id of the tag to delete</param>
        /// <returns>Boolean indicating whether or not the deletion was successful</returns>
        public async Task<bool> RemoveTagFromAllProjects(Guid tagId)
        {
            await DeleteAllWhereAsync(database.ProjectTagRelations, relation => relation.TagId == tagId);

            return true;
        }

        /// <summary>
        /// Deletes all tags from a project.
        /// 
        /// </summary>
        /// <param name="projectId">Id of the project to remove the tags from.</param>
        /// <returns>Boolean indicating whether or not deletion was successful.</returns>
        public async Task<bool> RemoveAllTagsFromProject(Guid projectId)
        {
            await DeleteAllWhereAsync(database.ProjectTagRelations, relation => relation.ProjectId == projectId);

            return true;
        }

        /// <summary>
        /// Deletes a creator from a project.
        /// 
        /// </summary>
        /// <param name="projectId">Id of the project to remove the creator from.</param>
        /// <param name="creatorId">Creator id of the creator to remove.</param>
        /// <returns>Boolean indicating whether or not deletion was successful.</returns>
        public async Task<bool> RemoveCreatorFromProject(Guid projectId, string creatorId)
        {
            await DeleteAsync(database.ProjectCreatorRelations, relation => relation.ProjectId == projectId && relation.CreatorId == creatorId);
            return true;
        }

        /// <summary>
        /// Deletes all creators from a project.
        /// 
        /// </summary>
        /// <param name="projectId">Id of the project to remove all creators from.</param>
        /// <returns>Boolean indicating whether or not deletion was successful.</returns>
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
