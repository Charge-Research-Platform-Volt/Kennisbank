using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models;
using System.Linq.Expressions;

namespace backend.Data
{
    // This part is for information
    public partial class ResourceManager
    {
        #region Generic functions

        protected async Task<bool> ExistsAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.AnyAsync(predicate); }

        #endregion



        // --------------------------------



        #region Resource

        // Hash exists

        public async Task<Guid?> HashExistsAsync(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return null;

            if (!await ExistsAsync(database.Resources, resource => resource.Hash == hash))
                return null;

            return await GetPropertyAsync(database.Resources, resource => resource.Hash == hash, resource => resource.Id);
        }

        // Resource exists

        public async Task<bool> ResourceExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.Resources, resource => resource.Id == resourceId); }

        public async Task<bool> ResourceExistsAsync(string resourceId)
        { return await ResourceExistsAsync(Guid.Parse(resourceId)); }

        #endregion
    }
}
