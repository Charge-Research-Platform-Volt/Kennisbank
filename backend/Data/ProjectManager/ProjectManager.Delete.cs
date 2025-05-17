using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        protected async Task<int> DeleteAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.Where(predicate).ExecuteDeleteAsync(); }

        protected async Task<int> DeleteAllWhereAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.Where(predicate).ExecuteDeleteAsync(); }

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

        public async Task<bool> DeleteProject(string id)
        {
            return await DeleteProject(Guid.Parse(id));
        }
    }
}