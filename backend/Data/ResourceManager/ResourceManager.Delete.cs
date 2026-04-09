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

            // Remove all resource-related_persons relations containing this resource
            await RemoveAllResourceRelatedPersonRelationsWithResourceIdAsync(id);

            // Remove all resource-tag relations containing this resource
            await RemoveAllResourceTagRelationsWithResourceIdAsync(id);

            // Remove website metadata for this resource
            await DeleteWebsiteMetadataAsync(id);

            // Remove audio metadata for this resource
            await DeleteAudioMetadataAsync(id);

            // Remove document metadata for this resource
            await DeleteDocumentMetadataAsync(id);

            // Delete vector embeddings for this resource
            await _vectorStore.DeletePointsByResourceIdAsync(id);

            if (startedTransaction) await Commit();

            return count > 0;
        }

        // --- Person

        public async Task<bool> DeletePersonAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();

            // Delete person from all organisations
            await RemovePersonFromAllOrganisationsAsync(id);

            // Delete all person relationships with this person
            await RemoveAllPersonRelationshipsContainingIdAsync(id);

            // Delete person from all author relations
            await RemoveAuthorFromAllResourcesAsync(id);

            // Delete all resource-related_person relations containing this person
            await RemoveRelatedPersonFromAllResourcesAsync(id);

            // Delete the person itself (ExecuteDeleteAsync doesn't support TPT — use Find + Remove)
            var person = await database.Persons.FindAsync(id);
            if (person != null) database.Persons.Remove(person);
            int count = person != null ? 1 : 0;

            // Delete vector embeddings for this person
            await _vectorStore.DeletePointsByEntityIdAsync(id);

            if (startedTransaction) await Commit();

            return count > 0;
        }

        // --- Organisation

        public async Task<bool> DeleteOrganisationAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();

            // Delete all organisation relationships with this organisation
            await RemoveAllOrganisationRelationshipsContainingIdAsync(id);

            // Delete organisation from all resource relations
            await RemoveOrganisationFromAllResourcesAsync(id);

            // Delete all person-organisation relations containing this organisation
            await RemoveAllPersonOrganisationRelationsWithOrganisationIdAsync(id);

            // Delete the organisation itself (ExecuteDeleteAsync doesn't support TPT — use Find + Remove)
            var organisation = await database.Organisations.FindAsync(id);
            if (organisation != null) database.Organisations.Remove(organisation);
            int count = organisation != null ? 1 : 0;

            // Delete vector embeddings for this organisation
            await _vectorStore.DeletePointsByEntityIdAsync(id);

            if (startedTransaction) await Commit();

            return count > 0;
        }

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

        // --- Audio Metadata

        public async Task<bool> DeleteAudioMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();

            // Delete the metadata itself
            int count = await DeleteAsync(database.AudioMetadata, metadata => metadata.ResourceId == resourceId);

            if (startedTransaction) await Commit();

            return count > 0;
        }

        // --- Video Metadata

        public async Task<bool> DeleteVideoMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();

            int count = await DeleteAsync(database.VideoMetadata, metadata => metadata.ResourceId == resourceId);

            if (startedTransaction) await Commit();

            return count > 0;
        }

        // --- Website Metadata

        public async Task<bool> DeleteWebsiteMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();

            int count = await DeleteAsync(database.WebsiteMetadata, metadata => metadata.ResourceId == resourceId);

            if (startedTransaction) await Commit();

            return count > 0;
        }

        // --- Document Metadata

        public async Task<bool> DeleteDocumentMetadataAsync(Guid resourceId)
        {
            bool startedTransaction = await BeginTransaction();

            int count = await DeleteAsync(database.DocumentMetadata, metadata => metadata.ResourceId == resourceId);

            if (startedTransaction) await Commit();

            return count > 0;
        }

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

        // --- Resource type

        public async Task<bool> DeleteResourceTypeAsync(Guid id)
        {
            bool startedTransaction = await BeginTransaction();

            int count = await DeleteAsync(database.ResourceTypes, type => type.Id == id);

            // Set the type of all resources with this type to unknown
            count += await UpdatePropertyAsync(database.Resources, i => i.TypeId == id, i => i.TypeId, Guid.Parse(DatabaseContext.UnknownResourceTypeId));

            if (startedTransaction) await Commit();

            return count > 0;
        }

        // --- Chat
        public async Task<bool> DeleteChatAsync(Guid id, Guid userId)
        {
            bool startedTransaction = await BeginTransaction();

            // Delete the chat itself (with user ownership verification)
            // All messages in this chat are deleted automatically due to the cascade delete rule
            int count = await DeleteAsync(database.Chats, chat => chat.Id == id && chat.UserId == userId);

            if (startedTransaction) await Commit();

            return count > 0;
        }
    }
}
