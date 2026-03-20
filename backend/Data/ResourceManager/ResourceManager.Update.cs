// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
{
    // This part is for updating properties
    public partial class ResourceManager
    {
        #region Generic functions

        protected async Task<int> UpdatePropertyAsync<T, TProperty>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate, Expression<Func<T, TProperty>> propertySelector, TProperty newValue) where T : class
        {
            bool startedTransaction = await BeginTransaction();
            int count = await dbSet.Where(predicate).ExecuteUpdateAsync(s => s.SetProperty(e => EF.Property<TProperty>(e, GetPropertyName(propertySelector)), _ => newValue));
            if (startedTransaction) await Commit();
            return count;
        }
        
        private string GetPropertyName<T, TProperty>(Expression<Func<T, TProperty>> propertyExpression)
        {
            if (propertyExpression.Body is MemberExpression memberExpression)
            {
                return memberExpression.Member.Name;
            }
            throw new ArgumentException("Expression must be a property access expression.", nameof(propertyExpression));
        }

        #endregion

        // --------------------------------



        #region Resource

        public async Task<bool> UpdateResourceAsync<T>(Guid id, Expression<Func<Resource, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Resources, resource => resource.Id == id, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateResourceAsync<T>(Expression<Func<Resource, bool>> predicate, Expression<Func<Resource, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Resources, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Organisation

        public async Task<bool> UpdateOrganisationAsync<T>(Guid id, Expression<Func<Organisation, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Organisations, organisation => organisation.Id == id, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateOrganisationAsync<T>(Expression<Func<Organisation, bool>> predicate, Expression<Func<Organisation, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Organisations, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Person

        public async Task<bool> UpdatePersonAsync<T>(Guid id, Expression<Func<Person, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Persons, person => person.Id == id, propertySelector, newValue) > 0; }

        public async Task<bool> UpdatePersonAsync<T>(Expression<Func<Person, bool>> predicate, Expression<Func<Person, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Persons, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Region

        public async Task<bool> UpdateRegionAsync<T>(Guid id, Expression<Func<Region, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Regions, region => region.Id == id, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateRegionAsync<T>(Expression<Func<Region, bool>> predicate, Expression<Func<Region, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Regions, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region AudioMetadata

        public async Task<bool> UpdateAudioMetadataAsync<T>(Guid resourceId, Expression<Func<AudioMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.AudioMetadata, metadata => metadata.ResourceId == resourceId, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateAudioMetadataAsync<T>(Expression<Func<AudioMetadata, bool>> predicate, Expression<Func<AudioMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.AudioMetadata, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region VideoMetadata

        public async Task<bool> UpdateVideoMetadataAsync<T>(Guid resourceId, Expression<Func<VideoMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.VideoMetadata, metadata => metadata.ResourceId == resourceId, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateVideoMetadataAsync<T>(Expression<Func<VideoMetadata, bool>> predicate, Expression<Func<VideoMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.VideoMetadata, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region DocumentMetadata

        public async Task<bool> UpdateDocumentMetadataAsync<T>(Guid resourceId, Expression<Func<DocumentMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.DocumentMetadata, metadata => metadata.ResourceId == resourceId, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateDocumentMetadataAsync<T>(Expression<Func<DocumentMetadata, bool>> predicate, Expression<Func<DocumentMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.DocumentMetadata, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region WebsiteMetadata

        public async Task<bool> UpdateWebsiteMetadataAsync<T>(Guid resourceId, Expression<Func<WebsiteMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.WebsiteMetadata, metadata => metadata.ResourceId == resourceId, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateWebsiteMetadataAsync<T>(Expression<Func<WebsiteMetadata, bool>> predicate, Expression<Func<WebsiteMetadata, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.WebsiteMetadata, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Tag

        public async Task<bool> UpdateTagAsync<T>(Guid id, Expression<Func<Tag, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Tags, tag => tag.Id == id, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateTagAsync<T>(Expression<Func<Tag, bool>> predicate, Expression<Func<Tag, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Tags, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Chat

        public async Task<bool> UpdateChatAsync<T>(Guid id, Expression<Func<Chats, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.Chats, chat => chat.Id == id, propertySelector, newValue) > 0; }

        #endregion

        #region Resource Type

        public async Task<bool> UpdateResourceTypeAsync<T>(Guid id, Expression<Func<ResourceType, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.ResourceTypes, resourceType => resourceType.Id == id, propertySelector, newValue) > 0; }

        public async Task<bool> UpdateResourceTypeAsync<T>(Expression<Func<ResourceType, bool>> predicate, Expression<Func<ResourceType, T>> propertySelector, T newValue)
        { return await UpdatePropertyAsync(database.ResourceTypes, predicate, propertySelector, newValue) > 0; }

        #endregion

        #region Organisation Relationship

        // Relation

        public async Task<bool> UpdateRelationInOrganisationRelationshipAsync(Guid sourceOrganisationId, string newRelation, Guid targetOrganisationId)
        { return await UpdatePropertyAsync(database.OrganisationRelationships, relation => relation.SourceOrganisationId == sourceOrganisationId && relation.TargetOrganisationId == targetOrganisationId, relationship => relationship.Relation, newRelation) > 0; }

        #endregion

        #region Person Organisation

        // Role

        public async Task<bool> UpdateRoleInPersonOrganisationRelationAsync(Guid personId, string newRole, Guid organisationId)
        { return await UpdatePropertyAsync(database.PersonOrganisationRelations, relation => relation.PersonId == personId && relation.OrganisationId == organisationId, relation => relation.Role, newRole) > 0; }

        #endregion


        #region Person Relationship

        // Relation

        public async Task<bool> UpdateRelationInPersonRelationshipAsync(Guid sourcePersonId, string newRelation, Guid targetPersonId)
        { return await UpdatePropertyAsync(database.PersonRelationships, relation => relation.SourcePersonId == sourcePersonId && relation.TargetPersonId == targetPersonId, relationship => relationship.Relation, newRelation) > 0; }

        #endregion

        #region Resource Organisation

        // Role

        public async Task<bool> UpdateRoleInResourceOrganisationRelationAsync(Guid resourceId, Guid organisationId, string newRole)
        { return await UpdatePropertyAsync(database.ResourceOrganisationRelations, relation => relation.ResourceId == resourceId && relation.OrganisationId == organisationId, relation => relation.Role, newRole) > 0; }

        #endregion

        #region Resource Related Person

        // Role

        public async Task<bool> UpdateRoleInResourceRelatedPersonRelationAsync(Guid resourceId, Guid personId, string newRole)
        { return await UpdatePropertyAsync(database.ResourceRelatedPersonRelations, relation => relation.ResourceId == resourceId && relation.PersonId == personId, relation => relation.Role, newRole) > 0; }

        #endregion
    }
}
