using KnowledgeBank.Models;
using Serilog;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        private readonly Expression<Func<ProjectFolderRelation, object>> projectFolderRelationDefaultOrderBy = relation => relation.ParentId;
        private const bool projectFolderRelationDefaultOrderDescending = true;

        /// <summary>
        /// Gets all folders given a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="orderBy">What to oder folders by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="predicate">Predicate used to filter all project-folder relations.</param>
        /// <returns>All project-folder relations satisfying the predicate.</returns>
        public async Task<ProjectFolderRelation[]> GetAllFolders(Expression<Func<ProjectFolderRelation, object>>? orderBy = null, bool orderDescending = projectFolderRelationDefaultOrderDescending, Expression<Func<ProjectFolderRelation, bool>>? predicate = null)
        {
            orderBy ??= projectFolderRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectFolderRelations, orderBy, orderDescending, predicate);
        }

        private readonly Expression<Func<ProjectTagRelation, object>> projectTagRelationDefaultOrderBy = relation => relation.Tag.Name;
        private const bool projectTagRelationDefaultOrderDescending = true;

        /// <summary>
        /// Gets all tags of a project given a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="orderBy">What to oder tags by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="predicate">Predicate used to filter all project-tag relations.</param>
        /// <returns>All project-tag relations satisfying the predicate.</returns>
        public async Task<ProjectTagRelation[]> GetAllTags(Expression<Func<ProjectTagRelation, object>>? orderBy = null, bool orderDescending = projectTagRelationDefaultOrderDescending, Expression<Func<ProjectTagRelation, bool>>? predicate = null)
        {
            orderBy ??= projectTagRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectTagRelations, orderBy, orderDescending, predicate, includeProperties: "Project");
        }

        private readonly Expression<Func<ProjectCreatorRelation, object>> projectCreatorRelationDefaultOrderBy = relation => relation.CreatorId;
        private const bool projectCreatorRelationDefaultOrderDescending = true;

        /// <summary>
        /// Gets all creators of a project given a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="orderBy">What to oder creators by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="predicate">Predicate used to filter all project-creator relations.</param>
        /// <returns>All project-creator relations satisfying the predicate.</returns>
        public async Task<ProjectCreatorRelation[]> GetAllCreators(Expression<Func<ProjectCreatorRelation, object>>? orderBy = null, bool orderDescending = projectCreatorRelationDefaultOrderDescending, Expression<Func<ProjectCreatorRelation, bool>>? predicate = null)
        {
            orderBy ??= projectCreatorRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectCreatorRelations, orderBy, orderDescending, predicate);
        }

        private readonly Expression<Func<ProjectResourceRelation, object>> projectResourceRelationDefaultOrderBy = relation => relation.ResourceId;
        private const bool projectResourceRelationDefaultOrderDescending = true;

        /// <summary>
        /// Gets all resources of a project given a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="orderBy">What to oder resources by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="predicate">Predicate used to filter all project-resource relations.</param>
        /// <returns>All project-resource relations satisfying the predicate.</returns>
        public async Task<ProjectResourceRelation[]> GetAllResources(Expression<Func<ProjectResourceRelation, object>>? orderBy = null, bool orderDescending = projectResourceRelationDefaultOrderDescending, Expression<Func<ProjectResourceRelation, bool>>? predicate = null)
        {
            orderBy ??= projectResourceRelationDefaultOrderBy;

            return await GetAllAsync(database.ProjectResourceRelations, orderBy, orderDescending, predicate);
        }

    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)