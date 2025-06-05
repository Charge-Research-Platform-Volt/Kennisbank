using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    // This part is for information about projects
    public partial class ProjectManager
    {

        /// <summary>
        /// Checks if something exists in the table using a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of the thing to check.</typeparam>
        /// <param name="dbSet">Table to check in.</param>
        /// <param name="predicate">Predicate used to check if some entity exists.</param>
        /// <returns>Nothing, just checks the database.</returns>
        protected async Task<bool> ExistsAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.AsNoTracking().AnyAsync(predicate); }

        /// <summary>
        /// Gets the count of an entity given a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of the thing to count.</typeparam>
        /// <param name="dbSet">Table to count in.</param>
        /// <param name="predicate">Predicate used to check the count of some entity.</param>
        /// <returns>Nothing, just checks the database.</returns>
        protected async Task<int> GetCount<T>(DbSet<T> dbSet, Expression<Func<T, bool>>? predicate = null) where T : class
        {
            IQueryable<T> query = dbSet.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);
            return await query.CountAsync();
        }

        /// <summary>
        /// Checks if a project exists using an id.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id in question.</param>
        /// <returns>Nothing, just checks the database.</returns>
        public async Task<bool> ProjectExistsAsync(Guid projectId)
        { return await ExistsAsync(database.Projects, project => project.Id == projectId); }

        /// <summary>
        /// Checks if a project exists using an id.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projectId">Project id in question.</param>
        /// <returns>Nothing, just checks the database.</returns>
        public async Task<bool> ProjectExistsAsync(string projectId)
        { return await ProjectExistsAsync(Guid.Parse(projectId)); }

        /// <summary>
        /// Checks if a project exists using a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="predicate">Predicate in question.</param>
        /// <returns>Nothing, just checks the database.</returns>
        public async Task<bool> ProjectExistsAsync(Expression<Func<Project, bool>> predicate)
        { return await ExistsAsync(database.Projects, predicate); }

        // Count

        /// <summary>
        /// Checks the count of projects using a predicate.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="predicate">Predicate used to calculate the count.</param>
        /// <returns>Nothing, just checks the database.</returns>
        public async Task<int> ProjectCount(Expression<Func<Project, bool>>? predicate = null)
        { return await GetCount(database.Projects, predicate); }

        #region project-tags
        /// <summary>
        /// Checks if some project-tag relation exists given a predicate
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="predicate">Predicate to filter on</param>
        /// <returns>Boolean indicating whether or not some tag relation exists or not</returns>
        public async Task<bool> ProjectTagRelationExistsAsync(Expression<Func<ProjectTagRelation, bool>> predicate)
        { return await ExistsAsync(database.ProjectTagRelations, predicate); }

        #endregion
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)