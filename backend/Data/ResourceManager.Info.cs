using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    // This part is for information
    public partial class ResourceManager
    {
        #region Generic functions

        protected async Task<bool> ExistsAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.AsNoTracking().AnyAsync(predicate); }

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

        public async Task<bool> ResourceExistsAsync(Expression<Func<Resource, bool>> predicate)
        { return await ExistsAsync(database.Resources, predicate); }

        #endregion

        #region Tag

        // Tag exists

        public async Task<bool> TagExistsAsync(Guid tagId)
        { return await ExistsAsync(database.Tags, tag => tag.Id == tagId); }

        public async Task<bool> TagExistsAsync(string tagId)
        { return await TagExistsAsync(Guid.Parse(tagId)); }

        public async Task<bool> TagExistsAsync(Expression<Func<Tag, bool>> predicate)
        { return await ExistsAsync(database.Tags, predicate); }

        #endregion

        #region Person

        // Person exists
        public async Task<bool> PersonExistsAsync(Guid personId)
        { return await ExistsAsync(database.Persons, person => person.Id == personId); }

        public async Task<bool> PersonExistsAsync(string personId)
        { return await PersonExistsAsync(Guid.Parse(personId)); }

        public async Task<bool> PersonExistsAsync(Expression<Func<Person, bool>> predicate)
        { return await ExistsAsync(database.Persons, predicate); }

        #endregion

        #region Organisation

        // Organisation exists
        public async Task<bool> OrganisationExistsAsync(Guid organisationId)
        { return await ExistsAsync(database.Organisations, organisation => organisation.Id == organisationId); }

        public async Task<bool> OrganisationExistsAsync(string organisationId)
        { return await OrganisationExistsAsync(Guid.Parse(organisationId)); }

        public async Task<bool> OrganisationExistsAsync(Expression<Func<Organisation, bool>> predicate)
        { return await ExistsAsync(database.Organisations, predicate); }

        #endregion

        #region Region

        // Region exists
        public async Task<bool> RegionExistsAsync(Guid regionId)
        { return await ExistsAsync(database.Regions, region => region.Id == regionId); }

        public async Task<bool> RegionExistsAsync(string regionId)
        { return await RegionExistsAsync(Guid.Parse(regionId)); }

        public async Task<bool> RegionExistsAsync(Expression<Func<Region, bool>> predicate)
        { return await ExistsAsync(database.Regions, predicate); }

        #endregion

        #region AudioMetadata

        // AudioMetadata exists
        public async Task<bool> AudioMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.AudioMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> AudioMetadataExistsAsync(string resourceId)
        { return await AudioMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> AudioMetadataExistsAsync(Expression<Func<AudioMetadata, bool>> predicate)
        { return await ExistsAsync(database.AudioMetadata, predicate); }

        #endregion

        #region VideoMetadata

        // VideoMetadata exists
        public async Task<bool> VideoMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.VideoMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> VideoMetadataExistsAsync(string resourceId)
        { return await VideoMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> VideoMetadataExistsAsync(Expression<Func<VideoMetadata, bool>> predicate)
        { return await ExistsAsync(database.VideoMetadata, predicate); }

        #endregion

        #region DocumentMetadata

        // DocumentMetadata exists
        public async Task<bool> DocumentMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.DocumentMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> DocumentMetadataExistsAsync(string resourceId)
        { return await DocumentMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> DocumentMetadataExistsAsync(Expression<Func<DocumentMetadata, bool>> predicate)
        { return await ExistsAsync(database.DocumentMetadata, predicate); }

        #endregion

        #region WebsiteMetadata

        // WebsiteMetadata exists
        public async Task<bool> WebsiteMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.WebsiteMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> WebsiteMetadataExistsAsync(string resourceId)
        { return await WebsiteMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> WebsiteMetadataExistsAsync(Expression<Func<WebsiteMetadata, bool>> predicate)
        { return await ExistsAsync(database.WebsiteMetadata, predicate); }

        #endregion

        #region ResourceType

        // ResourceType exists
        public async Task<bool> ResourceTypeExistsAsync(Guid resourceTypeId)
        { return await ExistsAsync(database.ResourceTypes, resourceType => resourceType.Id == resourceTypeId); }

        public async Task<bool> ResourceTypeExistsAsync(string resourceTypeId)
        { return await ResourceTypeExistsAsync(Guid.Parse(resourceTypeId)); }

        public async Task<bool> ResourceTypeExistsAsync(Expression<Func<ResourceType, bool>> predicate)
        { return await ExistsAsync(database.ResourceTypes, predicate); }

        #endregion
    }
}
