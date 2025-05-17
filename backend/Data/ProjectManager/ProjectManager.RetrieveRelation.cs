using KnowledgeBank.Models;
using Serilog;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        private readonly Expression<Func<ProjectFolderRelation, object>> projectFolderRelationDefaultOrderBy = relation => relation.ParentId;
        private const bool projectFolderRelationDefaultOrderDescending = true;
        public async Task<ProjectFolderRelation[]> GetAllFolders(Expression<Func<ProjectFolderRelation, object>>? orderBy = null, bool orderDescending = projectFolderRelationDefaultOrderDescending, Expression<Func<ProjectFolderRelation, bool>>? predicate = null)
        {
            orderBy ??= projectFolderRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectFolderRelations, orderBy, orderDescending, predicate);
        }

        private readonly Expression<Func<ProjectTagRelation, object>> projectTagRelationDefaultOrderBy = relation => relation.Tag.Name;
        private const bool projectTagRelationDefaultOrderDescending = true;

        public async Task<ProjectTagRelation[]> GetAllTags(Expression<Func<ProjectTagRelation, object>>? orderBy = null, bool orderDescending = projectTagRelationDefaultOrderDescending, Expression<Func<ProjectTagRelation, bool>>? predicate = null)
        {
            orderBy ??= projectTagRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectTagRelations, orderBy, orderDescending, predicate);
        }

        private readonly Expression<Func<ProjectCreatorRelation, object>> projectCreatorRelationDefaultOrderBy = relation => relation.CreatorId;
        private const bool projectCreatorRelationDefaultOrderDescending = true;

        public async Task<ProjectCreatorRelation[]> GetAllCreators(Expression<Func<ProjectCreatorRelation, object>>? orderBy = null, bool orderDescending = projectCreatorRelationDefaultOrderDescending, Expression<Func<ProjectCreatorRelation, bool>>? predicate = null)
        {
            orderBy ??= projectCreatorRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectCreatorRelations, orderBy, orderDescending, predicate);
        }

        private readonly Expression<Func<ProjectResourceRelation, object>> projectResourceRelationDefaultOrderBy = relation => relation.ResourceId;
        private const bool projectResourceRelationDefaultOrderDescending = true;

        public async Task<ProjectResourceRelation[]> GetAllResources(Expression<Func<ProjectResourceRelation, object>>? orderBy = null, bool orderDescending = projectResourceRelationDefaultOrderDescending, Expression<Func<ProjectResourceRelation, bool>>? predicate = null)
        {
            orderBy ??= projectResourceRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectResourceRelations, orderBy, orderDescending, predicate);
        }

    }
}