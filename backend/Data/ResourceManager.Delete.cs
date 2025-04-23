using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace KnowledgeBank.Data
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
            bool startedTransaction = await BeginTransaction();
        
            // Delete the resource itself
            int count = await DeleteAsync(database.Resources, resource => resource.Id == id);

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

            // Remove all resource-tag relations containing this resource
            await RemoveAllResourceTagRelationsWithResourceIdAsync(id);

            // Remove website metadata for this resource
            await DeleteWebsiteMetadataAsync(id);

            // Remove audio metadata for this resource
            await DeleteAudioMetadataAsync(id);

            // Remove document metadata for this resource
            await DeleteDocumentMetadataAsync(id);

            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteResourceAsync(string id)
        { return await DeleteResourceAsync(Guid.Parse(id)); }

        // --- Person

        public async Task<bool> DeletePersonAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();
        
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
            
            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeletePersonAsync(string id)
        { return await DeletePersonAsync(Guid.Parse(id)); }

        // --- Organisation

        public async Task<bool> DeleteOrganisationAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();
        
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
            
            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteOrganisationAsync(string id)
        { return await DeleteOrganisationAsync(Guid.Parse(id)); }

        // --- Region

        public async Task<bool> DeleteRegionAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();
        
            // Delete the region itself
            int count = await DeleteAsync(database.Regions, region => region.Id == id);

            // Delete region from all resources
            await RemoveRegionFromAllResourcesAsync(id);
            
            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteRegionAsync(string id)
        { return await DeleteRegionAsync(Guid.Parse(id)); }

        // --- Audio Metadata

        public async Task<bool> DeleteAudioMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();
        
            // Delete the metadata itself
            int count = await DeleteAsync(database.AudioMetadata, metadata => metadata.ResourceId == resourceId);
            
            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteAudioMetadataAsync(string resourceId)
        { return await DeleteAudioMetadataAsync(Guid.Parse(resourceId)); }

        // --- Video Metadata

        public async Task<bool> DeleteVideoMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();
        
            int count = await DeleteAsync(database.VideoMetadata, metadata => metadata.ResourceId == resourceId);
            
            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteVideoMetadataAsync(string resourceId)
        { return await DeleteVideoMetadataAsync(Guid.Parse(resourceId)); }

        // --- Website Metadata

        public async Task<bool> DeleteWebsiteMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();
        
            int count = await DeleteAsync(database.WebsiteMetadata, metadata => metadata.ResourceId == resourceId);
            
            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteWebsiteMetadataAsync(string resourceId)
        { return await DeleteWebsiteMetadataAsync(Guid.Parse(resourceId)); }

        // --- Document Metadata

        public async Task<bool> DeleteDocumentMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();
        
            int count = await DeleteAsync(database.DocumentMetadata, metadata => metadata.ResourceId == resourceId);
            
            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteDocumentMetadataAsync(string resourceId)
        { return await DeleteDocumentMetadataAsync(Guid.Parse(resourceId)); }

        // --- Tag

        public async Task<bool> DeleteTagAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();
        
            int count = await DeleteAsync(database.Tags, tag => tag.Id == id);

            // Remove all resource-tag relations containing this tag
            await RemoveTagFromAllResourcesAsync(id);
            
            if (startedTransaction) await Commit();

            return count > 0;
        }
        
        public async Task<bool> DeleteTagAsync(string id)
        { return await DeleteTagAsync(Guid.Parse(id)); }

        // --- Resource type

        public async Task<bool> DeleteResourceTypeAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();
        
            int count = await DeleteAsync(database.ResourceTypes, type => type.Id == id);

            // Set the type of all resources with this type to unknown
            count += await UpdatePropertyAsync(database.Resources, i => i.TypeId == id, i => i.TypeId, Guid.Parse(DatabaseSeeder.UnknownResourceTypeId));

            if (startedTransaction) await Commit();

            return count > 0;
        }

        public async Task<bool> DeleteResourceTypeAsync(string id)
        { return await DeleteResourceTypeAsync(Guid.Parse(id)); }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


