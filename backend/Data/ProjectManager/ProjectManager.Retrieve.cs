using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    public partial class ProjectManager
    {
        #region General

        /// <summary>
        /// Gets some entity given options.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of entity to retrieve.</typeparam>
        /// <param name="dbSet">Table to fetch in.</param>
        /// <param name="predicate">Predicate used to return the entities that satisfy the predicate.</param>
        /// <param name="orderBy">What to order the returned entities by.</param>
        /// <param name="orderDescending">Whether or not the returned entities should be ordered descending or not.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>Entities fetched and optionally ordered.</returns>
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

        /// <summary>
        /// Gets some entity given options.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of entity to retrieve.</typeparam>
        /// <param name="dbSet">Table to fetch in.</param>
        /// <param name="predicate">Predicate used to return the entities that satisfy the predicate.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>Entities fetched.</returns>
        protected async Task<T?> GetAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, params string[] includeProperties) where T : class
        { return await GetAsync(dbSet, predicate, null, false, includeProperties); }

        /// <summary>
        /// Constructs a query used to get all entities.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of entity.</typeparam>
        /// <param name="dbSet">Table in question.</param>
        /// <param name="orderBy">What to order it by.</param>
        /// <param name="orderDescending">Whether or not it should be ordered descending.</param>
        /// <param name="predicate">Predicate to filter on.</param>
        /// <param name="includeProperties">Extra navigation properties.</param>
        /// <returns>Query to fetch all entities of a specific type with some options included.</returns>
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

        /// <summary>
        /// Gets all entities given options.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="T">Type of entity to retrieve.</typeparam>
        /// <param name="dbSet">Table to fetch in.</param>
        /// <param name="predicate">Predicate used to return the entities that satisfy the predicate.</param>
        /// <param name="orderBy">What to order the returned entities by.</param>
        /// <param name="orderDescending">Whether or not the returned entities should be ordered descending or not.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All entities of some type fetched and optionally filtered/ordered.</returns>
        protected async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null, params string[] includeProperties) where T : class
        { return await getAllQuery(dbSet, orderBy, orderDescending, predicate, includeProperties).ToArrayAsync(); }

        /// <summary>
        /// Gets all entities given options.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="dbSet">Table to fetch in.</param>
        /// <param name="projection">Which column to fetch</param>
        /// <param name="predicate">Predicate used to return the entities that satisfy the predicate.</param>
        /// <param name="orderBy">What to order the returned entities by.</param>
        /// <param name="orderDescending">Whether or not the returned entities should be ordered descending or not.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All entities of some type fetched and optionally filtered/ordered.</returns>
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

        /// <summary>
        /// Gets paged results of a specific entity.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <typeparam name="TSet">Input type</typeparam>
        /// <typeparam name="TResult">Output type</typeparam>
        /// <param name="dbSet">Table to fetch in.</param>
        /// <param name="projection">Columns to retrieve.</param>
        /// <param name="pageIndex">Which page to retrieve.</param>
        /// <param name="pageSize">How many items are retrieved per page.</param>
        /// <param name="orderBy">What to order items by.</param>
        /// <param name="orderDescending">Whether or not the items should be ordered descending.</param>
        /// <param name="predicate">Which items to take.</param>
        /// <param name="includeProperties">Extra navigation properties.</param>
        /// <returns>Paged contents of some item.</returns>
        protected async Task<TResult[]> GetPageAsync<TSet, TResult>(DbSet<TSet> dbSet, Expression<Func<TSet, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<TSet, object>>? orderBy = null, bool orderDescending = false, Expression<Func<TSet, bool>>? predicate = null, params string[] includeProperties) where TSet : class
        {
            // Return empty for invalid inpt
            if (pageIndex < 1 || pageSize < 1) return [];

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            IQueryable<TSet> query = getAllQuery(dbSet, orderBy, orderDescending, predicate, includeProperties);

            return await query.Skip(skip).Take(pageSize).Select(projection).ToArrayAsync();
        }

        private readonly Expression<Func<Project, object>> projectDefaultOrderBy = project => project.CreationDate;
        private const bool projectDefaultOrderDescending = false;

        #endregion

        #region Fetching 1 project

        /// <summary>
        /// Retrieves project given an id.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Project id.</param>
        /// <returns>Project retrieved.</returns>
        public async Task<Project?> GetProjectAsync(Guid id)
        { return await GetAsync(database.Projects, project => project.Id == id); }

        /// <summary>
        /// Retrieves project given an id.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Project id.</param>
        /// <returns>Project retrieved.</returns>
        public async Task<Project?> GetProjectAsync(string id)
        { return await GetProjectAsync(Guid.Parse(id)); }

        /// <summary>
        /// Retrieves single project given a predicate.
        ///
        /// </summary>
        /// <param name="predicate">What the retrieved project should satisfy.</param>
        /// <param name="orderBy">unused.</param>
        /// <param name="orderDescending">unused.</param>
        /// <param name="includeProperties">Extra navigation properties.</param>
        /// <returns>Project retrieved with navigation properties.</returns>
        public async Task<Project?> GetProjectAsync(Expression<Func<Project, bool>> predicate, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetAsync(database.Projects, predicate, orderBy, orderDescending, includeProperties);
        }

        /// <summary>
        /// Retrieves a single project with navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to retrieve</param>
        /// <param name="orderBy">unused</param>
        /// <param name="orderDescending">unused</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>Project retrieved with navigation properties.</returns>
        public async Task<Project?> GetProjectAsync(Guid id, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            return await GetProjectAsync(r => r.Id == id, orderBy, orderDescending, includeProperties);
        }

        /// <summary>
        /// Retrieves a single project with navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id of the project to retrieve</param>
        /// <param name="orderBy">unused</param>
        /// <param name="orderDescending">unused</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>Project retrieved with navigation properties.</returns>
        public async Task<Project?> GetProjectAsync(string id, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            return await GetProjectAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties);
        }

        /// <summary>
        /// Gets a project given a predicate and extra navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="predicate">Predicate to fetch project with.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>Project retrieved with navigation properties.</returns>
        public async Task<Project?> GetProjectAsync(Expression<Func<Project, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.Projects, predicate, projectDefaultOrderBy, projectDefaultOrderDescending, includeProperties); }

        /// <summary>
        /// Gets a project given an id and extra navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id to fetch project with.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>Project retrieved with navigation properties.</returns>
        public async Task<Project?> GetProjectAsync(Guid id, params string[] includeProperties)
        { return await GetProjectAsync(r => r.Id == id, projectDefaultOrderBy, projectDefaultOrderDescending, includeProperties); }

        /// <summary>
        /// Gets a project given an id and extra navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="id">Id to fetch project with.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>Project retrieved with navigation properties.</returns>
        public async Task<Project?> GetProjectAsync(string id, params string[] includeProperties)
        { return await GetProjectAsync(Guid.Parse(id), projectDefaultOrderBy, projectDefaultOrderDescending, includeProperties); }
        #endregion

        #region Fetching multiple projects

        /// <summary>
        /// Fetches all projects using a predicate and optional ordering.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="orderBy">What to order the projects by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="predicate">Predicate used to fetch the projects the user wants to fetch.</param>
        /// <returns>All projects satisfying the predicate and optionally ordered.</returns>
        public async Task<Project[]> GetAllProjectsAsync(Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetAllAsync(database.Projects, orderBy, orderDescending, predicate);
        }

        /// <summary>
        /// Fetches all projects using a predicate with extra navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="predicate">Predicate used to fetch the projects the user wants to fetch.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All projects satisfying the predicate with the navigation properties.</returns>
        public async Task<Project[]> GetAllProjectsAsync(Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        /// <summary>
        /// Fetches all projects using with optional ordering and extra navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="orderBy">What to order the projects by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All projects optionally ordered with navigation properties.</returns>
        public async Task<Project[]> GetAllProjectsAsync(Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, orderBy, orderDescending, null, includeProperties); }

        /// <summary>
        /// Fetches all projects with extra navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All projects with navigation properties.</returns>
        public async Task<Project[]> GetAllProjectsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion

        #region Paged projects

        /// <summary>
        /// Gets a page of projects.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="orderBy">What to order projects by.</param>
        /// <param name="orderDescending">Whether to order in descending order or not.</param>
        /// <param name="predicate">Predicate to filter projects on.</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) and filtered by the predicate.</returns>
        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetPageAsync(database.Projects, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        /// <summary>
        /// Gets a page of projects using predicate and with properties.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="predicate">Predicate to filter projects on.</param>
        /// <param name="includeProperties">Navigation properties to include.</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) and filtered by the predicate, with properties.</returns>
        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        /// <summary>
        /// Gets a page of projects, including some navigation properties.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="orderBy">What to order projects by.</param>
        /// <param name="orderDescending">Whether to order in descending order or not.</param>
        /// <param name="includeProperties">Navigation properties to include</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) with extra properties.</returns>
        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;

            return await GetPageAsync(database.Projects, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        /// <summary>
        /// Gets a page of projects.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="includeProperties">Navigation properties to include</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) with extra properties.</returns>
        public async Task<Project[]> GetProjectPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion

        #region Filter / Projection of All Projects

        /// <summary>
        /// Fetches all projects using a predicate and optional ordering and projection of a column.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="orderBy">What to order the projects by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="predicate">Predicate used to fetch the projects the user wants to fetch.</param>
        /// <returns>All projects satisfying the predicate and optionally ordered.</returns>
        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetAllAsync(database.Projects, projection, orderBy, orderDescending, predicate);
        }

        /// <summary>
        /// Fetches all projects using a predicate with extra navigation properties and projection of a column.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="predicate">Predicate used to fetch the projects the user wants to fetch.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All projects satisfying the predicate with the navigation properties.</returns>
        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projection, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        /// <summary>
        /// Fetches all projects using with optional ordering and extra navigation properties and projection of a column.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="orderBy">What to order the projects by.</param>
        /// <param name="orderDescending">Whether to order descending or not.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All projects optionally ordered with navigation properties.</returns>
        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetAllAsync(database.Projects, projection, orderBy, orderDescending, null, includeProperties);
        }

        /// <summary>
        /// Fetches all projects with extra navigation properties and projection of a column.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="includeProperties">Extra navigation properties</param>
        /// <returns>All projects with navigation properties.</returns>
        public async Task<TResult[]> GetAllProjectsAsync<TResult>(Expression<Func<Project, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.Projects, projection, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion

        #region Filter / Projection of Paged Projects

        /// <summary>
        /// Gets a page of projects and projection of a column.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="orderBy">What to order projects by.</param>
        /// <param name="orderDescending">Whether to order in descending order or not.</param>
        /// <param name="predicate">Predicate to filter projects on.</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) and filtered by the predicate.</returns>
        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, Expression<Func<Project, bool>>? predicate = null)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        /// <summary>
        /// Gets a page of projects using predicate and with properties and projection of a column.
        ///
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="predicate">Predicate to filter projects on.</param>
        /// <param name="includeProperties">Navigation properties to include.</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) and filtered by the predicate, with properties.</returns>
        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Project, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, predicate, includeProperties); }

        /// <summary>
        /// Gets a page of projects, including some navigation properties and projection of a column.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="orderBy">What to order projects by.</param>
        /// <param name="orderDescending">Whether to order in descending order or not.</param>
        /// <param name="includeProperties">Navigation properties to include</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) with extra properties.</returns>
        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Project, object>>? orderBy = null, bool orderDescending = projectDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= projectDefaultOrderBy;
            return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        /// <summary>
        /// Gets a page of projects and projection of a column.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="projection">Which column to display.</param>
        /// <param name="pageIndex">Index of the page to fetch.</param>
        /// <param name="pageSize">Size of pages.</param>
        /// <param name="includeProperties">Navigation properties to include</param>
        /// <returns>Page of projects, of the indicated size (or less if there aren't that many) with extra properties.</returns>
        public async Task<TResult[]> GetProjectPageAsync<TResult>(Expression<Func<Project, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Projects, projection, pageIndex, pageSize, projectDefaultOrderBy, projectDefaultOrderDescending, null, includeProperties); }

        #endregion


        #region Get Project Children

        /// <summary>
        /// Gets the contents of a specific project. This includes the resources, folders, tags, and creators.
        /// 
        /// Author: Jelle v.h. Schut
        /// </summary>
        /// <param name="id">Id of project to fetch contents of</param>
        /// <returns>Project with contents included.</returns>
        public async Task<Project?> GetProjectChildrenAsync(Guid id)
        { return await GetProjectAsync(id, includeProperties: ["ProjectResourcesRelations.Resource", "ChildFolders.ChildFolder", "ProjectTagRelations.Tag", "ProjectCreatorRelations.Creator"]); }

        /// <summary>
        /// Gets the contents of a specific project. This includes the resources, folders, tags, and creators.
        /// 
        /// Author: Jelle v.h. Schut
        /// </summary>
        /// <param name="id">Id of project to fetch contents of</param>
        /// <returns>Project with contents included.</returns>
        public async Task<Project?> GetProjectChildrenAsync(string id)
        { return await GetProjectChildrenAsync(Guid.Parse(id)); }

        #endregion

        #region fetch user

        /// <summary>
        /// Gets the user names given a list of ids.
        /// 
        /// Author: Justin Liem
        /// </summary>
        /// <param name="ids">Ids to fetch usernames of.</param>
        /// <returns>Usernames corresponding to the ids given.</returns>
        public async Task<Dictionary<string, string>> GetUserNamesByIds(HashSet<string> ids)
        {
            Dictionary<string, string> dict = new();
            foreach (IdentityUser u in await GetAllAsync(dbSet: database.Users, predicate: user => ids.Contains(user.Id)))
                dict.Add(u.Id, u.UserName ?? "Unknown");
            return dict;
        }
        #endregion

    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)