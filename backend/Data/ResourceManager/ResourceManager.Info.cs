// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

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
        
        protected async Task<int> GetCount<T>(DbSet<T> dbSet, Expression<Func<T, bool>>? predicate = null) where T : class
        {
            IQueryable<T> query = dbSet.AsNoTracking();
            if (predicate != null) query = query.Where(predicate);
            return await query.CountAsync();
        }

        #endregion



        // --------------------------------



        #region Resource

        // Resource exists

        public async Task<bool> ResourceExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.Resources, resource => resource.Id == resourceId); }

        public async Task<bool> ResourceExistsAsync(string resourceId)
        { return await ResourceExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> ResourceExistsAsync(Expression<Func<Resource, bool>> predicate)
        { return await ExistsAsync(database.Resources, predicate); }
        
        // Count
        
        public async Task<int> ResourceCountAsync(Expression<Func<Resource, bool>>? predicate = null)
        { return await GetCount(database.Resources, predicate); }

        #endregion

        #region Tag

        // Tag exists

        public async Task<bool> TagExistsAsync(Guid tagId)
        { return await ExistsAsync(database.Tags, tag => tag.Id == tagId); }

        public async Task<bool> TagExistsAsync(string tagId)
        { return await TagExistsAsync(Guid.Parse(tagId)); }

        public async Task<bool> TagExistsAsync(Expression<Func<Tag, bool>> predicate)
        { return await ExistsAsync(database.Tags, predicate); }
        
        // Count
        
        public async Task<int> TagCountAsync(Expression<Func<Tag, bool>>? predicate = null)
        { return await GetCount(database.Tags, predicate); }

        #endregion

        #region Person

        // Person exists
        public async Task<bool> PersonExistsAsync(Guid personId)
        { return await ExistsAsync(database.Persons, person => person.Id == personId); }

        public async Task<bool> PersonExistsAsync(string personId)
        { return await PersonExistsAsync(Guid.Parse(personId)); }

        public async Task<bool> PersonExistsAsync(Expression<Func<Person, bool>> predicate)
        { return await ExistsAsync(database.Persons, predicate); }
        
        // Count
        
        public async Task<int> PersonCountAsync(Expression<Func<Person, bool>>? predicate = null)
        { return await GetCount(database.Persons, predicate); }

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
        
        // Count
        
        public async Task<int> RegionCountAsync(Expression<Func<Region, bool>>? predicate = null)
        { return await GetCount(database.Regions, predicate); }

        #endregion

        #region AudioMetadata

        // AudioMetadata exists
        public async Task<bool> AudioMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.AudioMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> AudioMetadataExistsAsync(string resourceId)
        { return await AudioMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> AudioMetadataExistsAsync(Expression<Func<AudioMetadata, bool>> predicate)
        { return await ExistsAsync(database.AudioMetadata, predicate); }
        
        // Count
        
        public async Task<int> AudioMetadataCountAsync(Expression<Func<AudioMetadata, bool>>? predicate = null)
        { return await GetCount(database.AudioMetadata, predicate); }

        #endregion

        #region VideoMetadata

        // VideoMetadata exists
        public async Task<bool> VideoMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.VideoMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> VideoMetadataExistsAsync(string resourceId)
        { return await VideoMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> VideoMetadataExistsAsync(Expression<Func<VideoMetadata, bool>> predicate)
        { return await ExistsAsync(database.VideoMetadata, predicate); }
        
        // Count
        
        public async Task<int> VideoMetadataCountAsync(Expression<Func<VideoMetadata, bool>>? predicate = null)
        { return await GetCount(database.VideoMetadata, predicate); }

        #endregion

        #region DocumentMetadata

        // DocumentMetadata exists
        public async Task<bool> DocumentMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.DocumentMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> DocumentMetadataExistsAsync(string resourceId)
        { return await DocumentMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> DocumentMetadataExistsAsync(Expression<Func<DocumentMetadata, bool>> predicate)
        { return await ExistsAsync(database.DocumentMetadata, predicate); }
        
        // Count
        
        public async Task<int> DocumentMetadataCountAsync(Expression<Func<DocumentMetadata, bool>>? predicate = null)
        { return await GetCount(database.DocumentMetadata, predicate); }

        #endregion

        #region WebsiteMetadata

        // WebsiteMetadata exists
        public async Task<bool> WebsiteMetadataExistsAsync(Guid resourceId)
        { return await ExistsAsync(database.WebsiteMetadata, metadata => metadata.ResourceId == resourceId); }

        public async Task<bool> WebsiteMetadataExistsAsync(string resourceId)
        { return await WebsiteMetadataExistsAsync(Guid.Parse(resourceId)); }

        public async Task<bool> WebsiteMetadataExistsAsync(Expression<Func<WebsiteMetadata, bool>> predicate)
        { return await ExistsAsync(database.WebsiteMetadata, predicate); }
        
        // Count
        
        public async Task<int> WebsiteMetadataCountAsync(Expression<Func<WebsiteMetadata, bool>>? predicate = null)
        { return await GetCount(database.WebsiteMetadata, predicate); }

        #endregion

        #region ResourceType

        // ResourceType exists
        public async Task<bool> ResourceTypeExistsAsync(Guid resourceTypeId)
        { return await ExistsAsync(database.ResourceTypes, resourceType => resourceType.Id == resourceTypeId); }

        public async Task<bool> ResourceTypeExistsAsync(string resourceTypeId)
        { return await ResourceTypeExistsAsync(Guid.Parse(resourceTypeId)); }
        
        public async Task<bool> ResourceTypeExistsAsync(Expression<Func<ResourceType, bool>> predicate)
        { return await ExistsAsync(database.ResourceTypes, predicate); }
        
        // Count
        
        public async Task<int> ResourceTypeCountAsync(Expression<Func<ResourceType, bool>>? predicate = null)
        { return await GetCount(database.ResourceTypes, predicate); }

        #endregion
        
        #region OrganisationRelationship

        // OrganisationRelationship exists

        public async Task<bool> OrganisationRelationshipExistsAsync(Expression<Func<OrganisationRelationship, bool>> predicate)
        { return await ExistsAsync(database.OrganisationRelationships, predicate); }

        // Count

        public async Task<int> OrganisationRelationshipCountAsync(Expression<Func<OrganisationRelationship, bool>>? predicate = null)
        { return await GetCount(database.OrganisationRelationships, predicate); }

        #endregion

        #region PersonOrganisationRelation

        // PersonOrganisationRelation exists

        public async Task<bool> PersonOrganisationRelationExistsAsync(Expression<Func<PersonOrganisationRelation, bool>> predicate)
        { return await ExistsAsync(database.PersonOrganisationRelations, predicate); }

        // Count

        public async Task<int> PersonOrganisationRelationCountAsync(Expression<Func<PersonOrganisationRelation, bool>>? predicate = null)
        { return await GetCount(database.PersonOrganisationRelations, predicate); }

        #endregion

        #region PersonRelationship

        // PersonRelationship exists

        public async Task<bool> PersonRelationshipExistsAsync(Expression<Func<PersonRelationship, bool>> predicate)
        { return await ExistsAsync(database.PersonRelationships, predicate); }

        // Count

        public async Task<int> PersonRelationshipCountAsync(Expression<Func<PersonRelationship, bool>>? predicate = null)
        { return await GetCount(database.PersonRelationships, predicate); }

        #endregion

        #region ResourceAuthorRelation

        // ResourceAuthorRelation exists

        public async Task<bool> ResourceAuthorRelationExistsAsync(Expression<Func<ResourceAuthorRelation, bool>> predicate)
        { return await ExistsAsync(database.ResourceAuthorRelations, predicate); }

        // Count

        public async Task<int> ResourceAuthorRelationCountAsync(Expression<Func<ResourceAuthorRelation, bool>>? predicate = null)
        { return await GetCount(database.ResourceAuthorRelations, predicate); }

        #endregion

        #region ResourceOrganisationRelation

        // ResourceOrganisationRelation exists

        public async Task<bool> ResourceOrganisationRelationExistsAsync(Expression<Func<ResourceOrganisationRelation, bool>> predicate)
        { return await ExistsAsync(database.ResourceOrganisationRelations, predicate); }

        // Count

        public async Task<int> ResourceOrganisationRelationCountAsync(Expression<Func<ResourceOrganisationRelation, bool>>? predicate = null)
        { return await GetCount(database.ResourceOrganisationRelations, predicate); }

        #endregion

        #region ResourceRegionRelation

        // ResourceRegionRelation exists

        public async Task<bool> ResourceRegionRelationExistsAsync(Expression<Func<ResourceRegionRelation, bool>> predicate)
        { return await ExistsAsync(database.ResourceRegionRelations, predicate); }

        // Count

        public async Task<int> ResourceRegionRelationCountAsync(Expression<Func<ResourceRegionRelation, bool>>? predicate = null)
        { return await GetCount(database.ResourceRegionRelations, predicate); }

        #endregion

        #region ResourceRelatedOrganisationRelation

        // ResourceRelatedOrganisationRelation exists

        public async Task<bool> ResourceRelatedOrganisationRelationExistsAsync(Expression<Func<ResourceRelatedOrganisationRelation, bool>> predicate)
        { return await ExistsAsync(database.ResourceRelatedOrganisationRelations, predicate); }

        // Count

        public async Task<int> ResourceRelatedOrganisationRelationCountAsync(Expression<Func<ResourceRelatedOrganisationRelation, bool>>? predicate = null)
        { return await GetCount(database.ResourceRelatedOrganisationRelations, predicate); }

        #endregion

        #region ResourceRelatedPersonRelations

        // ResourceRelatedPersonRelations exists

        public async Task<bool> ResourceRelatedPersonRelationsExistsAsync(Expression<Func<ResourceRelatedPersonRelation, bool>> predicate)
        { return await ExistsAsync(database.ResourceRelatedPersonRelations, predicate); }

        // Count

        public async Task<int> ResourceRelatedPersonRelationsCountAsync(Expression<Func<ResourceRelatedPersonRelation, bool>>? predicate = null)
        { return await GetCount(database.ResourceRelatedPersonRelations, predicate); }

        #endregion

        #region ResourceSourceRelation

        // ResourceSourceRelation exists

        public async Task<bool> ResourceSourceRelationExistsAsync(Expression<Func<ResourceSourceRelation, bool>> predicate)
        { return await ExistsAsync(database.ResourceSourceRelations, predicate); }

        // Count

        public async Task<int> ResourceSourceRelationCountAsync(Expression<Func<ResourceSourceRelation, bool>>? predicate = null)
        { return await GetCount(database.ResourceSourceRelations, predicate); }

        #endregion

        #region ResourceTagRelation

        // ResourceTagRelation exists

        public async Task<bool> ResourceTagRelationExistsAsync(Expression<Func<ResourceTagRelation, bool>> predicate)
        { return await ExistsAsync(database.ResourceTagRelations, predicate); }

        // Count

        public async Task<int> ResourceTagRelationCountAsync(Expression<Func<ResourceTagRelation, bool>>? predicate = null)
        { return await GetCount(database.ResourceTagRelations, predicate); }

        #endregion
    }
}