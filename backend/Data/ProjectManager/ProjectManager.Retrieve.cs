using KnowledgeBank.Models;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Cmp;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        #region General
        protected async Task<T?> GetAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, params string[] includeProperties) where T : class
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (orderBy != null)
            {
                if (orderDescending)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }

            foreach (string includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return await query.Where(predicate).AsNoTracking().FirstOrDefaultAsync();
        }

        protected async Task<T?> GetAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, params string[] includeProperties) where T : class
        { return await GetAsync(dbSet, predicate, null, false, includeProperties); }

        private IQueryable<T> getAllQuery<T>(DbSet<T> dbSet, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null, params string[] includeProperties) where T : class 
        {
            IQueryable<T> query = dbSet.AsQueryable().AsNoTracking();

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
            {
                if (orderDescending)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }

            foreach (string includeProperty in includeProperties)
            {
                query = query.Include(includeProperty);
            }

            return query;
        }

        protected async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null, params string[] includeProperties) where T : class
        { return await getAllQuery(dbSet, orderBy, orderDescending, predicate, includeProperties).ToArrayAsync(); }

        protected async Task<TResult[]> GetAllAsync<TSet, TResult>(DbSet<TSet> dbSet, Expression<Func<TSet, TResult>> projection, Expression<Func<TSet, object>>? orderBy = null, bool orderDescending = false, Expression<Func<TSet, bool>>? predicate = null, params string[] includeProperties) where TSet : class
        { return await getAllQuery(dbSet, orderBy, orderDescending, predicate, includeProperties).Select(projection).ToArrayAsync(); }

        protected async Task<T[]> GetPageAsync<T>(DbSet<T> dbSet, int pageIndex = 1, int pageSize = 100, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null, params string[] includeProperties) where T : class
        {
            // Return empty for invalid inpt
            if (pageIndex < 1 || pageSize < 1) return [];

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            IQueryable<T> query = getAllQuery(dbSet, orderBy, orderDescending, predicate, includeProperties);

            return await query.Skip(skip).Take(pageSize).ToArrayAsync();
        }

        protected async Task<TResult[]> GetPageAsync<TSet, TResult>(DbSet<TSet> dbSet, Expression<Func<TSet, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<TSet, object>>? orderBy = null, bool orderDescending = false, Expression<Func<TSet, bool>>? predicate = null, params string[] includeProperties) where TSet : class
        {
            // Return empty for invalid inpt
            if (pageIndex < 1 || pageSize < 1) return [];

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            IQueryable<TSet> query = getAllQuery(dbSet, orderBy, orderDescending, predicate, includeProperties);

            return await query.Skip(skip).Take(pageSize).Select(projection).ToArrayAsync();
        }

        protected async Task<TResult> GetPropertyAsync<TSet, TResult>(DbSet<TSet> dbSet, Expression<Func<TSet, bool>> predicate, Expression<Func<TSet, TResult>> selector, Expression<Func<TSet, object>>? orderBy = null, bool orderDescending = false) where TSet : class
        {
            IQueryable<TSet> query = dbSet.Where(predicate);

            if (orderBy != null) 
                query = orderDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            return await query.Select(selector).FirstAsync();
        }

        protected async Task<TResult?> GetPropertyOrDefaultAsync<TSet, TResult>(DbSet<TSet> dbSet, Expression<Func<TSet, bool>> predicate, Expression<Func<TSet, TResult>> selector, Expression<Func<TSet, object>>? orderBy = null, bool orderDescending = false) where TSet : class
        {
            IQueryable<TSet> query = dbSet.Where(predicate);

            if (orderBy != null) 
                query = orderDescending ? query.OrderByDescending(orderBy) : query.OrderBy(orderBy);

            return await query.Select(selector).FirstOrDefaultAsync();
        }

        private readonly Expression<Func<Project, object>> projectDefaultOrderBy = project => project.CreationDate;
        private const bool projectDefaultOrderDescending = false;

        #endregion

        #region Fetching 1 project

        public async Task<Project?> GetProjectAsync(Guid id)
        { return await GetAsync(database.Projects, project => project.Id == id); }

        public async Task<Project?> GetProjectAsync(string id)
        { return await GetProjectAsync(Guid.Parse(id)); }

        public async Task<Project?> GetProjectAsync(Expression<Func<Project, bool>> predicate, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetAsync(database.Projects, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<Project?> GetProjectAsync(Guid id, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            return await GetProjectAsync(r => r.Id == id, orderBy, orderDescending, includeProperties);
        }

        public async Task<Project?> GetProjectAsync(string id, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            return await GetProjectAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties);
        }

        public async Task<Project?> GetProjectAsync(Expression<Func<Project, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.Projects, predicate, projectDefaultOrderBy, projectDefaultOrderDescending, includeProperties); }

        public async Task<Project?> GetProjectAsync(Guid id, params string[] includeProperties)
        { return await GetProjectAsync(r => r.Id == id, projectDefaultOrderBy, projectDefaultOrderDescending, includeProperties); }

        public async Task<Project?> GetProjectAsync(string id, params string[] includeProperties)
        { return await GetProjectAsync(Guid.Parse(id), projectDefaultOrderBy, projectDefaultOrderDescending, includeProperties); }
        #endregion

        #region Fetching multiple projects

        public async Task<Project[]> GetAllProjectsAsync(Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetAllAsync(database.Projects, orderBy, orderDescending, predicate);
        }

        public async Task<Project[]> GetAllProjectsAsync(Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        public async Task<Project[]> GetAllProjectsAsync(Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, orderBy, orderDescending, null, includeProperties); }

        public async Task<Project[]> GetAllProjectsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion

        #region Paged projects

        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetPageAsync(database.Projects, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetPageAsync(database.Projects, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion

        #region Filter / Projection of All Projects


        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetAllAsync(database.Projects, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projection, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetAllAsync(database.Projects, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projection, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion

        #region Filter / Projection of Paged Projects

        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion


        #region Get Project Children

        public async Task<Project?> GetProjectChildrenAsync(Guid id)
        { return await GetProjectAsync(id, includeProperties: ["ProjectResourcesRelations.Resource", "ChildFolders.ChildFolder", "ProjectTagRelations.Tag", "ProjectCreatorRelations.Creator"]); }

        public async Task<Project?> GetProjectChildrenAsync(string id)
        { return await GetProjectChildrenAsync(Guid.Parse(id)); }
        
        #endregion


    }
}