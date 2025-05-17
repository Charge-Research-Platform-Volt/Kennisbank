using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    // This part is for information about projects
    public partial class ProjectManager
    {

        protected async Task<bool> ExistsAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.AsNoTracking().AnyAsync(predicate); }

        protected async Task<int> GetCount<T>(DbSet<T> dbSet, Expression<Func<T, bool>>? predicate = null) where T : class
        {
            IQueryable<T> query = dbSet.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);
            return await query.CountAsync();
        }

        public async Task<bool> ProjectExistsAsync(Guid projectId)
        { return await ExistsAsync(database.Projects, project => project.Id == projectId); }

        public async Task<bool> ProjectExistsAsync(string projectId)
        { return await ProjectExistsAsync(Guid.Parse(projectId)); }

        public async Task<bool> ProjectExistsAsync(Expression<Func<Project, bool>> predicate)
        { return await ExistsAsync(database.Projects, predicate); }

        // Count
        public async Task<int> ProjectCount(Expression<Func<Project, bool>>? predicate = null)
        { return await GetCount(database.Projects, predicate); }


    }
}