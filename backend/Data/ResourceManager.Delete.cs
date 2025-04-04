using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace backend.Data
{
    // This part is for deleting resources
    public partial class ResourceManager
    {
        // --- Generic functions

        protected async Task<int> DeleteAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.Where(predicate).ExecuteDeleteAsync(); }

        protected async Task<int> DeleteAllWhereAsync<T>(DbSet<T> dbSet, Expression<Func<T, bool>> predicate) where T : class
        { return await dbSet.Where(predicate).ExecuteDeleteAsync(); }


        // --- Resource

        public async Task<bool> DeleteResourceAsync(Guid id)
        {
            // Delete the resource itself
            int count = await DeleteAsync(database.Resources, resource => resource.Id == id);

            await database.SaveResourceChangesAsync();

            // Remove all resource author relations containing this resource
            await RemoveAllResourceAuthorRelationsWithResourceIdAsync(id);

            // Remove all resource-organisation relations containing this resource
            await RemoveAllResourceOrganisationRelationsWithResourceIdAsync(id);

            // Remove all resource-region relations containing this resource
            await RemoveAllResourceRegionRelationsWithResourceIdAsync(id);

            // Remove all resource-source relations containing this resource
            await RemoveAllResourceSourceRelationsWithResourceIdAsync(id);

            // Remove all resource-related_organisation relations containing this resource
            await RemoveAllResourceRelatedOrganisationRelationsWithResourceIdAsync(id);

            // Remove all resource-related_persons relations containing this resource
            await RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(id);

            // Remove all resource-related_sources relations containing this resource
            await RemoveAllResourceRelatedSourceRelationsWithResourceIdAsync(id);

            // Remove all resource-admintag relations containing this resource
            await RemoveAllResourceUserTagRelationsWithResourceIdAsync(id);

            // Remove all resource-admintag relations containing this resource
            await RemoveAllResourceUserTagRelationsWithResourceIdAsync(id);

            return count > 0;
        }

        public async Task<bool> DeleteResourceAsync(string id)
        { return await DeleteResourceAsync(Guid.Parse(id)); }

        // --- Person

        public async Task<bool> DeletePersonAsync(Guid id)
        {
            // Delete the person itself
            int count = await DeleteAsync(database.Persons, person => person.Id == id);

            // Delete person from all organisations
            await RemovePersonFromAllOrganisationsAsync(id);

            // Delete all person relationships with this person
            await RemoveAllPersonRelationshipsContainingIdAsync(id);

            // Delete person from all author relations
            await RemoveAuthorFromAllResourcesAsync(id);

            // Delete all resource-related_person relations containing this person
            await RemoveRelatedPersonFromAllResourcesAsync(id);

            return count > 0;
        }

        public async Task<bool> DeletePersonAsync(string id)
        { return await DeletePersonAsync(Guid.Parse(id)); }

        // --- Organisation

        public async Task<bool> DeleteOrganisationAsync(Guid id)
        {
            // Delete the organisation itself
            int count = await DeleteAsync(database.Organisations, organisation => organisation.Id == id);

            // Delete all organisation relationships with this organisation
            await RemoveAllOrganisationRelationshipsContainingIdAsync(id);

            // Delete organisation from all direct resource relations
            await RemoveOrganisationFromAllResourcesAsync(id);

            // Delete all person-organisation relations containing this organisation
            await RemoveAllPersonOrganisationRelationsWithOrganisationIdAsync(id);

            // Delete all resource-organisation relations containing this organisation
            await RemoveRelatedOrganisationFromAllResourcesAsync(id);

            return count > 0;
        }

        public async Task<bool> DeleteOrganisationAsync(string id)
        { return await DeleteOrganisationAsync(Guid.Parse(id)); }

        // --- Region

        public async Task<bool> DeleteRegionAsync(Guid id)
        {
            // Delete the region itself
            int count = await DeleteAsync(database.Regions, region => region.Id == id);

            // Delete region from all resources
            await RemoveRegionFromAllResourcesAsync(id);

            return count > 0;
        }

        public async Task<bool> DeleteRegionAsync(string id)
        { return await DeleteRegionAsync(Guid.Parse(id)); }

        // --- Audio Metadata

        public async Task<bool> DeleteAudioMetadataAsync(Guid resourceId)
        {
            // Delete the metadata itself
            int count = await DeleteAsync(database.AudioMetadata, metadata => metadata.ResourceId == resourceId);

            return count > 0;
        }

        public async Task<bool> DeleteAudioMetadataAsync(string resourceId)
        { return await DeleteAudioMetadataAsync(Guid.Parse(resourceId)); }

        // --- Video Metadata

        public async Task<bool> DeleteVideoMetadataAsync(Guid resourceId)
        {
            int count = await DeleteAsync(database.VideoMetadata, metadata => metadata.ResourceId == resourceId);

            return count > 0;
        }

        public async Task<bool> DeleteVideoMetadataAsync(string resourceId)
        { return await DeleteVideoMetadataAsync(Guid.Parse(resourceId)); }

        // --- Website Metadata

        public async Task<bool> DeleteWebsiteMetadataAsync(Guid resourceId)
        {
            int count = await DeleteAsync(database.WebsiteMetadata, metadata => metadata.ResourceId == resourceId);

            return count > 0;
        }

        public async Task<bool> DeleteWebsiteMetadataAsync(string resourceId)
        { return await DeleteWebsiteMetadataAsync(Guid.Parse(resourceId)); }

        // --- Document Metadata

        public async Task<bool> DeleteDocumentMetadataAsync(Guid resourceId)
        {
            int count = await DeleteAsync(database.DocumentMetadata, metadata => metadata.ResourceId == resourceId);

            return count > 0;
        }

        public async Task<bool> DeleteDocumentMetadataAsync(string resourceId)
        { return await DeleteDocumentMetadataAsync(Guid.Parse(resourceId)); }

        // --- User Tag

        public async Task<bool> DeleteUserTagAsync(Guid id)
        {
            int count = await DeleteAsync(database.UserTags, tag => tag.Id == id);

            // Remove all resource-usertag relations containing this tag
            await RemoveUserTagFromAllResourcesAsync(id);

            return count > 0;
        }
        
        public async Task<bool> DeleteUserTagAsync(string id)
        { return await DeleteUserTagAsync(Guid.Parse(id)); }

        // --- Admin Tag

        public async Task<bool> DeleteAdminTagAsync(Guid id)
        {
            int count = await DeleteAsync(database.AdminTags, tag => tag.Id == id);

            // Remove all resource-admintag relations containing this tag
            await RemoveAdminTagFromAllResourcesAsync(id);

            return count > 0;
        }
        
        public async Task<bool> DeleteAdminTagAsync(string id)
        { return await DeleteAdminTagAsync(Guid.Parse(id)); }

        // --- Resource type

        public async Task<bool> DeleteResourceTypeAsync(Guid id)
        {
            int count = await DeleteAsync(database.ResourceTypes, type => type.Id == id);

            // Set the type of all resources with this type to unknown
            count += await UpdatePropertyAsync(database.Resources, i => i.TypeId == id, i => i.TypeId, Guid.Parse(DatabaseSeeder.UnknownResourceTypeId));

            return count > 0;
        }

        public async Task<bool> DeleteResourceTypeAsync(string id)
        { return await DeleteResourceTypeAsync(Guid.Parse(id)); }
    }
}
