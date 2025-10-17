using KnowledgeBank.Models;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        #region Project-tag

        // Range

        /// <summary>
        /// Adds a range of tags to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tags to.</param>
        /// <param name="tagIds">Ids of the tags to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectRangeAsync(Guid projectId, Guid[] tagIds)
        {
            if (tagIds.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            ProjectTagRelation[] tagRelations = new ProjectTagRelation[tagIds.Length];

            for (int i = 0; i < tagIds.Length; i++)
            {
                tagRelations[i] = new()
                {
                    ProjectId = projectId,
                    TagId = tagIds[i]
                };
            }

            await database.ProjectTagRelations.AddRangeAsync(tagRelations);

            if (startedTransaction) await Commit();
        }

        /// <summary>
        /// Adds a range of tags to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tags to.</param>
        /// <param name="tagIds">Ids of the tags to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectRangeAsync(Guid projectId, string[] tagIds)
        { await AddTagToProjectRangeAsync(projectId, StringToGuidArray(tagIds)); }

        /// <summary>
        /// Adds a range of tags to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tags to.</param>
        /// <param name="tagIds">Ids of the tags to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectRangeAsync(string projectId, Guid[] tagIds)
        { await AddTagToProjectRangeAsync(Guid.Parse(projectId), tagIds); }

        /// <summary>
        /// Adds a range of tags to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tags to.</param>
        /// <param name="tagIds">Ids of the tags to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectRangeAsync(string projectId, string[] tagIds)
        { await AddTagToProjectRangeAsync(Guid.Parse(projectId), StringToGuidArray(tagIds)); }

        // Single

        /// <summary>
        /// Adds a single tag to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tag to.</param>
        /// <param name="tagId">Id of the tag to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectAsync(Guid projectId, Guid tagId)
        { await AddTagToProjectRangeAsync(projectId, [tagId]); }

        /// <summary>
        /// Adds a single tag to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tag to.</param>
        /// <param name="tagId">Id of the tag to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectAsync(string projectId, Guid tagId)
        { await AddTagToProjectAsync(Guid.Parse(projectId), tagId); }

        /// <summary>
        /// Adds a single tag to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tag to.</param>
        /// <param name="tagId">Id of the tag to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectAsync(Guid projectId, string tagId)
        { await AddTagToProjectAsync(projectId, Guid.Parse(tagId)); }

        /// <summary>
        /// Adds a single tag to the project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the tag to.</param>
        /// <param name="tagId">Id of the tag to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddTagToProjectAsync(string projectId, string tagId)
        { await AddTagToProjectAsync(Guid.Parse(projectId), Guid.Parse(tagId)); }

        #endregion

        #region project-creator

        // Range

        /// <summary>
        /// Adds multiple creators to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creators to.</param>
        /// <param name="userIds">Ids of the creators to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectRangeAsync(Guid projectId, Guid[] userIds)
        {
            // Is cascading
            if (userIds.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            Queue<Guid> projectsToUpdate = new Queue<Guid>();
            projectsToUpdate.Enqueue(projectId);

            while (projectsToUpdate.Count > 0)
            {
                ProjectCreatorRelation[] creatorRelations = new ProjectCreatorRelation[userIds.Length];
                Guid id = projectsToUpdate.Dequeue();

                for (int i = 0; i < userIds.Length; i++)
                {
                    creatorRelations[i] = new()
                    {
                        ProjectId = id,
                        CreatorId = userIds[i].ToString()
                    };
                }
                (await GetAllFolders(predicate: p => p.ParentId == id)).Select(r => r.ChildId).ToList().ForEach(projectsToUpdate.Enqueue);
                await database.ProjectCreatorRelations.AddRangeAsync(creatorRelations);
            }

            if (startedTransaction) await Commit();
        }

        /// <summary>
        /// Adds multiple creators to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creators to.</param>
        /// <param name="userIds">Ids of the creators to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectRangeAsync(Guid projectId, string[] userIds)
        { await AddCreatorToProjectRangeAsync(projectId, StringToGuidArray(userIds)); }

        /// <summary>
        /// Adds multiple creators to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creators to.</param>
        /// <param name="userIds">Ids of the creators to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectRangeAsync(string projectId, Guid[] userIds)
        { await AddCreatorToProjectRangeAsync(Guid.Parse(projectId), userIds); }

        /// <summary>
        /// Adds multiple creators to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creators to.</param>
        /// <param name="userIds">Ids of the creators to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectRangeAsync(string projectId, string[] userIds)
        { await AddCreatorToProjectRangeAsync(Guid.Parse(projectId), StringToGuidArray(userIds)); }

        // Single

        /// <summary>
        /// Adds a creator to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creator to.</param>
        /// <param name="userId">Id of the creator to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectAsync(Guid projectId, Guid userId)
        { await AddCreatorToProjectRangeAsync(projectId, [userId]); }

        /// <summary>
        /// Adds a creator to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creator to.</param>
        /// <param name="userId">Id of the creator to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectAsync(string projectId, Guid userId)
        { await AddCreatorToProjectAsync(Guid.Parse(projectId), userId); }

        /// <summary>
        /// Adds a creator to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creator to.</param>
        /// <param name="userId">Id of the creator to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectAsync(Guid projectId, string userId)
        { await AddCreatorToProjectAsync(projectId, Guid.Parse(userId)); }

        /// <summary>
        /// Adds a creator to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the creator to.</param>
        /// <param name="userId">Id of the creator to add to the project.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddCreatorToProjectAsync(string projectId, string userId)
        { await AddCreatorToProjectAsync(Guid.Parse(projectId), Guid.Parse(userId)); }


        #endregion

        #region project-folder
        // Users only add 1 folder at a time, so no need to ever add a range of them

        /// <summary>
        /// Adds a folder to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the folder to.</param>
        /// <param name="folderId">Id of the folder to add to the project.</param>
        /// <param name="addedBy">Id of the user that has created this folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddFolderToProjectAsync(Guid projectId, Guid folderId, Guid addedBy)
        {
            if (string.IsNullOrEmpty(folderId.ToString())) return;

            bool startedTransaction = await BeginTransaction();

            ProjectFolderRelation folderRelation = new()
            {
                ParentId = projectId,
                ChildId = folderId,
                AddedBy = addedBy.ToString()
            };

            await database.ProjectFolderRelations.AddAsync(folderRelation);

            if (startedTransaction) await Commit();
        }

        /// <summary>
        /// Adds a folder to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the folder to.</param>
        /// <param name="folderId">Id of the folder to add to the project.</param>
        /// <param name="addedBy">Id of the user that has created this folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddFolderToProjectAsync(string projectId, Guid folderId, Guid addedBy)
        { await AddFolderToProjectAsync(Guid.Parse(projectId), folderId, addedBy); }

        /// <summary>
        /// Adds a folder to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the folder to.</param>
        /// <param name="folderId">Id of the folder to add to the project.</param>
        /// <param name="addedBy">Id of the user that has created this folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddFolderToProjectAsync(Guid projectId, string folderId, Guid addedBy)
        { await AddFolderToProjectAsync(projectId, Guid.Parse(folderId), addedBy); }

        /// <summary>
        /// Adds a folder to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id of the project to add the folder to.</param>
        /// <param name="folderId">Id of the folder to add to the project.</param>
        /// <param name="addedBy">Id of the user that has created this folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddFolderToProjectAsync(string projectId, string folderId, Guid addedBy)
        { await AddFolderToProjectAsync(Guid.Parse(projectId), Guid.Parse(folderId), addedBy); }

        #endregion

        #region project-resource

        // Range

        /// <summary>
        /// Adds resources to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resources to.</param>
        /// <param name="resourceIds">Ids of the resources to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resources to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectRangeAsync(Guid projectId, Guid[] resourceIds, Guid addedBy)
        {
            if (resourceIds.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            ProjectResourceRelation[] resourceRelations = new ProjectResourceRelation[resourceIds.Length];

            for (int i = 0; i < resourceIds.Length; i++)
            {
                resourceRelations[i] = new()
                {
                    ProjectId = projectId,
                    ResourceId = resourceIds[i],
                    AddedBy = addedBy.ToString()
                };
            }

            await database.ProjectResourceRelations.AddRangeAsync(resourceRelations);

            if (startedTransaction) await Commit();
        }

        /// <summary>
        /// Adds resources to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resources to.</param>
        /// <param name="resourceIds">Ids of the resources to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resources to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectRangeAsync(Guid projectId, string[] resourceIds, Guid addedBy)
        { await AddResourceToProjectRangeAsync(projectId, StringToGuidArray(resourceIds), addedBy); }

        /// <summary>
        /// Adds resources to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resources to.</param>
        /// <param name="resourceIds">Ids of the resources to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resources to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectRangeAsync(string projectId, Guid[] resourceIds, Guid addedBy)
        { await AddResourceToProjectRangeAsync(Guid.Parse(projectId), resourceIds, addedBy); }

        /// <summary>
        /// Adds resources to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resources to.</param>
        /// <param name="resourceIds">Ids of the resources to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resources to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectRangeAsync(string projectId, string[] resourceIds, Guid addedBy)
        { await AddResourceToProjectRangeAsync(Guid.Parse(projectId), StringToGuidArray(resourceIds), addedBy); }

        // Single

        /// <summary>
        /// Adds a resource to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resource to.</param>
        /// <param name="resourceId">Id of the resource to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resource to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectAsync(Guid projectId, Guid resourceId, Guid addedBy)
        { await AddResourceToProjectRangeAsync(projectId, [resourceId], addedBy); }

        /// <summary>
        /// Adds a resource to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resource to.</param>
        /// <param name="resourceId">Id of the resource to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resource to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectAsync(string projectId, Guid resourceId, Guid addedBy)
        { await AddResourceToProjectAsync(Guid.Parse(projectId), resourceId, addedBy); }

        /// <summary>
        /// Adds a resource to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resource to.</param>
        /// <param name="resourceId">Id of the resource to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resource to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectAsync(Guid projectId, string resourceId, Guid addedBy)
        { await AddResourceToProjectAsync(projectId, Guid.Parse(resourceId), addedBy); }

        /// <summary>
        /// Adds a resource to a project.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project / folder id of the project to add the resource to.</param>
        /// <param name="resourceId">Id of the resource to add to the project.</param>
        /// <param name="addedBy">Id of the user that has added the resource to this project / folder.</param>
        /// <returns>Nothing, just updates the database.</returns>
        public async Task AddResourceToProjectAsync(string projectId, string resourceId, Guid addedBy)
        { await AddResourceToProjectAsync(Guid.Parse(projectId), Guid.Parse(resourceId), addedBy); }

        #endregion
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)