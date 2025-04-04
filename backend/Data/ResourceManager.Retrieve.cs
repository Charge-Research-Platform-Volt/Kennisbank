using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend.Data
{
    // This part is for retrieving resources or their properties
    public partial class ResourceManager
    {
        // Generic functions:
        protected async Task<T?> GetAsync<T>(Guid id, DbSet<T> dbSet) where T : class
        { return await dbSet.FindAsync(id); }

        protected async Task<T?> GetAsync<T>(string id, DbSet<T> dbSet) where T : class
        { return await GetAsync(Guid.Parse(id), dbSet); }

        protected async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null) where T : class
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
            {
                if (orderDescending)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }

            return await query.AsNoTracking().ToArrayAsync();
        }

        protected async Task<T[]> GetPageAsync<T>(DbSet<T> dbSet, int pageIndex = 1, int pageSize = 100, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null) where T : class
        {
            // Return empty for invalid input
            if (pageIndex < 1 || pageSize < 1) return Array.Empty<T>();

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            IQueryable<T> query = dbSet.AsQueryable();

            if (predicate != null)
                query = query.Where(predicate);

            if (orderBy != null)
            {
                if (orderDescending)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }

            return await query.Skip(skip).Take(pageSize).AsNoTracking().ToArrayAsync();
        }

        protected async Task<T?> GetFirstWhereAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Expression<Func<T, object>>? orderBy, bool orderDescending = false) where T : class
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (orderBy != null)
            {
                if (orderDescending)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }
            
            return await query.Where(predicate).AsNoTracking().FirstOrDefaultAsync();
        }

        protected async Task<T[]> GetAllWhereAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Expression<Func<T, object>>? orderBy, bool orderDescending = false) where T : class
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (orderBy != null)
            {
                if (orderDescending)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }

            return await query.Where(predicate).AsNoTracking().ToArrayAsync();
        }

        protected async Task<T[]> GetAllWherePageAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, int pageIndex = 1, int pageSize = 1, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false) where T : class
        { return await GetPageAsync(dbSet, pageIndex, pageSize, orderBy, orderDescending, predicate); }

        protected async Task<TResult> GetPropertyAsync<TSet, TResult>(DbSet<TSet> dbSet, Expression<Func<TSet, bool>> predicate, Expression<Func<TSet, TResult>> selector) where TSet : class
        { return await dbSet.Where(predicate).Select(selector).FirstAsync(); }

        protected async Task<TResult?> GetPropertyOrDefaultAsync<TSet, TResult>(DbSet<TSet> dbSet, Expression<Func<TSet, bool>> predicate, Expression<Func<TSet, TResult>> selector) where TSet : class
        { return await dbSet.Where(predicate).Select(selector).FirstOrDefaultAsync(); }



        // --------------------------------



        #region Resource

        // Resource itself

        private readonly Expression<Func<Resource, object>> resourceDefaultOrderBy = resource => resource.CreationDate;
        private const bool resourceDefaultOrderDescending = true;

        public async Task<Resource?> GetResourceAsync(Guid id)
        { return await GetAsync(id, database.Resources); }

        public async Task<Resource?> GetResourceAsync(string id)
        { return await GetAsync(id, database.Resources); }

        public async Task<Resource[]> GetAllResourcesAsync(Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceDefaultOrderBy;

            return await GetAllAsync(database.Resources, orderBy, orderDescending, predicate);
        }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceDefaultOrderBy;

            return await GetPageAsync(database.Resources, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        // Filetype

        public async Task<string> GetResourceFileTypeAsync(Guid resourceId)
        { return await GetPropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.FileType); }

        public async Task<string> GetResourceFileTypeAsync(string resourceId)
        { return await GetResourceFileTypeAsync(Guid.Parse(resourceId)); }

        // Title

        public async Task<string> GetResourceTitleAsync(Guid resourceId)
        { return await GetPropertyAsync(database.Resources, resource => resource.Id == resourceId, resource => resource.Title); }

        public async Task<string> GetResourceTitleAsync(string resourceId)
        { return await GetResourceTitleAsync(Guid.Parse(resourceId)); }

        #endregion

        #region Person

        private readonly Expression<Func<Person, object>> personDefaultOrderBy = person => person.Name;
        private const bool personDefaultOrderDescending = false;

        public async Task<Person?> GetPersonAsync(Guid id)
        { return await GetAsync(id, database.Persons); }

        public async Task<Person?> GetPersonAsync(string id)
        { return await GetAsync(id, database.Persons); }

        public async Task<Person[]> GetAllPersonsAsync(Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = personDefaultOrderBy;

            return await GetAllAsync(database.Persons, orderBy, orderDescending, predicate);
        }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = personDefaultOrderBy;

            return await GetPageAsync(database.Persons, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region Organisation

        private readonly Expression<Func<Organisation, object>> organisationDefaultOrderBy = organisation => organisation.Name;
        private const bool organisationDefaultOrderDescending = false;

        public async Task<Organisation?> GetOrganisationAsync(Guid id)
        { return await GetAsync(id, database.Organisations); }

        public async Task<Organisation?> GetOrganisationAsync(string id)
        { return await GetAsync(id, database.Organisations); }

        public async Task<Organisation[]> GetAllOrganisationsAsync(Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = organisationDefaultOrderBy;

            return await GetAllAsync(database.Organisations, orderBy, orderDescending, predicate);
        }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = organisationDefaultOrderBy;

            return await GetPageAsync(database.Organisations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region Region

        private readonly Expression<Func<Region, object>> regionDefaultOrderBy = r => r.Name;
        private const bool regionDefaultOrderDescending = false;

        public async Task<Region?> GetRegionAsync(Guid id)
        { return await GetAsync(id, database.Regions); }

        public async Task<Region?> GetRegionAsync(string id)
        { return await GetAsync(id, database.Regions); }

        public async Task<Region[]> GetAllRegionsAsync(Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = regionDefaultOrderBy;

            return await GetAllAsync(database.Regions, orderBy, orderDescending, predicate);
        }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = regionDefaultOrderBy;

            return await GetPageAsync(database.Regions, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region Audio

        private readonly Expression<Func<AudioMetadata, object>> audioDefaultOrderBy = a => a.Length;
        private const bool audioDefaultOrderDescending = false;

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Guid id)
        { return await GetAsync(id, database.AudioMetadata); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(string id)
        { return await GetAsync(id, database.AudioMetadata); }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = audioDefaultOrderBy;
            
            return await GetAllAsync(database.AudioMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = audioDefaultOrderBy;

            return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region Video

        private readonly Expression<Func<VideoMetadata, object>> videoDefaultOrderBy = v => v.Length;
        private const bool videoDefaultOrderDescending = false;

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Guid id)
        { return await GetAsync(id, database.VideoMetadata); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(string id)
        { return await GetAsync(id, database.VideoMetadata); }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = videoDefaultOrderBy;
            
            return await GetAllAsync(database.VideoMetadata, orderBy, orderDescending, predicate); 
        }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = videoDefaultOrderBy;

            return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region Website

        private readonly Expression<Func<WebsiteMetadata, object>> websiteDefaultOrderBy = w => w.AccessedOn;
        private const bool websiteDefaultOrderDescending = true;

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Guid id)
        { return await GetAsync(id, database.WebsiteMetadata); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(string id)
        { return await GetAsync(id, database.WebsiteMetadata); }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = websiteDefaultOrderBy;

            return await GetAllAsync(database.WebsiteMetadata, orderBy, orderDescending, predicate); 
        }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = websiteDefaultOrderBy;
            
            return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region Document

        private readonly Expression<Func<DocumentMetadata, object>> documentDefaultOrderBy = d => d.Abstract;
        private const bool documentDefaultOrderDescending = false;

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid id)
        { return await GetAsync(id, database.DocumentMetadata); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(string id)
        { return await GetDocumentMetadataAsync(Guid.Parse(id)); }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = documentDefaultOrderBy;
            
            return await GetAllAsync(database.DocumentMetadata, orderBy, orderDescending, predicate); 
        }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = documentDefaultOrderBy;
            
            return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region User Tag

        private readonly Expression<Func<UserTag, object>> userTagDefaultOrderBy = ut => ut.Name;
        private const bool userTagDefaultOrderDescending = false;

        public async Task<UserTag?> GetUserTagAsync(Guid id)
        { return await GetAsync(id, database.UserTags); }

        public async Task<UserTag?> GetUserTagAsync(string id)
        { return await GetAsync(id, database.UserTags); }

        public async Task<UserTag[]> GetAllUserTagsAsync(Expression<Func<UserTag, object>>? orderBy = null, bool orderDescending = userTagDefaultOrderDescending, Expression<Func<UserTag, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = userTagDefaultOrderBy;

            return await GetAllAsync(database.UserTags, orderBy, orderDescending, predicate);
        }

        public async Task<UserTag[]> GetUserTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<UserTag, object>>? orderBy = null, bool orderDescending = userTagDefaultOrderDescending, Expression<Func<UserTag, bool>>? predicate = null)
        { return await GetPageAsync(database.UserTags, pageIndex, pageSize, orderBy, orderDescending, predicate); }

        #endregion

        #region Admin Tag

        private readonly Expression<Func<AdminTag, object>> adminTagDefaultOrderBy = at => at.Name;
        private const bool adminTagDefaultOrderDescending = false;

        public async Task<AdminTag?> GetAdminTagAsync(Guid id)
        { return await GetAsync(id, database.AdminTags); }

        public async Task<AdminTag?> GetAdminTagAsync(string id)
        { return await GetAsync(id, database.AdminTags); }

        public async Task<AdminTag[]> GetAllAdminTagsAsync(Expression<Func<AdminTag, object>>? orderBy = null, bool orderDescending = adminTagDefaultOrderDescending, Expression<Func<AdminTag, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = adminTagDefaultOrderBy;
            
            return await GetAllAsync(database.AdminTags, orderBy, orderDescending, predicate);
        }

        public async Task<AdminTag[]> GetAdminTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AdminTag, object>>? orderBy = null, bool orderDescending = adminTagDefaultOrderDescending, Expression<Func<AdminTag, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = adminTagDefaultOrderBy;
            
            return await GetPageAsync(database.AdminTags, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion

        #region Resource Type

        private readonly Expression<Func<ResourceType, object>> resourceTypeDefaultOrderBy = rt => rt.Name;
        private const bool resourceTypeDefaultOrderDescending = false;

        public async Task<ResourceType?> GetResourceTypeAsync(Guid id)
        { return await GetAsync(id, database.ResourceTypes); }

        public async Task<ResourceType?> GetResourceTypeAsync(string id)
        { return await GetAsync(id, database.ResourceTypes); }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceTypeDefaultOrderBy;
            
            return await GetAllAsync(database.ResourceTypes, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceTypeDefaultOrderBy;

            return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        #endregion
    }
}
