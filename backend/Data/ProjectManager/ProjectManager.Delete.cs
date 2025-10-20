using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        /// <summary>
        /// Deletes something from the database given a table and a predicate
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of the entity to delete from the database.</typeparam>
        /// <param name="dbSet">Table where deletion takes place.</param>
        /// <param name="predicate">Predicate for deleting.</param>
        /// <returns>Amount of rows deleted in the database.</returns>
        protected async Task<int> DeleteAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.Where(predicate).ExecuteDeleteAsync(); }

        /// <summary>
        /// Deletes multiple entities from the database given a table and a predicate
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of the entity to delete from the database.</typeparam>
        /// <param name="dbSet">Table where deletion takes place.</param>
        /// <param name="predicate">Predicate for deleting multiple entities</param>
        /// <returns>Amount of rows deleted in the database.</returns>
        protected async Task<int> DeleteAllWhereAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.Where(predicate).ExecuteDeleteAsync(); }

        /// <summary>
        /// Delets a project given an id. Also deletes all linked resources, folder, creators and tags. It does this
        /// with a cascading delete so that relations between subfolders and resources are also deleted.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to delete</param>
        /// <returns>Whether or not the deletion was successful.</returns>
        public async Task<bool> DeleteProject(Guid id)
        {
            bool startedTransaction = await BeginTransaction();

            // Delete all relations of the project
            await RemoveAllResourcesFromProject(id);
            await RemoveAllFoldersFromProject(id);
            await RemoveAllCreatorsFromProject(id);
            await RemoveAllTagsFromProject(id);

            if (startedTransaction) await Commit();

            return true;
        }

        /// <summary>
        /// Delets a project given an id. Also deletes all linked resources, folder, creators and tags. It does this
        /// with a cascading delete so that relations between subfolders and resources are also deleted.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to delete</param>
        /// <returns>Whether or not the deletion was successful.</returns>
        public async Task<bool> DeleteProject(string id)
        {
            return await DeleteProject(Guid.Parse(id));
        }
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)