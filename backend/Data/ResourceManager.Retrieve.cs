using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    // This part is for retrieving resources or their properties
    public partial class ResourceManager
    {
        // Generic functions:
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

            foreach (var includeProperty in includeProperties)
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

            foreach (var includeProperty in includeProperties)
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



        // --------------------------------



        #region Resource

        // Resource itself

        private readonly Expression<Func<Resource, object>> resourceDefaultOrderBy = resource => resource.CreationDate;
        private const bool resourceDefaultOrderDescending = true;


        // Single
        public async Task<Resource?> GetResourceAsync(Guid id)
        { return await GetAsync(database.Resources, r => r.Id == id); }

        public async Task<Resource?> GetResourceAsync(string id)
        { return await GetResourceAsync(Guid.Parse(id)); }



        public async Task<Resource?> GetResourceAsync(Expression<Func<Resource, bool>> predicate, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceDefaultOrderBy;

            return await GetAsync(database.Resources, predicate, orderBy, orderDescending, includeProperties); 
        }

        public async Task<Resource?> GetResourceAsync(Guid id, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params string[] includeProperties)
        { return await GetResourceAsync(r => r.Id == id, orderBy, orderDescending, includeProperties); }

        public async Task<Resource?> GetResourceAsync(string id, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params string[] includeProperties)
        { return await GetResourceAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties); }



        public async Task<Resource?> GetResourceAsync(Expression<Func<Resource, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.Resources, predicate, resourceDefaultOrderBy, resourceDefaultOrderDescending, includeProperties); }

        public async Task<Resource?> GetResourceAsync(Guid id, params string[] includeProperties)
        { return await GetResourceAsync(r => r.Id == id, resourceDefaultOrderBy, resourceDefaultOrderDescending, includeProperties); }

        public async Task<Resource?> GetResourceAsync(string id, params string[] includeProperties)
        { return await GetResourceAsync(Guid.Parse(id), resourceDefaultOrderBy, resourceDefaultOrderDescending, includeProperties); }


        // Multiple
        public async Task<Resource[]> GetAllResourcesAsync(Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            orderBy ??= resourceDefaultOrderBy;

            return await GetAllAsync(database.Resources, orderBy, orderDescending, predicate);
        }

        public async Task<Resource[]> GetAllResourcesAsync(Expression<Func<Resource, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Resources, resourceDefaultOrderBy, resourceDefaultOrderDescending, predicate, includeProperties); }

        public async Task<Resource[]> GetAllResourcesAsync(Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.Resources, orderBy, orderDescending, null, includeProperties); }

        public async Task<Resource[]> GetAllResourcesAsync(params string[] includeProperties)
        { return await GetAllAsync(database.Resources, resourceDefaultOrderBy, resourceDefaultOrderDescending, null, includeProperties); }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            orderBy ??= resourceDefaultOrderBy;

            return await GetPageAsync(database.Resources, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Resources, pageIndex, pageSize, resourceDefaultOrderBy, regionDefaultOrderDescending, predicate, includeProperties); }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceDefaultOrderBy;

            return await GetPageAsync(database.Resources, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Resources, pageIndex, pageSize, resourceDefaultOrderBy, resourceDefaultOrderDescending, null, includeProperties); }
        
        // Multiple with projection
        public async Task<TResult[]> GetAllResourcesAsync<TResult>(Expression<Func<Resource, TResult>> projection, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            orderBy ??= resourceDefaultOrderBy;
            return await GetAllAsync(database.Resources, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllResourcesAsync<TResult>(Expression<Func<Resource, TResult>> projection, Expression<Func<Resource, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Resources, projection, resourceDefaultOrderBy, resourceDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllResourcesAsync<TResult>(Expression<Func<Resource, TResult>> projection, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceDefaultOrderBy;
            return await GetAllAsync(database.Resources, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllResourcesAsync<TResult>(Expression<Func<Resource, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.Resources, projection, resourceDefaultOrderBy, resourceDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetResourcePageAsync<TResult>(Expression<Func<Resource, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            orderBy ??= resourceDefaultOrderBy;
            return await GetPageAsync(database.Resources, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetResourcePageAsync<TResult>(Expression<Func<Resource, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Resources, projection, pageIndex, pageSize, resourceDefaultOrderBy, resourceDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetResourcePageAsync<TResult>(Expression<Func<Resource, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceDefaultOrderBy;
            return await GetPageAsync(database.Resources, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetResourcePageAsync<TResult>(Expression<Func<Resource, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Resources, projection, pageIndex, pageSize, resourceDefaultOrderBy, resourceDefaultOrderDescending, null, includeProperties); }    


        // Properties

        public async Task<T> GetResourcePropertyAsync<T>(Guid resourceId, Expression<Func<Resource, T>> selector, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending)
        { 
            orderBy ??= resourceDefaultOrderBy;
            return await GetPropertyAsync(database.Resources, r => r.Id == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetResourcePropertyAsync<T>(string resourceId, Expression<Func<Resource, T>> selector, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending)
        { return await GetResourcePropertyAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetResourcePropertyAsync<T>(Expression<Func<Resource, bool>> predicate, Expression<Func<Resource, T>> selector, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending)
        {
            orderBy ??= resourceDefaultOrderBy;
            return await GetPropertyAsync(database.Resources, predicate, selector, orderBy, orderDescending);
        }




        public async Task<T?> GetResourcePropertyOrDefaultAsync<T>(Guid resourceId, Expression<Func<Resource, T>> selector, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending)
        { 
            orderBy ??= resourceDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Resources, r => r.Id == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetResourcePropertyOrDefaultAsync<T>(string resourceId, Expression<Func<Resource, T>> selector, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending)
        { return await GetResourcePropertyOrDefaultAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetResourcePropertyOrDefaultAsync<T>(Expression<Func<Resource, bool>> predicate, Expression<Func<Resource, T>> selector, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending)
        {
            orderBy ??= resourceDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Resources, predicate, selector, orderBy, orderDescending);
        }

        #endregion



        #region Person

        private readonly Expression<Func<Person, object>> personDefaultOrderBy = person => person.Name;
        private const bool personDefaultOrderDescending = false;


        // Single
        public async Task<Person?> GetPersonAsync(Guid id)
        { return await GetAsync(database.Persons, p => p.Id == id); }

        public async Task<Person?> GetPersonAsync(string id)
        { return await GetPersonAsync(Guid.Parse(id)); }

        public async Task<Person?> GetPersonAsync(Expression<Func<Person, bool>> predicate, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personDefaultOrderBy;

            return await GetAsync(database.Persons, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<Person?> GetPersonAsync(Guid id, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params string[] includeProperties)
        { return await GetPersonAsync(p => p.Id == id, orderBy, orderDescending, includeProperties); }

        public async Task<Person?> GetPersonAsync(string id, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params string[] includeProperties)
        { return await GetPersonAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties); }



        public async Task<Person?> GetPersonAsync(Expression<Func<Person, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.Persons, predicate, personDefaultOrderBy, personDefaultOrderDescending, includeProperties); }

        public async Task<Person?> GetPersonAsync(Guid id, params string[] includeProperties)
        { return await GetPersonAsync(p => p.Id == id, personDefaultOrderBy, personDefaultOrderDescending, includeProperties); }

        public async Task<Person?> GetPersonAsync(string id, params string[] includeProperties)
        { return await GetPersonAsync(Guid.Parse(id), personDefaultOrderBy, personDefaultOrderDescending, includeProperties); }


        // Multiple
        public async Task<Person[]> GetAllPersonsAsync(Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            orderBy ??= personDefaultOrderBy;

            return await GetAllAsync(database.Persons, orderBy, orderDescending, predicate);
        }

        public async Task<Person[]> GetAllPersonsAsync(Expression<Func<Person, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Persons, null, false, predicate, includeProperties); }

        public async Task<Person[]> GetAllPersonsAsync(Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.Persons, orderBy, orderDescending, null, includeProperties); }

        public async Task<Person[]> GetAllPersonsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.Persons, personDefaultOrderBy, personDefaultOrderDescending, null, includeProperties); }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            orderBy ??= personDefaultOrderBy;

            return await GetPageAsync(database.Persons, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Person, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Persons, pageIndex, pageSize, personDefaultOrderBy, personDefaultOrderDescending, predicate, includeProperties); }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personDefaultOrderBy;
            return await GetPageAsync(database.Persons, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Persons, pageIndex, pageSize, personDefaultOrderBy, personDefaultOrderDescending, null, includeProperties); }

        
        // Multiple with projection
        public async Task<TResult[]> GetAllPersonsAsync<TResult>(Expression<Func<Person, TResult>> projection, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            orderBy ??= personDefaultOrderBy;
            return await GetAllAsync(database.Persons, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllPersonsAsync<TResult>(Expression<Func<Person, TResult>> projection, Expression<Func<Person, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Persons, projection, personDefaultOrderBy, personDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllPersonsAsync<TResult>(Expression<Func<Person, TResult>> projection, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personDefaultOrderBy;
            return await GetAllAsync(database.Persons, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllPersonsAsync<TResult>(Expression<Func<Person, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.Persons, projection, personDefaultOrderBy, personDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetPersonPageAsync<TResult>(Expression<Func<Person, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            orderBy ??= personDefaultOrderBy;
            return await GetPageAsync(database.Persons, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetPersonPageAsync<TResult>(Expression<Func<Person, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Person, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Persons, projection, pageIndex, pageSize, personDefaultOrderBy, personDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetPersonPageAsync<TResult>(Expression<Func<Person, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= personDefaultOrderBy;
            return await GetPageAsync(database.Persons, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetPersonPageAsync<TResult>(Expression<Func<Person, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Persons, projection, pageIndex, pageSize, personDefaultOrderBy, personDefaultOrderDescending, null, includeProperties); }

        // Property

        public async Task<T> GetPersonPropertyAsync<T>(Guid personId, Expression<Func<Person, T>> selector, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending)
        { 
            orderBy ??= personDefaultOrderBy;
            return await GetPropertyAsync(database.Persons, p => p.Id == personId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetPersonPropertyAsync<T>(string personId, Expression<Func<Person, T>> selector, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending)
        { return await GetPersonPropertyAsync(Guid.Parse(personId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetPersonPropertyAsync<T>(Expression<Func<Person, bool>> predicate, Expression<Func<Person, T>> selector, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending)
        {
            orderBy ??= personDefaultOrderBy;
            return await GetPropertyAsync(database.Persons, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetPersonPropertyOrDefaultAsync<T>(Guid personId, Expression<Func<Person, T>> selector, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending)
        { 
            orderBy ??= personDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Persons, p => p.Id == personId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetPersonPropertyOrDefaultAsync<T>(string personId, Expression<Func<Person, T>> selector, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending)
        { return await GetPersonPropertyOrDefaultAsync(Guid.Parse(personId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetPersonPropertyOrDefaultAsync<T>(Expression<Func<Person, bool>> predicate, Expression<Func<Person, T>> selector, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending)
        {
            orderBy ??= personDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Persons, predicate, selector, orderBy, orderDescending);
        }

        #endregion



        #region Organisation

        private readonly Expression<Func<Organisation, object>> organisationDefaultOrderBy = organisation => organisation.Name;
        private const bool organisationDefaultOrderDescending = false;

        // Single
        public async Task<Organisation?> GetOrganisationAsync(Guid id)
        { return await GetAsync(database.Organisations, o => o.Id == id); }

        public async Task<Organisation?> GetOrganisationAsync(string id)
        { return await GetOrganisationAsync(Guid.Parse(id)); }

        public async Task<Organisation?> GetOrganisationAsync(Expression<Func<Organisation, bool>> predicate, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetAsync(database.Organisations, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<Organisation?> GetOrganisationAsync(Guid id, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params string[] includeProperties)
        { return await GetOrganisationAsync(o => o.Id == id, orderBy, orderDescending, includeProperties); }

        public async Task<Organisation?> GetOrganisationAsync(string id, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params string[] includeProperties)
        { return await GetOrganisationAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties); }



        public async Task<Organisation?> GetOrganisationAsync(Expression<Func<Organisation, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.Organisations, predicate, organisationDefaultOrderBy, organisationDefaultOrderDescending, includeProperties); }

        public async Task<Organisation?> GetOrganisationAsync(Guid id, params string[] includeProperties)
        { return await GetOrganisationAsync(o => o.Id == id, organisationDefaultOrderBy, organisationDefaultOrderDescending, includeProperties); }

        public async Task<Organisation?> GetOrganisationAsync(string id, params string[] includeProperties)
        { return await GetOrganisationAsync(Guid.Parse(id), organisationDefaultOrderBy, organisationDefaultOrderDescending, includeProperties); }



        // Multiple
        public async Task<Organisation[]> GetAllOrganisationsAsync(Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetAllAsync(database.Organisations, orderBy, orderDescending, predicate);
        }

        public async Task<Organisation[]> GetAllOrganisationsAsync(Expression<Func<Organisation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Organisations, null, false, predicate, includeProperties); }

        public async Task<Organisation[]> GetAllOrganisationsAsync(Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.Organisations, orderBy, orderDescending, null, includeProperties); }

        public async Task<Organisation[]> GetAllOrganisationsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.Organisations, organisationDefaultOrderBy, organisationDefaultOrderDescending, null, includeProperties); }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetPageAsync(database.Organisations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Organisations, pageIndex, pageSize, organisationDefaultOrderBy, organisationDefaultOrderDescending, predicate, includeProperties); }
        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetPageAsync(database.Organisations, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Organisations, pageIndex, pageSize, organisationDefaultOrderBy, organisationDefaultOrderDescending, null, includeProperties); }

        
        // Multiple with projection
        public async Task<TResult[]> GetAllOrganisationsAsync<TResult>(Expression<Func<Organisation, TResult>> projection, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetAllAsync(database.Organisations, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllOrganisationsAsync<TResult>(Expression<Func<Organisation, TResult>> projection, Expression<Func<Organisation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Organisations, projection, organisationDefaultOrderBy, organisationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllOrganisationsAsync<TResult>(Expression<Func<Organisation, TResult>> projection, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetAllAsync(database.Organisations, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllOrganisationsAsync<TResult>(Expression<Func<Organisation, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.Organisations, projection, organisationDefaultOrderBy, organisationDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetOrganisationPageAsync<TResult>(Expression<Func<Organisation, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetPageAsync(database.Organisations, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetOrganisationPageAsync<TResult>(Expression<Func<Organisation, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Organisations, projection, pageIndex, pageSize, organisationDefaultOrderBy, organisationDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetOrganisationPageAsync<TResult>(Expression<Func<Organisation, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetPageAsync(database.Organisations, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetOrganisationPageAsync<TResult>(Expression<Func<Organisation, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Organisations, projection, pageIndex, pageSize, organisationDefaultOrderBy, organisationDefaultOrderDescending, null, includeProperties); }
        

        // Property

        public async Task<T> GetOrganisationPropertyAsync<T>(Guid organisationId, Expression<Func<Organisation, T>> selector, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending)
        { 
            orderBy ??= organisationDefaultOrderBy;
            return await GetPropertyAsync(database.Organisations, o => o.Id == organisationId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetOrganisationPropertyAsync<T>(string organisationId, Expression<Func<Organisation, T>> selector, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending)
        { return await GetOrganisationPropertyAsync(Guid.Parse(organisationId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetOrganisationPropertyAsync<T>(Expression<Func<Organisation, bool>> predicate, Expression<Func<Organisation, T>> selector, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetPropertyAsync(database.Organisations, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetOrganisationPropertyOrDefaultAsync<T>(Guid organisationId, Expression<Func<Organisation, T>> selector, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Organisations, o => o.Id == organisationId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetOrganisationPropertyOrDefaultAsync<T>(string organisationId, Expression<Func<Organisation, T>> selector, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending)
        { return await GetOrganisationPropertyOrDefaultAsync(Guid.Parse(organisationId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetOrganisationPropertyOrDefaultAsync<T>(Expression<Func<Organisation, bool>> predicate, Expression<Func<Organisation, T>> selector, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending)
        {
            orderBy ??= organisationDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Organisations, predicate, selector, orderBy, orderDescending);
        }

        #endregion

        #region Region

        private readonly Expression<Func<Region, object>> regionDefaultOrderBy = r => r.Name;
        private const bool regionDefaultOrderDescending = false;


        // Single
        public async Task<Region?> GetRegionAsync(Guid id)
        { return await GetAsync(database.Regions, r => r.Id == id); }

        public async Task<Region?> GetRegionAsync(string id)
        { return await GetRegionAsync(Guid.Parse(id)); }

        public async Task<Region?> GetRegionAsync(Expression<Func<Region, bool>> predicate, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetAsync(database.Regions, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<Region?> GetRegionAsync(Guid id, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params string[] includeProperties)
        { return await GetRegionAsync(r => r.Id == id, orderBy, orderDescending, includeProperties); }

        public async Task<Region?> GetRegionAsync(string id, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params string[] includeProperties)
        { return await GetRegionAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties); }



        public async Task<Region?> GetRegionAsync(Expression<Func<Region, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.Regions, predicate, regionDefaultOrderBy, regionDefaultOrderDescending, includeProperties); }

        public async Task<Region?> GetRegionAsync(Guid id, params string[] includeProperties)
        { return await GetRegionAsync(r => r.Id == id, regionDefaultOrderBy, regionDefaultOrderDescending, includeProperties); }

        public async Task<Region?> GetRegionAsync(string id, params string[] includeProperties)
        { return await GetRegionAsync(Guid.Parse(id), regionDefaultOrderBy, regionDefaultOrderDescending, includeProperties); }


        // Multiple
        public async Task<Region[]> GetAllRegionsAsync(Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetAllAsync(database.Regions, orderBy, orderDescending, predicate);
        }

        public async Task<Region[]> GetAllRegionsAsync(Expression<Func<Region, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Regions, null, false, predicate, includeProperties); }

        public async Task<Region[]> GetAllRegionsAsync(Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.Regions, orderBy, orderDescending, null, includeProperties); }

        public async Task<Region[]> GetAllRegionsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.Regions, regionDefaultOrderBy, regionDefaultOrderDescending, null, includeProperties); }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPageAsync(database.Regions, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Region, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Regions, pageIndex, pageSize, regionDefaultOrderBy, regionDefaultOrderDescending, predicate, includeProperties); }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPageAsync(database.Regions, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Regions, pageIndex, pageSize, regionDefaultOrderBy, regionDefaultOrderDescending, null, includeProperties); }


        // Multiple with projections
        public async Task<TResult[]> GetAllRegionsAsync<TResult>(Expression<Func<Region, TResult>> projection, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetAllAsync(database.Regions, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllRegionsAsync<TResult>(Expression<Func<Region, TResult>> projection, Expression<Func<Region, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Regions, projection, regionDefaultOrderBy, regionDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllRegionsAsync<TResult>(Expression<Func<Region, TResult>> projection, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetAllAsync(database.Regions, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllRegionsAsync<TResult>(Expression<Func<Region, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.Regions, projection, regionDefaultOrderBy, regionDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetRegionPageAsync<TResult>(Expression<Func<Region, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPageAsync(database.Regions, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetRegionPageAsync<TResult>(Expression<Func<Region, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Region, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Regions, projection, pageIndex, pageSize, regionDefaultOrderBy, regionDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetRegionPageAsync<TResult>(Expression<Func<Region, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPageAsync(database.Regions, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetRegionPageAsync<TResult>(Expression<Func<Region, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Regions, projection, pageIndex, pageSize, regionDefaultOrderBy, regionDefaultOrderDescending, null, includeProperties); }
        

        // Property

        public async Task<T> GetRegionPropertyAsync<T>(Guid regionId, Expression<Func<Region, T>> selector, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPropertyAsync(database.Regions, r => r.Id == regionId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetRegionPropertyAsync<T>(string regionId, Expression<Func<Region, T>> selector, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending)
        { return await GetRegionPropertyAsync(Guid.Parse(regionId), selector, orderBy, orderDescending); }
        
        public async Task<T> GetRegionPropertyAsync<T>(Expression<Func<Region, bool>> predicate, Expression<Func<Region, T>> selector, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPropertyAsync(database.Regions, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetRegionPropertyOrDefaultAsync<T>(Guid regionId, Expression<Func<Region, T>> selector, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Regions, r => r.Id == regionId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetRegionPropertyOrDefaultAsync<T>(string regionId, Expression<Func<Region, T>> selector, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending)
        { return await GetRegionPropertyOrDefaultAsync(Guid.Parse(regionId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetRegionPropertyOrDefaultAsync<T>(Expression<Func<Region, bool>> predicate, Expression<Func<Region, T>> selector, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending)
        {
            orderBy ??= regionDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Regions, predicate, selector, orderBy, orderDescending);
        }

        #endregion




        #region Audio

        private readonly Expression<Func<AudioMetadata, object>>? audioDefaultOrderBy = null;
        private const bool audioDefaultOrderDescending = false;


        // Single
        public async Task<AudioMetadata?> GetAudioMetadataAsync(Guid resourceId)
        { return await GetAsync(database.AudioMetadata, a => a.ResourceId == resourceId); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(string resourceId)
        { return await GetAudioMetadataAsync(Guid.Parse(resourceId)); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Expression<Func<AudioMetadata, bool>> predicate, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetAsync(database.AudioMetadata, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Guid resourceId, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params string[] includeProperties)
        { return await GetAudioMetadataAsync(a => a.ResourceId == resourceId, orderBy, orderDescending, includeProperties); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(string resourceId, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params string[] includeProperties)
        { return await GetAudioMetadataAsync(Guid.Parse(resourceId), orderBy, orderDescending, includeProperties); }



        public async Task<AudioMetadata?> GetAudioMetadataAsync(Expression<Func<AudioMetadata, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.AudioMetadata, predicate, audioDefaultOrderBy, audioDefaultOrderDescending, includeProperties); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Guid resourceId, params string[] includeProperties)
        { return await GetAudioMetadataAsync(a => a.ResourceId == resourceId, audioDefaultOrderBy, audioDefaultOrderDescending, includeProperties); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(string resourceId, params string[] includeProperties)
        { return await GetAudioMetadataAsync(Guid.Parse(resourceId), audioDefaultOrderBy, audioDefaultOrderDescending, includeProperties); }


        // Multiple
        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetAllAsync(database.AudioMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(Expression<Func<AudioMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.AudioMetadata, null, false, predicate, includeProperties); }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.AudioMetadata, orderBy, orderDescending, null, includeProperties); }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(params string[] includeProperties)
        { return await GetAllAsync(database.AudioMetadata, audioDefaultOrderBy, audioDefaultOrderDescending, null, includeProperties); }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, audioDefaultOrderBy, audioDefaultOrderDescending, predicate, includeProperties); }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, audioDefaultOrderBy, audioDefaultOrderDescending, null, includeProperties); }

        // Multpile with projection
        public async Task<TResult[]> GetAllAudioMetadatasAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetAllAsync(database.AudioMetadata, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllAudioMetadatasAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, Expression<Func<AudioMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.AudioMetadata, projection, audioDefaultOrderBy, audioDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllAudioMetadatasAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetAllAsync(database.AudioMetadata, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllAudioMetadatasAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.AudioMetadata, projection, audioDefaultOrderBy, audioDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetAudioMetadataPageAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetPageAsync(database.AudioMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAudioMetadataPageAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.AudioMetadata, projection, pageIndex, pageSize, audioDefaultOrderBy, audioDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAudioMetadataPageAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetPageAsync(database.AudioMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAudioMetadataPageAsync<TResult>(Expression<Func<AudioMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.AudioMetadata, projection, pageIndex, pageSize, audioDefaultOrderBy, audioDefaultOrderDescending, null, includeProperties); }


        // Property

        public async Task<T> GetAudioMetadataPropertyAsync<T>(Guid resourceId, Expression<Func<AudioMetadata, T>> selector, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetPropertyAsync(database.AudioMetadata, a => a.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetAudioMetadataPropertyAsync<T>(string resourceId, Expression<Func<AudioMetadata, T>> selector, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending)
        { return await GetAudioMetadataPropertyAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T> GetAudioMetadataPropertyAsync<T>(Expression<Func<AudioMetadata, bool>> predicate, Expression<Func<AudioMetadata, T>> selector, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetPropertyAsync(database.AudioMetadata, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetAudioMetadataPropertyOrDefaultAsync<T>(Guid resourceId, Expression<Func<AudioMetadata, T>> selector, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending)
        { 
            orderBy ??= audioDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.AudioMetadata, a => a.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetAudioMetadataPropertyOrDefaultAsync<T>(string resourceId, Expression<Func<AudioMetadata, T>> selector, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending)
        { return await GetAudioMetadataPropertyOrDefaultAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetAudioMetadataPropertyOrDefaultAsync<T>(Expression<Func<AudioMetadata, bool>> predicate, Expression<Func<AudioMetadata, T>> selector, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending)
        {
            orderBy ??= audioDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.AudioMetadata, predicate, selector, orderBy, orderDescending);
        }

        #endregion




        #region Video

        private readonly Expression<Func<VideoMetadata, object>>? videoDefaultOrderBy = null;
        private const bool videoDefaultOrderDescending = false;


        // Single
        public async Task<VideoMetadata?> GetVideoMetadataAsync(Guid resourceId)
        { return await GetAsync(database.VideoMetadata, v => v.ResourceId == resourceId); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(string resourceId)
        { return await GetVideoMetadataAsync(Guid.Parse(resourceId)); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Expression<Func<VideoMetadata, bool>> predicate, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetAsync(database.VideoMetadata, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Guid resourceId, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params string[] includeProperties)
        { return await GetVideoMetadataAsync(v => v.ResourceId == resourceId, orderBy, orderDescending, includeProperties); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(string resourceId, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params string[] includeProperties)
        { return await GetVideoMetadataAsync(Guid.Parse(resourceId), orderBy, orderDescending, includeProperties); }



        public async Task<VideoMetadata?> GetVideoMetadataAsync(Expression<Func<VideoMetadata, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.VideoMetadata, predicate, videoDefaultOrderBy, videoDefaultOrderDescending, includeProperties); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Guid resourceId, params string[] includeProperties)
        { return await GetVideoMetadataAsync(v => v.ResourceId == resourceId, videoDefaultOrderBy, videoDefaultOrderDescending, includeProperties); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(string resourceId, params string[] includeProperties)
        { return await GetVideoMetadataAsync(Guid.Parse(resourceId), videoDefaultOrderBy, videoDefaultOrderDescending, includeProperties); }



        // Multiple
        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetAllAsync(database.VideoMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(Expression<Func<VideoMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.VideoMetadata, null, false, predicate, includeProperties); }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.VideoMetadata, orderBy, orderDescending, null, includeProperties); }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(params string[] includeProperties)
        { return await GetAllAsync(database.VideoMetadata, videoDefaultOrderBy, videoDefaultOrderDescending, null, includeProperties); }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, videoDefaultOrderBy, videoDefaultOrderDescending, predicate, includeProperties); }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, videoDefaultOrderBy, videoDefaultOrderDescending, null, includeProperties); }

        
        // Multiple with projection
        public async Task<TResult[]> GetAllVideoMetadatasAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetAllAsync(database.VideoMetadata, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllVideoMetadatasAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, Expression<Func<VideoMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.VideoMetadata, projection, videoDefaultOrderBy, videoDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllVideoMetadatasAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetAllAsync(database.VideoMetadata, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllVideoMetadatasAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.VideoMetadata, projection, videoDefaultOrderBy, videoDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetVideoMetadataPageAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetPageAsync(database.VideoMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetVideoMetadataPageAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.VideoMetadata, projection, pageIndex, pageSize, videoDefaultOrderBy, videoDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetVideoMetadataPageAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetPageAsync(database.VideoMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetVideoMetadataPageAsync<TResult>(Expression<Func<VideoMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.VideoMetadata, projection, pageIndex, pageSize, videoDefaultOrderBy, videoDefaultOrderDescending, null, includeProperties); }


        // Property

        public async Task<T> GetVideoMetadataPropertyAsync<T>(Guid resourceId, Expression<Func<VideoMetadata, T>> selector, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending)
        { 
            orderBy ??= videoDefaultOrderBy;
            return await GetPropertyAsync(database.VideoMetadata, v => v.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetVideoMetadataPropertyAsync<T>(string resourceId, Expression<Func<VideoMetadata, T>> selector, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending)
        { return await GetVideoMetadataPropertyAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T> GetVideoMetadataPropertyAsync<T>(Expression<Func<VideoMetadata, bool>> predicate, Expression<Func<VideoMetadata, T>> selector, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetPropertyAsync(database.VideoMetadata, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetVideoMetadataPropertyOrDefaultAsync<T>(Guid resourceId, Expression<Func<VideoMetadata, T>> selector, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending)
        { 
            orderBy ??= videoDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.VideoMetadata, v => v.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetVideoMetadataPropertyOrDefaultAsync<T>(string resourceId, Expression<Func<VideoMetadata, T>> selector, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending)
        { return await GetVideoMetadataPropertyOrDefaultAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetVideoMetadataPropertyOrDefaultAsync<T>(Expression<Func<VideoMetadata, bool>> predicate, Expression<Func<VideoMetadata, T>> selector, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending)
        {
            orderBy ??= videoDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.VideoMetadata, predicate, selector, orderBy, orderDescending);
        }

        #endregion




        #region Website

        private readonly Expression<Func<WebsiteMetadata, object>>? websiteDefaultOrderBy = null;
        private const bool websiteDefaultOrderDescending = true;


        // Single
        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Guid resourceId)
        { return await GetAsync(database.WebsiteMetadata, w => w.ResourceId == resourceId); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(string resourceId)
        { return await GetWebsiteMetadataAsync(Guid.Parse(resourceId)); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Expression<Func<WebsiteMetadata, bool>> predicate, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetAsync(database.WebsiteMetadata, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Guid resourceId, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params string[] includeProperties)
        { return await GetWebsiteMetadataAsync(w => w.ResourceId == resourceId, orderBy, orderDescending, includeProperties); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(string resourceId, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params string[] includeProperties)
        { return await GetWebsiteMetadataAsync(Guid.Parse(resourceId), orderBy, orderDescending, includeProperties); }



        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Expression<Func<WebsiteMetadata, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.WebsiteMetadata, predicate, websiteDefaultOrderBy, websiteDefaultOrderDescending, includeProperties); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Guid resourceId, params string[] includeProperties)
        { return await GetWebsiteMetadataAsync(w => w.ResourceId == resourceId, websiteDefaultOrderBy, websiteDefaultOrderDescending, includeProperties); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(string resourceId, params string[] includeProperties)
        { return await GetWebsiteMetadataAsync(Guid.Parse(resourceId), websiteDefaultOrderBy, websiteDefaultOrderDescending, includeProperties); }

        // Multiple
        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetAllAsync(database.WebsiteMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(Expression<Func<WebsiteMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.WebsiteMetadata, null, false, predicate, includeProperties); }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.WebsiteMetadata, orderBy, orderDescending, null, includeProperties); }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(params string[] includeProperties)
        { return await GetAllAsync(database.WebsiteMetadata, websiteDefaultOrderBy, websiteDefaultOrderDescending, null, includeProperties); }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, websiteDefaultOrderBy, websiteDefaultOrderDescending, predicate, includeProperties); }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, websiteDefaultOrderBy, websiteDefaultOrderDescending, null, includeProperties); }

        // Multiple with projection
        public async Task<TResult[]> GetAllWebsiteMetadatasAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetAllAsync(database.WebsiteMetadata, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllWebsiteMetadatasAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, Expression<Func<WebsiteMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.WebsiteMetadata, projection, websiteDefaultOrderBy, websiteDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllWebsiteMetadatasAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetAllAsync(database.WebsiteMetadata, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllWebsiteMetadatasAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.WebsiteMetadata, projection, websiteDefaultOrderBy, websiteDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetWebsiteMetadataPageAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetPageAsync(database.WebsiteMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetWebsiteMetadataPageAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.WebsiteMetadata, projection, pageIndex, pageSize, websiteDefaultOrderBy, websiteDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetWebsiteMetadataPageAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetPageAsync(database.WebsiteMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetWebsiteMetadataPageAsync<TResult>(Expression<Func<WebsiteMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.WebsiteMetadata, projection, pageIndex, pageSize, websiteDefaultOrderBy, websiteDefaultOrderDescending, null, includeProperties); }


        // Property

        public async Task<T> GetWebsiteMetadataPropertyAsync<T>(Guid resourceId, Expression<Func<WebsiteMetadata, T>> selector, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending)
        { 
            orderBy ??= websiteDefaultOrderBy;
            return await GetPropertyAsync(database.WebsiteMetadata, w => w.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetWebsiteMetadataPropertyAsync<T>(string resourceId, Expression<Func<WebsiteMetadata, T>> selector, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending)
        { return await GetWebsiteMetadataPropertyAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T> GetWebsiteMetadataPropertyAsync<T>(Expression<Func<WebsiteMetadata, bool>> predicate, Expression<Func<WebsiteMetadata, T>> selector, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetPropertyAsync(database.WebsiteMetadata, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetWebsiteMetadataPropertyOrDefaultAsync<T>(Guid resourceId, Expression<Func<WebsiteMetadata, T>> selector, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending)
        { 
            orderBy ??= websiteDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.WebsiteMetadata, w => w.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetWebsiteMetadataPropertyOrDefaultAsync<T>(string resourceId, Expression<Func<WebsiteMetadata, T>> selector, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending)
        { return await GetWebsiteMetadataPropertyOrDefaultAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetWebsiteMetadataPropertyOrDefaultAsync<T>(Expression<Func<WebsiteMetadata, bool>> predicate, Expression<Func<WebsiteMetadata, T>> selector, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending)
        {
            orderBy ??= websiteDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.WebsiteMetadata, predicate, selector, orderBy, orderDescending);
        }

        #endregion




        #region Document

        private readonly Expression<Func<DocumentMetadata, object>>? documentDefaultOrderBy = null;
        private const bool documentDefaultOrderDescending = false;


        // Single
        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid resourceId)
        { return await GetAsync(database.DocumentMetadata, d => d.ResourceId == resourceId); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(string resourceId)
        { return await GetDocumentMetadataAsync(Guid.Parse(resourceId)); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Expression<Func<DocumentMetadata, bool>> predicate, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetAsync(database.DocumentMetadata, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid resourceId, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params string[] includeProperties)
        { return await GetDocumentMetadataAsync(d => d.ResourceId == resourceId, orderBy, orderDescending, includeProperties); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(string resourceId, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params string[] includeProperties)
        { return await GetDocumentMetadataAsync(Guid.Parse(resourceId), orderBy, orderDescending, includeProperties); }



        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Expression<Func<DocumentMetadata, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.DocumentMetadata, predicate, documentDefaultOrderBy, documentDefaultOrderDescending, includeProperties); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid resourceId, params string[] includeProperties)
        { return await GetDocumentMetadataAsync(d => d.ResourceId == resourceId, documentDefaultOrderBy, documentDefaultOrderDescending, includeProperties); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(string resourceId, params string[] includeProperties)
        { return await GetDocumentMetadataAsync(Guid.Parse(resourceId), documentDefaultOrderBy, documentDefaultOrderDescending, includeProperties); }


        // Multiple
        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetAllAsync(database.DocumentMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(Expression<Func<DocumentMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.DocumentMetadata, null, false, predicate, includeProperties); }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.DocumentMetadata, orderBy, orderDescending, null, includeProperties); }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(params string[] includeProperties)
        { return await GetAllAsync(database.DocumentMetadata, documentDefaultOrderBy, documentDefaultOrderDescending, null, includeProperties); }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, documentDefaultOrderBy, documentDefaultOrderDescending, predicate, includeProperties); }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, documentDefaultOrderBy, documentDefaultOrderDescending, null, includeProperties); }

        
        // Multiple with projection
        public async Task<TResult[]> GetAllDocumentMetadataAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetAllAsync(database.DocumentMetadata, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllDocumentMetadataAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, Expression<Func<DocumentMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.DocumentMetadata, projection, documentDefaultOrderBy, documentDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllDocumentMetadataAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetAllAsync(database.DocumentMetadata, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllDocumentMetadataAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.DocumentMetadata, projection, documentDefaultOrderBy, documentDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetDocumentMetadataPageAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetPageAsync(database.DocumentMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetDocumentMetadataPageAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.DocumentMetadata, projection, pageIndex, pageSize, documentDefaultOrderBy, documentDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetDocumentMetadataPageAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetPageAsync(database.DocumentMetadata, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetDocumentMetadataPageAsync<TResult>(Expression<Func<DocumentMetadata, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.DocumentMetadata, projection, pageIndex, pageSize, documentDefaultOrderBy, documentDefaultOrderDescending, null, includeProperties); }


        // Property

        public async Task<T> GetDocumentMetadataPropertyAsync<T>(Guid resourceId, Expression<Func<DocumentMetadata, T>> selector, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending)
        { 
            orderBy ??= documentDefaultOrderBy;
            return await GetPropertyAsync(database.DocumentMetadata, d => d.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetDocumentMetadataPropertyAsync<T>(string resourceId, Expression<Func<DocumentMetadata, T>> selector, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending)
        { return await GetDocumentMetadataPropertyAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T> GetDocumentMetadataPropertyAsync<T>(Expression<Func<DocumentMetadata, bool>> predicate, Expression<Func<DocumentMetadata, T>> selector, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetPropertyAsync(database.DocumentMetadata, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetDocumentMetadataPropertyOrDefaultAsync<T>(Guid resourceId, Expression<Func<DocumentMetadata, T>> selector, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending)
        { 
            orderBy ??= documentDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.DocumentMetadata, d => d.ResourceId == resourceId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetDocumentMetadataPropertyOrDefaultAsync<T>(string resourceId, Expression<Func<DocumentMetadata, T>> selector, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending)
        { return await GetDocumentMetadataPropertyOrDefaultAsync(Guid.Parse(resourceId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetDocumentMetadataPropertyOrDefaultAsync<T>(Expression<Func<DocumentMetadata, bool>> predicate, Expression<Func<DocumentMetadata, T>> selector, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending)
        {
            orderBy ??= documentDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.DocumentMetadata, predicate, selector, orderBy, orderDescending);
        }

        #endregion




        #region Tag

        private readonly Expression<Func<Tag, object>> tagDefaultOrderBy = t => t.Name;
        private const bool tagDefaultOrderDescending = false;


        // Single
        public async Task<Tag?> GetTagAsync(Guid id)
        { return await GetAsync(database.Tags, t => t.Id == id); }

        public async Task<Tag?> GetTagAsync(string id)
        { return await GetTagAsync(Guid.Parse(id)); }

        public async Task<Tag?> GetTagAsync(Expression<Func<Tag, bool>> predicate, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetAsync(database.Tags, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<Tag?> GetTagAsync(Guid id, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, params string[] includeProperties)
        { return await GetTagAsync(t => t.Id == id, orderBy, orderDescending, includeProperties); }

        public async Task<Tag?> GetTagAsync(string id, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, params string[] includeProperties)
        { return await GetTagAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties); }



        public async Task<Tag?> GetTagAsync(Expression<Func<Tag, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.Tags, predicate, tagDefaultOrderBy, tagDefaultOrderDescending, includeProperties); }

        public async Task<Tag?> GetTagAsync(Guid id, params string[] includeProperties)
        { return await GetTagAsync(t => t.Id == id, tagDefaultOrderBy, tagDefaultOrderDescending, includeProperties); }

        public async Task<Tag?> GetTagAsync(string id, params string[] includeProperties)
        { return await GetTagAsync(Guid.Parse(id), tagDefaultOrderBy, tagDefaultOrderDescending, includeProperties); }


        // Multiple
        public async Task<Tag[]> GetAllTagsAsync(Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, Expression<Func<Tag, bool>>? predicate = null)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetAllAsync(database.Tags, orderBy, orderDescending, predicate);
        }

        public async Task<Tag[]> GetAllTagsAsync(Expression<Func<Tag, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Tags, null, false, predicate, includeProperties); }

        public async Task<Tag[]> GetAllTagsAsync(Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.Tags, orderBy, orderDescending, null, includeProperties); }

        public async Task<Tag[]> GetAllTagsAsync(params string[] includeProperties)
        { return await GetAllAsync(database.Tags, tagDefaultOrderBy, tagDefaultOrderDescending, null, includeProperties); }

        public async Task<Tag[]> GetTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, Expression<Func<Tag, bool>>? predicate = null)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetPageAsync(database.Tags, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Tag[]> GetTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Tag, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Tags, pageIndex, pageSize, tagDefaultOrderBy, tagDefaultOrderDescending, predicate, includeProperties); }

        public async Task<Tag[]> GetTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetPageAsync(database.Tags, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<Tag[]> GetTagPageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Tags, pageIndex, pageSize, tagDefaultOrderBy, tagDefaultOrderDescending, null, includeProperties); }

        
        // Multiple with projection
        public async Task<TResult[]> GetAllTagsAsync<TResult>(Expression<Func<Tag, TResult>> projection, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, Expression<Func<Tag, bool>>? predicate = null)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetAllAsync(database.Tags, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllTagsAsync<TResult>(Expression<Func<Tag, TResult>> projection, Expression<Func<Tag, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.Tags, projection, tagDefaultOrderBy, tagDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllTagsAsync<TResult>(Expression<Func<Tag, TResult>> projection, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetAllAsync(database.Tags, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllTagsAsync<TResult>(Expression<Func<Tag, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.Tags, projection, tagDefaultOrderBy, tagDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetTagPageAsync<TResult>(Expression<Func<Tag, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, Expression<Func<Tag, bool>>? predicate = null)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetPageAsync(database.Tags, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetTagPageAsync<TResult>(Expression<Func<Tag, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Tag, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.Tags, projection, pageIndex, pageSize, tagDefaultOrderBy, tagDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetTagPageAsync<TResult>(Expression<Func<Tag, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetPageAsync(database.Tags, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetTagPageAsync<TResult>(Expression<Func<Tag, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.Tags, projection, pageIndex, pageSize, tagDefaultOrderBy, tagDefaultOrderDescending, null, includeProperties); }


        // Property

        public async Task<T> GetTagPropertyAsync<T>(Guid TagId, Expression<Func<Tag, T>> selector, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending)
        { 
            orderBy ??= tagDefaultOrderBy;
            return await GetPropertyAsync(database.Tags, t => t.Id == TagId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetTagPropertyAsync<T>(string TagId, Expression<Func<Tag, T>> selector, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending)
        { return await GetTagPropertyAsync(Guid.Parse(TagId), selector, orderBy, orderDescending); }
        
        public async Task<T> GetTagPropertyAsync<T>(Expression<Func<Tag, bool>> predicate, Expression<Func<Tag, T>> selector, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetPropertyAsync(database.Tags, predicate, selector, orderBy, orderDescending);
        }



        public async Task<T?> GetTagPropertyOrDefaultAsync<T>(Guid TagId, Expression<Func<Tag, T>> selector, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending)
        { 
            orderBy ??= tagDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Tags, t => t.Id == TagId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetTagPropertyOrDefaultAsync<T>(string TagId, Expression<Func<Tag, T>> selector, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending)
        { return await GetTagPropertyOrDefaultAsync(Guid.Parse(TagId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetTagPropertyOrDefaultAsync<T>(Expression<Func<Tag, bool>> predicate, Expression<Func<Tag, T>> selector, Expression<Func<Tag, object>>? orderBy = null, bool orderDescending = tagDefaultOrderDescending)
        {
            orderBy ??= tagDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.Tags, predicate, selector, orderBy, orderDescending);
        }

        #endregion




        #region Resource Type

        private readonly Expression<Func<ResourceType, object>> resourceTypeDefaultOrderBy = rt => rt.Name;
        private const bool resourceTypeDefaultOrderDescending = false;


        // Single
        public async Task<ResourceType?> GetResourceTypeAsync(Guid id)
        { return await GetAsync(database.ResourceTypes, rt => rt.Id == id); }

        public async Task<ResourceType?> GetResourceTypeAsync(string id)
        { return await GetResourceTypeAsync(Guid.Parse(id)); }

        public async Task<ResourceType?> GetResourceTypeAsync(Expression<Func<ResourceType, bool>> predicate, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetAsync(database.ResourceTypes, predicate, orderBy, orderDescending, includeProperties);
        }

        public async Task<ResourceType?> GetResourceTypeAsync(Guid id, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params string[] includeProperties)
        { return await GetResourceTypeAsync(rt => rt.Id == id, orderBy, orderDescending, includeProperties); }

        public async Task<ResourceType?> GetResourceTypeAsync(string id, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params string[] includeProperties)
        { return await GetResourceTypeAsync(Guid.Parse(id), orderBy, orderDescending, includeProperties); }



        public async Task<ResourceType?> GetResourceTypeAsync(Expression<Func<ResourceType, bool>> predicate, params string[] includeProperties)
        { return await GetAsync(database.ResourceTypes, predicate, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, includeProperties); }

        public async Task<ResourceType?> GetResourceTypeAsync(Guid id, params string[] includeProperties)
        { return await GetResourceTypeAsync(rt => rt.Id == id, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, includeProperties); }

        public async Task<ResourceType?> GetResourceTypeAsync(string id, params string[] includeProperties)
        { return await GetResourceTypeAsync(Guid.Parse(id), resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, includeProperties); }



        // Multiple
        public async Task<ResourceType[]> GetAllResourceTypesAsync(Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetAllAsync(database.ResourceTypes, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(Expression<Func<ResourceType, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTypes, null, false, predicate, includeProperties); }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTypes, orderBy, orderDescending, null, includeProperties); }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTypes, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, null, includeProperties); }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, predicate, includeProperties); }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, null, includeProperties); }

        
        // Multiple with projection
        public async Task<TResult[]> GetAllResourceTypesAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetAllAsync(database.ResourceTypes, projection, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetAllResourceTypesAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, Expression<Func<ResourceType, bool>>? predicate = null, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTypes, projection, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetAllResourceTypesAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetAllAsync(database.ResourceTypes, projection, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetAllResourceTypesAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, params string[] includeProperties)
        { return await GetAllAsync(database.ResourceTypes, projection, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, null, includeProperties); }

        public async Task<TResult[]> GetResourceTypePageAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPageAsync(database.ResourceTypes, projection, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<TResult[]> GetResourceTypePageAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, bool>>? predicate = null, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTypes, projection, pageIndex, pageSize, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, predicate, includeProperties); }

        public async Task<TResult[]> GetResourceTypePageAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params string[] includeProperties)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPageAsync(database.ResourceTypes, projection, pageIndex, pageSize, orderBy, orderDescending, null, includeProperties);
        }

        public async Task<TResult[]> GetResourceTypePageAsync<TResult>(Expression<Func<ResourceType, TResult>> projection, int pageIndex = 1, int pageSize = 100, params string[] includeProperties)
        { return await GetPageAsync(database.ResourceTypes, projection, pageIndex, pageSize, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, null, includeProperties); }



        // Property

        public async Task<T> GetResourceTypePropertyAsync<T>(Guid resourceTypeId, Expression<Func<ResourceType, T>> selector, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending)
        { 
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceTypes, rt => rt.Id == resourceTypeId, selector, orderBy, orderDescending);
        }

        public async Task<T> GetResourceTypePropertyAsync<T>(string resourceTypeId, Expression<Func<ResourceType, T>> selector, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending)
        { return await GetResourceTypePropertyAsync(Guid.Parse(resourceTypeId), selector, orderBy, orderDescending); }
        
        public async Task<T> GetResourceTypePropertyAsync<T>(Expression<Func<ResourceType, bool>> predicate, Expression<Func<ResourceType, T>> selector, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending)
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPropertyAsync(database.ResourceTypes, predicate, selector, orderBy, orderDescending);
        }




        public async Task<T?> GetResourceTypePropertyOrDefaultAsync<T>(Guid resourceTypeId, Expression<Func<ResourceType, T>> selector, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending)
        { 
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceTypes, rt => rt.Id == resourceTypeId, selector, orderBy, orderDescending);
        }

        public async Task<T?> GetResourceTypePropertyOrDefaultAsync<T>(string resourceTypeId, Expression<Func<ResourceType, T>> selector, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending)
        { return await GetResourceTypePropertyOrDefaultAsync(Guid.Parse(resourceTypeId), selector, orderBy, orderDescending); }
        
        public async Task<T?> GetResourceTypePropertyOrDefaultAsync<T>(Expression<Func<ResourceType, bool>> predicate, Expression<Func<ResourceType, T>> selector, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending) 
        {
            orderBy ??= resourceTypeDefaultOrderBy;
            return await GetPropertyOrDefaultAsync(database.ResourceTypes, predicate, selector, orderBy, orderDescending);
        }

        #endregion
    }
}
