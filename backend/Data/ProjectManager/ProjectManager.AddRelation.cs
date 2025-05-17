using KnowledgeBank.Models;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        #region Project-tag

        // Range
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

        public async Task AddTagToProjectRangeAsync(Guid projectId, string[] tagIds)
        { await AddTagToProjectRangeAsync(projectId, StringToGuidArray(tagIds)); }

        public async Task AddTagToProjectRangeAsync(string projectId, Guid[] tagIds)
        { await AddTagToProjectRangeAsync(Guid.Parse(projectId), tagIds); }

        public async Task AddTagToProjectRangeAsync(string projectId, string[] tagIds)
        { await AddTagToProjectRangeAsync(Guid.Parse(projectId), StringToGuidArray(tagIds)); }

        // Single
        public async Task AddTagToProjectAsync(Guid projectId, Guid tagId)
        { await AddTagToProjectRangeAsync(projectId, [tagId]); }

        public async Task AddTagToProjectAsync(string projectId, Guid tagId)
        { await AddTagToProjectAsync(Guid.Parse(projectId), tagId); }

        public async Task AddTagToProjectAsync(Guid projectId, string tagId)
        { await AddTagToProjectAsync(projectId, Guid.Parse(tagId)); }

        public async Task AddTagToProjectAsync(string projectId, string tagId)
        { await AddTagToProjectAsync(Guid.Parse(projectId), Guid.Parse(tagId)); }

        #endregion

        #region project-creator

        // Range
        public async Task AddCreatorToProjectRangeAsync(Guid projectId, Guid[] userIds)
        {
            if (userIds.Length == 0) return;

            bool startedTransaction = await BeginTransaction();

            ProjectCreatorRelation[] creatorRelations = new ProjectCreatorRelation[userIds.Length];

            for (int i = 0; i < userIds.Length; i++)
            {
                creatorRelations[i] = new()
                {
                    ProjectId = projectId,
                    CreatorId = userIds[i].ToString()
                };
            }

            await database.ProjectCreatorRelations.AddRangeAsync(creatorRelations);

            if (startedTransaction) await Commit();
        }

        public async Task AddCreatorToProjectRangeAsync(Guid projectId, string[] userIds)
        { await AddCreatorToProjectRangeAsync(projectId, StringToGuidArray(userIds)); }

        public async Task AddCreatorToProjectRangeAsync(string projectId, Guid[] userIds)
        { await AddCreatorToProjectRangeAsync(Guid.Parse(projectId), userIds); }

        public async Task AddCreatorToProjectRangeAsync(string projectId, string[] userIds)
        { await AddCreatorToProjectRangeAsync(Guid.Parse(projectId), StringToGuidArray(userIds)); }

        // Single
        public async Task AddCreatorToProjectAsync(Guid projectId, Guid userId)
        { await AddCreatorToProjectRangeAsync(projectId, [userId]); }

        public async Task AddCreatorToProjectAsync(string projectId, Guid userId)
        { await AddCreatorToProjectAsync(Guid.Parse(projectId), userId); }

        public async Task AddCreatorToProjectAsync(Guid projectId, string userId)
        { await AddCreatorToProjectAsync(projectId, Guid.Parse(userId)); }

        public async Task AddCreatorToProjectAsync(string projectId, string userId)
        { await AddCreatorToProjectAsync(Guid.Parse(projectId), Guid.Parse(userId)); }


        #endregion

        #region project-folder
        // Users only add 1 folder at a time, so no need to ever add a range of them
        public async Task AddFolderToProjectAsync(Guid projectId, Guid folderId)
        {
            if (string.IsNullOrEmpty(folderId.ToString())) return;

            bool startedTransaction = await BeginTransaction();

            ProjectFolderRelation folderRelation = new()
            {
                ParentId = projectId,
                ChildId = folderId
            };

            await database.ProjectFolderRelations.AddAsync(folderRelation);

            if (startedTransaction) await Commit();
        }
        public async Task AddFolderToProjectAsync(string projectId, Guid folderId)
        { await AddFolderToProjectAsync(Guid.Parse(projectId), folderId); }

        public async Task AddFolderToProjectAsync(Guid projectId, string folderId)
        { await AddFolderToProjectAsync(projectId, Guid.Parse(folderId)); }

        public async Task AddFolderToProjectAsync(string projectId, string folderId)
        { await AddFolderToProjectAsync(Guid.Parse(projectId), Guid.Parse(folderId)); }

        #endregion

        #region project-resource

        // Range
        public async Task AddResourceToProjectRangeAsync(Guid projectId, Guid[] resourceIds)
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
                };
            }

            await database.ProjectResourceRelations.AddRangeAsync(resourceRelations);

            if (startedTransaction) await Commit();
        }

        public async Task AddResourceToProjectRangeAsync(Guid projectId, string[] resourceIds)
        { await AddResourceToProjectRangeAsync(projectId, StringToGuidArray(resourceIds)); }

        public async Task AddResourceToProjectRangeAsync(string projectId, Guid[] resourceIds)
        { await AddResourceToProjectRangeAsync(Guid.Parse(projectId), resourceIds); }

        public async Task AddResourceToProjectRangeAsync(string projectId, string[] resourceIds)
        { await AddResourceToProjectRangeAsync(Guid.Parse(projectId), StringToGuidArray(resourceIds)); }

        // Single
        public async Task AddResourceToProjectAsync(Guid projectId, Guid resourceId)
        { await AddResourceToProjectRangeAsync(projectId, [resourceId]); }

        public async Task AddResourceToProjectAsync(string projectId, Guid resourceId)
        { await AddResourceToProjectAsync(Guid.Parse(projectId), resourceId); }

        public async Task AddResourceToProjectAsync(Guid projectId, string resourceId)
        { await AddResourceToProjectAsync(projectId, Guid.Parse(resourceId)); }

        public async Task AddResourceToProjectAsync(string projectId, string resourceId)
        { await AddResourceToProjectAsync(Guid.Parse(projectId), Guid.Parse(resourceId)); }

        #endregion
    }
}