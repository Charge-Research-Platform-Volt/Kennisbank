using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {

        #region Generic functions
        /// <summary>
        /// Updates property of a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of entity to be updated.</typeparam>
        /// <typeparam name="TProperty">Type of property to be updated</typeparam>
        /// <param name="dbSet">Table to update in.</param>
        /// <param name="predicate">Predicate to select project with</param>
        /// <param name="propertySelector">Property selector used to update.</param>
        /// <param name="newValue">New value of the property to be updated.</param>
        /// <returns>Number of rows updated in database.</returns>
        protected async Task<int> UpdateProjectAsync<T, TProperty>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Expression<Func<T, TProperty>> propertySelector, TProperty newValue) where T : class
        {
            bool startedTransaction = await BeginTransaction();
            int count = await dbSet.Where(predicate).ExecuteUpdateAsync(s => s.SetProperty(e => EF.Property<TProperty>(e, GetPropertyName(propertySelector)), _ => newValue));
            if (startedTransaction) await Commit();
            return count;
        }

        /// <summary>
        /// Gets property name.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of entity to be updated</typeparam>
        /// <typeparam name="TProperty">Type of property to be updated</typeparam>
        /// <param name="propertyExpression"></param>
        /// <returns>Name of property to be updated.</returns>
        /// <exception cref="ArgumentException"></exception>
        private string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a property access expression.", nameof(propertyExpression));
        }

        #endregion

        #region Projects / folders
        // Used to alter properties of projects / folders themselves

        /// <summary>
        /// Updates a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Value type to update</typeparam>
        /// <param name="id">Id of project to update</param>
        /// <param name="propertySelector">Selector of property to update</param>
        /// <param name="newValue">New value of the property</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectAsync<T>(Guid id, Expression<Func<Project, T>> propertySelector, T newValue)
        { return await UpdateProjectAsync(database.Projects, resource => resource.Id == id, propertySelector, newValue) > 0; }

        /// <summary>
        /// Updates a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Value type to update</typeparam>
        /// <param name="id">Id of project to update</param>
        /// <param name="propertySelector">Selector of property to update</param>
        /// <param name="newValue">New value of the property</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectAsync<T>(string id, Expression<Func<Project, T>> propertySelector, T newValue)
        { return await UpdateProjectAsync(Guid.Parse(id), propertySelector, newValue); }

        /// <summary>
        /// Updates a project.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Value type to update</typeparam>
        /// <param name="predicate">Predicate to select project to update</param>
        /// <param name="propertySelector">Selector of property to update</param>
        /// <param name="newValue">New value of the property</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectAsync<T>(Expression<Func<Project, bool>> predicate, Expression<Func<Project, T>> propertySelector, T newValue)
        { return await UpdateProjectAsync(database.Projects, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Tags

        /// <summary>
        /// Updates tags of project, to do this we remove all previous tags.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter tags of</param>
        /// <param name="newValue">Ids of new tags</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectTagsAsync(Guid id, Guid[] newValue)
        {
            Guid[] existing = await database.ProjectTagRelations
                .Where(r => r.ProjectId == id)
                .Select(r => r.TagId)
                .ToArrayAsync();

            Guid[] toRemove = existing.Except(newValue).ToArray();
            Guid[] toAdd = newValue.Except(existing).ToArray();

            if (toRemove.Length > 0)
                await database.ProjectTagRelations
                    .Where(r => r.ProjectId == id && toRemove.Contains(r.TagId))
                    .ExecuteDeleteAsync();

            if (toAdd.Length > 0)
                await AddTagToProjectRangeAsync(id, toAdd);

            return true;
        }

        /// <summary>
        /// Updates tags of project, to do this we remove all previous tags.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter tags of</param>
        /// <param name="newValue">Ids of new tags</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectTagsAsync(string id, string[] newValue)
        {
            return await UpdateProjectTagsAsync(Guid.Parse(id), newValue.Select(Guid.Parse).ToArray());
        }

        /// <summary>
        /// Updates tags of project, to do this we remove all previous tags.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter tags of</param>
        /// <param name="newValue">Ids of new tags</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectTagsAsync(Guid id, string[] newValue)
        {
            return await UpdateProjectTagsAsync(id, newValue.Select(Guid.Parse).ToArray());
        }

        /// <summary>
        /// Updates tags of project, to do this we remove all previous tags.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter tags of</param>
        /// <param name="newValue">Ids of new tags</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectTagsAsync(string id, Guid[] newValue)
        {
            return await UpdateProjectTagsAsync(Guid.Parse(id), newValue);
        }

        #endregion

        #region Creators

        /// <summary>
        /// Updates creators of project, keep in mind we can only ever add to the creators list as we never
        /// want to remove access by updating.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter creators of</param>
        /// <param name="newValue">Ids of new creators</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectCreatorsAsync(Guid id, Guid[] newValue)
        {
            Guid[] existing = await database.ProjectCreatorRelations
                .Where(r => r.ProjectId == id)
                .Select(r => Guid.Parse(r.CreatorId))
                .ToArrayAsync();

            Guid[] toRemove = existing.Except(newValue).ToArray();
            Guid[] toAdd = newValue.Except(existing).ToArray();

            if (toRemove.Length > 0)
            {
                // Cascade removal to all subfolders, mirroring AddCreatorToProjectRangeAsync
                string[] toRemoveStrings = toRemove.Select(g => g.ToString()).ToArray();
                Queue<Guid> projectsToUpdate = new();
                projectsToUpdate.Enqueue(id);
                while (projectsToUpdate.Count > 0)
                {
                    Guid currentId = projectsToUpdate.Dequeue();
                    (await GetAllFolders(predicate: p => p.ParentId == currentId))
                        .Select(r => r.ChildId).ToList().ForEach(projectsToUpdate.Enqueue);
                    await database.ProjectCreatorRelations
                        .Where(r => r.ProjectId == currentId && toRemoveStrings.Contains(r.CreatorId))
                        .ExecuteDeleteAsync();
                }
            }

            if (toAdd.Length > 0)
                await AddCreatorToProjectRangeAsync(id, toAdd);

            return true;
        }

        /// <summary>
        /// Updates creators of project, keep in mind we can only ever add to the creators list as we never
        /// want to remove access by updating.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter creators of</param>
        /// <param name="newValue">Ids of new creators</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectCreatorsAsync(string id, string[] newValue)
        {
            return await UpdateProjectCreatorsAsync(Guid.Parse(id), newValue.Select(Guid.Parse).ToArray());
        }

        /// <summary>
        /// Updates creators of project, keep in mind we can only ever add to the creators list as we never
        /// want to remove access by updating.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter creators of</param>
        /// <param name="newValue">Ids of new creators</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectCreatorsAsync(Guid id, string[] newValue)
        {
            return await UpdateProjectCreatorsAsync(id, newValue.Select(Guid.Parse).ToArray());
        }

        /// <summary>
        /// Updates creators of project, keep in mind we can only ever add to the creators list as we never
        /// want to remove access by updating.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to alter creators of</param>
        /// <param name="newValue">Ids of new creators</param>
        /// <returns>Boolean indicating whether or not update was successful.</returns>
        public async Task<bool> UpdateProjectCreatorsAsync(string id, Guid[] newValue)
        {
            return await UpdateProjectCreatorsAsync(Guid.Parse(id), newValue);
        }

        #endregion
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)