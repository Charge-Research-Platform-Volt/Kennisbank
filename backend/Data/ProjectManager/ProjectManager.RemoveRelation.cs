using KnowledgeBank.Models;
using Serilog;

namespace KnowledgeBank.Data
{
    // This part is for removing relations
    public partial class ProjectManager
    {
        public async Task<bool> RemoveResourceFromProject(Guid projectId, Guid resourceId)
        {
            await DeleteAsync(database.ProjectResourceRelations, relation => relation.ProjectId == projectId && relation.ResourceId == resourceId);
            return true;
        }

        public async Task<bool> RemoveResourceFromProject(string projectId, string resourceId)
        {
            return await RemoveResourceFromProject(Guid.Parse(projectId), Guid.Parse(resourceId));
        }

        // THIS IS FOR SPECIFICALLY DELETING RESOURCES, NOT FOLDERS, AND THUS IT DOESN'T DO A CASCADING DELETE
        public async Task<bool> RemoveAllResourcesFromProject(Guid projectId)
        {
            await DeleteAllWhereAsync(database.ProjectResourceRelations, relation => relation.ProjectId == projectId);
            return true;
        }

        // This deletes all resources and folders inside the folder associated with that id
        public async Task<bool> RemoveFolderFromProject(Guid folderId)
        {
            Stack<Guid> foldersToRemove = new();
            foldersToRemove.Push(folderId);
            while(foldersToRemove.Count != 0)
            {
                Project curFolder = await GetProjectAsync(foldersToRemove.Pop());

                await RemoveAllResourcesFromProject(curFolder.Id); // delete all resources in this folder
                await DeleteAsync(database.Projects, project => project.Id == curFolder.Id); // delete yourself from the projects folder
                await DeleteAsync(database.ProjectFolderRelations, relation => relation.ChildId == curFolder.Id); // delete references to yourself from root
                List<Project> newFolders = (await GetAllFolders(project => project.ParentId == curFolder.Id)).Select(relation => relation.ChildFolder).ToList(); //get potential subfolders
                foreach (Project folder in newFolders) // Add new subfolders to the remove list
                {
                    foldersToRemove.Push(folder.Id);
                }
            }

            return true;
        }

        // This is a cascading delete
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

        public async Task<bool> RemoveTagFromProject(Guid projectId, Guid tagId)
        {
            await DeleteAsync(database.ProjectTagRelations, relation => relation.ProjectId == projectId && relation.TagId == tagId);

            return true;
        }

        public async Task<bool> RemoveAllTagsFromProject(Guid projectId)
        {
            await DeleteAllWhereAsync(database.ProjectTagRelations, relation => relation.ProjectId == projectId);

            return true;
        }

        public async Task<bool> RemoveCreatorFromProject(Guid projectId, string creatorId)
        {
            await DeleteAsync(database.ProjectCreatorRelations, relation => relation.ProjectId == projectId && relation.CreatorId == creatorId);
            return true;
        }

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