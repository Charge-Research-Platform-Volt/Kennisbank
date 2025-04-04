using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend.Data
{
    // This part is for retrieving resources or their properties
    public partial class ResourceManager
    {
        // Generic functions:
        protected async Task<T?> GetAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, params Expression<Func<T, object>>[] includes) where T : class
        {
            IQueryable<T> query = dbSet.AsQueryable();

            if (orderBy != null)
            {
                if (orderDescending)
                    query = query.OrderByDescending(orderBy);
                else
                    query = query.OrderBy(orderBy);
            }

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.Where(predicate).AsNoTracking().FirstOrDefaultAsync();
        }

        protected async Task<T?> GetAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes) where T : class
        { return await GetAsync(dbSet, predicate, null, false, includes); }

        protected async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includes) where T : class
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

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.AsNoTracking().ToArrayAsync();
        }

        protected async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includes) where T : class
        { return await GetAllAsync(dbSet, null, false, predicate, includes); }

        protected async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, params Expression<Func<T, object>>[] includes) where T : class
        { return await GetAllAsync(dbSet, orderBy, orderDescending, null, includes); }

        protected async Task<T[]> GetAllAsync<T>(DbSet<T> dbSet, params Expression<Func<T, object>>[] includes) where T : class
        { return await GetAllAsync(dbSet, null, false, null, includes); }

        protected async Task<T[]> GetPageAsync<T>(DbSet<T> dbSet, int pageIndex = 1, int pageSize = 100, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includes) where T : class
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

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.Skip(skip).Take(pageSize).AsNoTracking().ToArrayAsync();
        }

        protected async Task<T[]> GetPageAsync<T>(DbSet<T> dbSet, int pageIndex = 1, int pageSize = 100, Expression<Func<T, bool>>? predicate = null, params Expression<Func<T, object>>[] includes) where T : class
        { return await GetPageAsync(dbSet, pageIndex, pageSize, null, false, predicate, includes); }

        protected async Task<T[]> GetPageAsync<T>(DbSet<T> dbSet, int pageIndex = 1, int pageSize = 100, Expression<Func<T, object>>? orderBy = null, bool orderDescending = false, params Expression<Func<T, object>>[] includes) where T : class
        { return await GetPageAsync(dbSet, pageIndex, pageSize, orderBy, orderDescending, null, includes); }

        protected async Task<T[]> GetPageAsync<T>(DbSet<T> dbSet, int pageIndex = 1, int pageSize = 100, params Expression<Func<T, object>>[] includes) where T : class
        { return await GetPageAsync(dbSet, pageIndex, pageSize, null, false, null, includes); }

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
        { return await GetAsync(database.Resources, r => r.Id == id); }

        public async Task<Resource?> GetResourceAsync(string id)
        { return await GetResourceAsync(Guid.Parse(id)); }

        public async Task<Resource?> GetResourceAsync(Expression<Func<Resource, bool>> predicate, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params Expression<Func<Resource, object>>[] includes)
        {
            if (orderBy == null) orderBy = resourceDefaultOrderBy;

            return await GetAsync(database.Resources, predicate, orderBy, orderDescending); 
        }

        public async Task<Resource?> GetResourceAsync(Expression<Func<Resource, bool>> predicate, params Expression<Func<Resource, object>>[] includes)
        { return await GetAsync(database.Resources, predicate, resourceDefaultOrderBy, resourceDefaultOrderDescending, includes); }

        public async Task<Resource[]> GetAllResourcesAsync(Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceDefaultOrderBy;

            return await GetAllAsync(database.Resources, orderBy, orderDescending, predicate);
        }

        public async Task<Resource[]> GetAllResourcesAsync(Expression<Func<Resource, bool>>? predicate = null, params Expression<Func<Resource, object>>[] includes)
        { return await GetAllAsync(database.Resources, resourceDefaultOrderBy, resourceDefaultOrderDescending, predicate, includes); }

        public async Task<Resource[]> GetAllResourcesAsync(Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params Expression<Func<Resource, object>>[] includes)
        { return await GetAllAsync(database.Resources, orderBy, orderDescending, null, includes); }

        public async Task<Resource[]> GetAllResourcesAsync(params Expression<Func<Resource, object>>[] includes)
        { return await GetAllAsync(database.Resources, resourceDefaultOrderBy, resourceDefaultOrderDescending, null, includes); }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, Expression<Func<Resource, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceDefaultOrderBy;

            return await GetPageAsync(database.Resources, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, bool>>? predicate = null, params Expression<Func<Resource, object>>[] includes)
        { return await GetPageAsync(database.Resources, pageIndex, pageSize, resourceDefaultOrderBy, regionDefaultOrderDescending, predicate, includes); }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Resource, object>>? orderBy = null, bool orderDescending = resourceDefaultOrderDescending, params Expression<Func<Resource, object>>[] includes)
        {
            if (orderBy == null) orderBy = resourceDefaultOrderBy;
            return await GetPageAsync(database.Resources, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<Resource, object>>[] includes)
        { return await GetPageAsync(database.Resources, pageIndex, pageSize, resourceDefaultOrderBy, resourceDefaultOrderDescending, null, includes); }

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
        { return await GetAsync(database.Persons, p => p.Id == id); }

        public async Task<Person?> GetPersonAsync(string id)
        { return await GetPersonAsync(Guid.Parse(id)); }

        public async Task<Person?> GetPersonAsync(Expression<Func<Person, bool>> predicate, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params Expression<Func<Person, object>>[] includes)
        {
            if (orderBy == null) orderBy = personDefaultOrderBy;

            return await GetAsync(database.Persons, predicate, orderBy, orderDescending);
        }

        public async Task<Person?> GetPersonAsync(Expression<Func<Person, bool>> predicate, params Expression<Func<Person, object>>[] includes)
        { return await GetAsync(database.Persons, predicate, personDefaultOrderBy, personDefaultOrderDescending, includes); }

        public async Task<Person[]> GetAllPersonsAsync(Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = personDefaultOrderBy;

            return await GetAllAsync(database.Persons, orderBy, orderDescending, predicate);
        }

        public async Task<Person[]> GetAllPersonsAsync(Expression<Func<Person, bool>>? predicate = null, params Expression<Func<Person, object>>[] includes)
        { return await GetAllAsync(database.Persons, null, false, predicate, includes); }

        public async Task<Person[]> GetAllPersonsAsync(Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params Expression<Func<Person, object>>[] includes)
        { return await GetAllAsync(database.Persons, orderBy, orderDescending, null, includes); }

        public async Task<Person[]> GetAllPersonsAsync(params Expression<Func<Person, object>>[] includes)
        { return await GetAllAsync(database.Persons, personDefaultOrderBy, personDefaultOrderDescending, null, includes); }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, Expression<Func<Person, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = personDefaultOrderBy;

            return await GetPageAsync(database.Persons, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Person, bool>>? predicate = null, params Expression<Func<Person, object>>[] includes)
        { return await GetPageAsync(database.Persons, pageIndex, pageSize, personDefaultOrderBy, personDefaultOrderDescending, predicate, includes); }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Person, object>>? orderBy = null, bool orderDescending = personDefaultOrderDescending, params Expression<Func<Person, object>>[] includes)
        {
            if (orderBy == null) orderBy = personDefaultOrderBy;
            return await GetPageAsync(database.Persons, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<Person, object>>[] includes)
        { return await GetPageAsync(database.Persons, pageIndex, pageSize, personDefaultOrderBy, personDefaultOrderDescending, null, includes); }

        #endregion



        #region Organisation

        private readonly Expression<Func<Organisation, object>> organisationDefaultOrderBy = organisation => organisation.Name;
        private const bool organisationDefaultOrderDescending = false;

        public async Task<Organisation?> GetOrganisationAsync(Guid id)
        { return await GetAsync(database.Organisations, o => o.Id == id); }

        public async Task<Organisation?> GetOrganisationAsync(string id)
        { return await GetOrganisationAsync(Guid.Parse(id)); }

        public async Task<Organisation?> GetOrganisationAsync(Expression<Func<Organisation, bool>> predicate, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params Expression<Func<Organisation, object>>[] includes)
        {
            if (orderBy == null) orderBy = organisationDefaultOrderBy;
            return await GetAsync(database.Organisations, predicate, orderBy, orderDescending);
        }

        public async Task<Organisation?> GetOrganisationAsync(Expression<Func<Organisation, bool>> predicate, params Expression<Func<Organisation, object>>[] includes)
        { return await GetAsync(database.Organisations, predicate, organisationDefaultOrderBy, organisationDefaultOrderDescending, includes); }

        public async Task<Organisation[]> GetAllOrganisationsAsync(Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = organisationDefaultOrderBy;
            return await GetAllAsync(database.Organisations, orderBy, orderDescending, predicate);
        }

        public async Task<Organisation[]> GetAllOrganisationsAsync(Expression<Func<Organisation, bool>>? predicate = null, params Expression<Func<Organisation, object>>[] includes)
        { return await GetAllAsync(database.Organisations, null, false, predicate, includes); }

        public async Task<Organisation[]> GetAllOrganisationsAsync(Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params Expression<Func<Organisation, object>>[] includes)
        { return await GetAllAsync(database.Organisations, orderBy, orderDescending, null, includes); }

        public async Task<Organisation[]> GetAllOrganisationsAsync(params Expression<Func<Organisation, object>>[] includes)
        { return await GetAllAsync(database.Organisations, organisationDefaultOrderBy, organisationDefaultOrderDescending, null, includes); }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, Expression<Func<Organisation, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = organisationDefaultOrderBy;
            return await GetPageAsync(database.Organisations, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, bool>>? predicate = null, params Expression<Func<Organisation, object>>[] includes)
        { return await GetPageAsync(database.Organisations, pageIndex, pageSize, organisationDefaultOrderBy, organisationDefaultOrderDescending, predicate, includes); }
        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Organisation, object>>? orderBy = null, bool orderDescending = organisationDefaultOrderDescending, params Expression<Func<Organisation, object>>[] includes)
        {
            if (orderBy == null) orderBy = organisationDefaultOrderBy;
            return await GetPageAsync(database.Organisations, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<Organisation, object>>[] includes)
        { return await GetPageAsync(database.Organisations, pageIndex, pageSize, organisationDefaultOrderBy, organisationDefaultOrderDescending, null, includes); }

        #endregion

        #region Region

        private readonly Expression<Func<Region, object>> regionDefaultOrderBy = r => r.Name;
        private const bool regionDefaultOrderDescending = false;

        public async Task<Region?> GetRegionAsync(Guid id)
        { return await GetAsync(database.Regions, r => r.Id == id); }

        public async Task<Region?> GetRegionAsync(string id)
        { return await GetRegionAsync(Guid.Parse(id)); }

        public async Task<Region?> GetRegionAsync(Expression<Func<Region, bool>> predicate, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params Expression<Func<Region, object>>[] includes)
        {
            if (orderBy == null) orderBy = regionDefaultOrderBy;
            return await GetAsync(database.Regions, predicate, orderBy, orderDescending);
        }

        public async Task<Region?> GetRegionAsync(Expression<Func<Region, bool>> predicate, params Expression<Func<Region, object>>[] includes)
        { return await GetAsync(database.Regions, predicate, regionDefaultOrderBy, regionDefaultOrderDescending, includes); }

        public async Task<Region[]> GetAllRegionsAsync(Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = regionDefaultOrderBy;
            return await GetAllAsync(database.Regions, orderBy, orderDescending, predicate);
        }

        public async Task<Region[]> GetAllRegionsAsync(Expression<Func<Region, bool>>? predicate = null, params Expression<Func<Region, object>>[] includes)
        { return await GetAllAsync(database.Regions, null, false, predicate, includes); }

        public async Task<Region[]> GetAllRegionsAsync(Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params Expression<Func<Region, object>>[] includes)
        { return await GetAllAsync(database.Regions, orderBy, orderDescending, null, includes); }

        public async Task<Region[]> GetAllRegionsAsync(params Expression<Func<Region, object>>[] includes)
        { return await GetAllAsync(database.Regions, regionDefaultOrderBy, regionDefaultOrderDescending, null, includes); }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, Expression<Func<Region, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = regionDefaultOrderBy;
            return await GetPageAsync(database.Regions, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Region, bool>>? predicate = null, params Expression<Func<Region, object>>[] includes)
        { return await GetPageAsync(database.Regions, pageIndex, pageSize, regionDefaultOrderBy, regionDefaultOrderDescending, predicate, includes); }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<Region, object>>? orderBy = null, bool orderDescending = regionDefaultOrderDescending, params Expression<Func<Region, object>>[] includes)
        {
            if (orderBy == null) orderBy = regionDefaultOrderBy;
            return await GetPageAsync(database.Regions, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<Region, object>>[] includes)
        { return await GetPageAsync(database.Regions, pageIndex, pageSize, regionDefaultOrderBy, regionDefaultOrderDescending, null, includes); }

        #endregion




        #region Audio

        private readonly Expression<Func<AudioMetadata, object>> audioDefaultOrderBy = a => a.Length;
        private const bool audioDefaultOrderDescending = false;

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Guid id)
        { return await GetAsync(database.AudioMetadata, a => a.Id == id); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(string id)
        { return await GetAudioMetadataAsync(Guid.Parse(id)); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Expression<Func<AudioMetadata, bool>> predicate, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params Expression<Func<AudioMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = audioDefaultOrderBy;
            return await GetAsync(database.AudioMetadata, predicate, orderBy, orderDescending);
        }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Expression<Func<AudioMetadata, bool>> predicate, params Expression<Func<AudioMetadata, object>>[] includes)
        { return await GetAsync(database.AudioMetadata, predicate, audioDefaultOrderBy, audioDefaultOrderDescending, includes); }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = audioDefaultOrderBy;
            return await GetAllAsync(database.AudioMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(Expression<Func<AudioMetadata, bool>>? predicate = null, params Expression<Func<AudioMetadata, object>>[] includes)
        { return await GetAllAsync(database.AudioMetadata, null, false, predicate, includes); }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params Expression<Func<AudioMetadata, object>>[] includes)
        { return await GetAllAsync(database.AudioMetadata, orderBy, orderDescending, null, includes); }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync(params Expression<Func<AudioMetadata, object>>[] includes)
        { return await GetAllAsync(database.AudioMetadata, audioDefaultOrderBy, audioDefaultOrderDescending, null, includes); }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, Expression<Func<AudioMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = audioDefaultOrderBy;
            return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, bool>>? predicate = null, params Expression<Func<AudioMetadata, object>>[] includes)
        { return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, audioDefaultOrderBy, audioDefaultOrderDescending, predicate, includes); }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AudioMetadata, object>>? orderBy = null, bool orderDescending = audioDefaultOrderDescending, params Expression<Func<AudioMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = audioDefaultOrderBy;
            return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<AudioMetadata, object>>[] includes)
        { return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize, audioDefaultOrderBy, audioDefaultOrderDescending, null, includes); }

        #endregion




        #region Video

        private readonly Expression<Func<VideoMetadata, object>> videoDefaultOrderBy = v => v.Length;
        private const bool videoDefaultOrderDescending = false;

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Guid id)
        { return await GetAsync(database.VideoMetadata, v => v.Id == id); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(string id)
        { return await GetVideoMetadataAsync(Guid.Parse(id)); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Expression<Func<VideoMetadata, bool>> predicate, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params Expression<Func<VideoMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = videoDefaultOrderBy;
            return await GetAsync(database.VideoMetadata, predicate, orderBy, orderDescending);
        }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Expression<Func<VideoMetadata, bool>> predicate, params Expression<Func<VideoMetadata, object>>[] includes)
        { return await GetAsync(database.VideoMetadata, predicate, videoDefaultOrderBy, videoDefaultOrderDescending, includes); }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = videoDefaultOrderBy;
            return await GetAllAsync(database.VideoMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(Expression<Func<VideoMetadata, bool>>? predicate = null, params Expression<Func<VideoMetadata, object>>[] includes)
        { return await GetAllAsync(database.VideoMetadata, null, false, predicate, includes); }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params Expression<Func<VideoMetadata, object>>[] includes)
        { return await GetAllAsync(database.VideoMetadata, orderBy, orderDescending, null, includes); }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync(params Expression<Func<VideoMetadata, object>>[] includes)
        { return await GetAllAsync(database.VideoMetadata, videoDefaultOrderBy, videoDefaultOrderDescending, null, includes); }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, Expression<Func<VideoMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = videoDefaultOrderBy;
            return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, bool>>? predicate = null, params Expression<Func<VideoMetadata, object>>[] includes)
        { return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, videoDefaultOrderBy, videoDefaultOrderDescending, predicate, includes); }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<VideoMetadata, object>>? orderBy = null, bool orderDescending = videoDefaultOrderDescending, params Expression<Func<VideoMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = videoDefaultOrderBy;
            return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<VideoMetadata, object>>[] includes)
        { return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize, videoDefaultOrderBy, videoDefaultOrderDescending, null, includes); }

        #endregion




        #region Website

        private readonly Expression<Func<WebsiteMetadata, object>> websiteDefaultOrderBy = w => w.AccessedOn;
        private const bool websiteDefaultOrderDescending = true;

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Guid id)
        { return await GetAsync(database.WebsiteMetadata, w => w.Id == id); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(string id)
        { return await GetWebsiteMetadataAsync(Guid.Parse(id)); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Expression<Func<WebsiteMetadata, bool>> predicate, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params Expression<Func<WebsiteMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = websiteDefaultOrderBy;
            return await GetAsync(database.WebsiteMetadata, predicate, orderBy, orderDescending);
        }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Expression<Func<WebsiteMetadata, bool>> predicate, params Expression<Func<WebsiteMetadata, object>>[] includes)
        { return await GetAsync(database.WebsiteMetadata, predicate, websiteDefaultOrderBy, websiteDefaultOrderDescending, includes); }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = websiteDefaultOrderBy;
            return await GetAllAsync(database.WebsiteMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(Expression<Func<WebsiteMetadata, bool>>? predicate = null, params Expression<Func<WebsiteMetadata, object>>[] includes)
        { return await GetAllAsync(database.WebsiteMetadata, null, false, predicate, includes); }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params Expression<Func<WebsiteMetadata, object>>[] includes)
        { return await GetAllAsync(database.WebsiteMetadata, orderBy, orderDescending, null, includes); }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync(params Expression<Func<WebsiteMetadata, object>>[] includes)
        { return await GetAllAsync(database.WebsiteMetadata, websiteDefaultOrderBy, websiteDefaultOrderDescending, null, includes); }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = websiteDefaultOrderBy;
            return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, bool>>? predicate = null, params Expression<Func<WebsiteMetadata, object>>[] includes)
        { return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, websiteDefaultOrderBy, websiteDefaultOrderDescending, predicate, includes); }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<WebsiteMetadata, object>>? orderBy = null, bool orderDescending = websiteDefaultOrderDescending, params Expression<Func<WebsiteMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = websiteDefaultOrderBy;
            return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<WebsiteMetadata, object>>[] includes)
        { return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize, websiteDefaultOrderBy, websiteDefaultOrderDescending, null, includes); }

        #endregion




        #region Document

        private readonly Expression<Func<DocumentMetadata, object>> documentDefaultOrderBy = d => d.Abstract;
        private const bool documentDefaultOrderDescending = false;

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Guid id)
        { return await GetAsync(database.DocumentMetadata, d => d.Id == id); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(string id)
        { return await GetDocumentMetadataAsync(Guid.Parse(id)); }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Expression<Func<DocumentMetadata, bool>> predicate, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params Expression<Func<DocumentMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = documentDefaultOrderBy;
            return await GetAsync(database.DocumentMetadata, predicate, orderBy, orderDescending);
        }

        public async Task<DocumentMetadata?> GetDocumentMetadataAsync(Expression<Func<DocumentMetadata, bool>> predicate, params Expression<Func<DocumentMetadata, object>>[] includes)
        { return await GetAsync(database.DocumentMetadata, predicate, documentDefaultOrderBy, documentDefaultOrderDescending, includes); }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = documentDefaultOrderBy;
            return await GetAllAsync(database.DocumentMetadata, orderBy, orderDescending, predicate);
        }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(Expression<Func<DocumentMetadata, bool>>? predicate = null, params Expression<Func<DocumentMetadata, object>>[] includes)
        { return await GetAllAsync(database.DocumentMetadata, null, false, predicate, includes); }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params Expression<Func<DocumentMetadata, object>>[] includes)
        { return await GetAllAsync(database.DocumentMetadata, orderBy, orderDescending, null, includes); }

        public async Task<DocumentMetadata[]> GetAllDocumentMetadataAsync(params Expression<Func<DocumentMetadata, object>>[] includes)
        { return await GetAllAsync(database.DocumentMetadata, documentDefaultOrderBy, documentDefaultOrderDescending, null, includes); }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, Expression<Func<DocumentMetadata, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = documentDefaultOrderBy;
            return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, bool>>? predicate = null, params Expression<Func<DocumentMetadata, object>>[] includes)
        { return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, documentDefaultOrderBy, documentDefaultOrderDescending, predicate, includes); }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<DocumentMetadata, object>>? orderBy = null, bool orderDescending = documentDefaultOrderDescending, params Expression<Func<DocumentMetadata, object>>[] includes)
        {
            if (orderBy == null) orderBy = documentDefaultOrderBy;
            return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<DocumentMetadata[]> GetDocumentMetadataPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<DocumentMetadata, object>>[] includes)
        { return await GetPageAsync(database.DocumentMetadata, pageIndex, pageSize, documentDefaultOrderBy, documentDefaultOrderDescending, null, includes); }

        #endregion




        #region User Tag

        private readonly Expression<Func<UserTag, object>> userTagDefaultOrderBy = ut => ut.Name;
        private const bool userTagDefaultOrderDescending = false;

        public async Task<UserTag?> GetUserTagAsync(Guid id)
        { return await GetAsync(database.UserTags, ut => ut.Id == id); }

        public async Task<UserTag?> GetUserTagAsync(string id)
        { return await GetUserTagAsync(Guid.Parse(id)); }

        public async Task<UserTag?> GetUserTagAsync(Expression<Func<UserTag, bool>> predicate, Expression<Func<UserTag, object>>? orderBy = null, bool orderDescending = userTagDefaultOrderDescending, params Expression<Func<UserTag, object>>[] includes)
        {
            if (orderBy == null) orderBy = userTagDefaultOrderBy;
            return await GetAsync(database.UserTags, predicate, orderBy, orderDescending);
        }

        public async Task<UserTag?> GetUserTagAsync(Expression<Func<UserTag, bool>> predicate, params Expression<Func<UserTag, object>>[] includes)
        { return await GetAsync(database.UserTags, predicate, userTagDefaultOrderBy, userTagDefaultOrderDescending, includes); }

        public async Task<UserTag[]> GetAllUserTagsAsync(Expression<Func<UserTag, object>>? orderBy = null, bool orderDescending = userTagDefaultOrderDescending, Expression<Func<UserTag, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = userTagDefaultOrderBy;
            return await GetAllAsync(database.UserTags, orderBy, orderDescending, predicate);
        }

        public async Task<UserTag[]> GetAllUserTagsAsync(Expression<Func<UserTag, bool>>? predicate = null, params Expression<Func<UserTag, object>>[] includes)
        { return await GetAllAsync(database.UserTags, null, false, predicate, includes); }

        public async Task<UserTag[]> GetAllUserTagsAsync(Expression<Func<UserTag, object>>? orderBy = null, bool orderDescending = userTagDefaultOrderDescending, params Expression<Func<UserTag, object>>[] includes)
        { return await GetAllAsync(database.UserTags, orderBy, orderDescending, null, includes); }

        public async Task<UserTag[]> GetAllUserTagsAsync(params Expression<Func<UserTag, object>>[] includes)
        { return await GetAllAsync(database.UserTags, userTagDefaultOrderBy, userTagDefaultOrderDescending, null, includes); }

        public async Task<UserTag[]> GetUserTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<UserTag, object>>? orderBy = null, bool orderDescending = userTagDefaultOrderDescending, Expression<Func<UserTag, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = userTagDefaultOrderBy;
            return await GetPageAsync(database.UserTags, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<UserTag[]> GetUserTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<UserTag, bool>>? predicate = null, params Expression<Func<UserTag, object>>[] includes)
        { return await GetPageAsync(database.UserTags, pageIndex, pageSize, userTagDefaultOrderBy, userTagDefaultOrderDescending, predicate, includes); }

        public async Task<UserTag[]> GetUserTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<UserTag, object>>? orderBy = null, bool orderDescending = userTagDefaultOrderDescending, params Expression<Func<UserTag, object>>[] includes)
        {
            if (orderBy == null) orderBy = userTagDefaultOrderBy;
            return await GetPageAsync(database.UserTags, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<UserTag[]> GetUserTagPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<UserTag, object>>[] includes)
        { return await GetPageAsync(database.UserTags, pageIndex, pageSize, userTagDefaultOrderBy, userTagDefaultOrderDescending, null, includes); }

        #endregion




        #region Admin Tag

        private readonly Expression<Func<AdminTag, object>> adminTagDefaultOrderBy = at => at.Name;
        private const bool adminTagDefaultOrderDescending = false;

        public async Task<AdminTag?> GetAdminTagAsync(Guid id)
        { return await GetAsync(database.AdminTags, at => at.Id == id); }

        public async Task<AdminTag?> GetAdminTagAsync(string id)
        { return await GetAdminTagAsync(Guid.Parse(id)); }

        public async Task<AdminTag?> GetAdminTagAsync(Expression<Func<AdminTag, bool>> predicate, Expression<Func<AdminTag, object>>? orderBy = null, bool orderDescending = adminTagDefaultOrderDescending, params Expression<Func<AdminTag, object>>[] includes)
        {
            if (orderBy == null) orderBy = adminTagDefaultOrderBy;
            return await GetAsync(database.AdminTags, predicate, orderBy, orderDescending);
        }

        public async Task<AdminTag?> GetAdminTagAsync(Expression<Func<AdminTag, bool>> predicate, params Expression<Func<AdminTag, object>>[] includes)
        { return await GetAsync(database.AdminTags, predicate, adminTagDefaultOrderBy, adminTagDefaultOrderDescending, includes); }

        public async Task<AdminTag[]> GetAllAdminTagsAsync(Expression<Func<AdminTag, object>>? orderBy = null, bool orderDescending = adminTagDefaultOrderDescending, Expression<Func<AdminTag, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = adminTagDefaultOrderBy;
            return await GetAllAsync(database.AdminTags, orderBy, orderDescending, predicate);
        }

        public async Task<AdminTag[]> GetAllAdminTagsAsync(Expression<Func<AdminTag, bool>>? predicate = null, params Expression<Func<AdminTag, object>>[] includes)
        { return await GetAllAsync(database.AdminTags, null, false, predicate, includes); }

        public async Task<AdminTag[]> GetAllAdminTagsAsync(Expression<Func<AdminTag, object>>? orderBy = null, bool orderDescending = adminTagDefaultOrderDescending, params Expression<Func<AdminTag, object>>[] includes)
        { return await GetAllAsync(database.AdminTags, orderBy, orderDescending, null, includes); }

        public async Task<AdminTag[]> GetAllAdminTagsAsync(params Expression<Func<AdminTag, object>>[] includes)
        { return await GetAllAsync(database.AdminTags, adminTagDefaultOrderBy, adminTagDefaultOrderDescending, null, includes); }

        public async Task<AdminTag[]> GetAdminTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AdminTag, object>>? orderBy = null, bool orderDescending = adminTagDefaultOrderDescending, Expression<Func<AdminTag, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = adminTagDefaultOrderBy;
            return await GetPageAsync(database.AdminTags, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<AdminTag[]> GetAdminTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AdminTag, bool>>? predicate = null, params Expression<Func<AdminTag, object>>[] includes)
        { return await GetPageAsync(database.AdminTags, pageIndex, pageSize, adminTagDefaultOrderBy, adminTagDefaultOrderDescending, predicate, includes); }

        public async Task<AdminTag[]> GetAdminTagPageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<AdminTag, object>>? orderBy = null, bool orderDescending = adminTagDefaultOrderDescending, params Expression<Func<AdminTag, object>>[] includes)
        {
            if (orderBy == null) orderBy = adminTagDefaultOrderBy;
            return await GetPageAsync(database.AdminTags, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<AdminTag[]> GetAdminTagPageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<AdminTag, object>>[] includes)
        { return await GetPageAsync(database.AdminTags, pageIndex, pageSize, adminTagDefaultOrderBy, adminTagDefaultOrderDescending, null, includes); }

        #endregion




        #region Resource Type

        private readonly Expression<Func<ResourceType, object>> resourceTypeDefaultOrderBy = rt => rt.Name;
        private const bool resourceTypeDefaultOrderDescending = false;

        public async Task<ResourceType?> GetResourceTypeAsync(Guid id)
        { return await GetAsync(database.ResourceTypes, rt => rt.Id == id); }

        public async Task<ResourceType?> GetResourceTypeAsync(string id)
        { return await GetResourceTypeAsync(Guid.Parse(id)); }

        public async Task<ResourceType?> GetResourceTypeAsync(Expression<Func<ResourceType, bool>> predicate, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params Expression<Func<ResourceType, object>>[] includes)
        {
            if (orderBy == null) orderBy = resourceTypeDefaultOrderBy;
            return await GetAsync(database.ResourceTypes, predicate, orderBy, orderDescending);
        }

        public async Task<ResourceType?> GetResourceTypeAsync(Expression<Func<ResourceType, bool>> predicate, params Expression<Func<ResourceType, object>>[] includes)
        { return await GetAsync(database.ResourceTypes, predicate, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, includes); }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceTypeDefaultOrderBy;
            return await GetAllAsync(database.ResourceTypes, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(Expression<Func<ResourceType, bool>>? predicate = null, params Expression<Func<ResourceType, object>>[] includes)
        { return await GetAllAsync(database.ResourceTypes, null, false, predicate, includes); }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params Expression<Func<ResourceType, object>>[] includes)
        { return await GetAllAsync(database.ResourceTypes, orderBy, orderDescending, null, includes); }

        public async Task<ResourceType[]> GetAllResourceTypesAsync(params Expression<Func<ResourceType, object>>[] includes)
        { return await GetAllAsync(database.ResourceTypes, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, null, includes); }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, Expression<Func<ResourceType, bool>>? predicate = null)
        {
            if (orderBy == null) orderBy = resourceTypeDefaultOrderBy;
            return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, orderBy, orderDescending, predicate);
        }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, bool>>? predicate = null, params Expression<Func<ResourceType, object>>[] includes)
        { return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, predicate, includes); }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, Expression<Func<ResourceType, object>>? orderBy = null, bool orderDescending = resourceTypeDefaultOrderDescending, params Expression<Func<ResourceType, object>>[] includes)
        {
            if (orderBy == null) orderBy = resourceTypeDefaultOrderBy;
            return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, orderBy, orderDescending, null, includes);
        }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100, params Expression<Func<ResourceType, object>>[] includes)
        { return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize, resourceTypeDefaultOrderBy, resourceTypeDefaultOrderDescending, null, includes); }
        
        #endregion
    }
}
