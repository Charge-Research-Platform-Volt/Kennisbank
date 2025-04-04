using KnowledgeBank.Models;

namespace backend.Data
{
    public partial class ResourceManager
    {
        // --- Resource

        public async Task<bool> DeleteResourceAsync(Guid id)
        {
            Resource? resource = await GetResourceAsync(id);

            if (resource == null) return false;

            database.Resources.Remove(resource);

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

            return true;
        }

        public async Task<bool> DeleteResourceAsync(string id)
        { return await DeleteResourceAsync(Guid.Parse(id)); }

        // --- Person

        public async Task<bool> DeletePersonAsync(Guid id)
        {
            Person? person = await GetPersonAsync(id);

            if (person == null) return false;

            database.Persons.Remove(person);

            await database.SaveChangesAsync();

            // Delete person from all organisations
            await RemovePersonFromAllOrganisationsAsync(id);

            // Delete all person relationships with this person
            await RemoveAllPersonRelationshipsContainingIdAsync(id);

            // Delete person from all author relations
            await RemoveAuthorFromAllResourcesAsync(id);

            // Delete all resource-related_person relations containing this person
            await RemoveRelatedPersonFromAllResourcesAsync(id);

            return true;
        }

        public async Task<bool> DeletePersonAsync(string id)
        { return await DeletePersonAsync(Guid.Parse(id)); }

        // --- Organisation

        public async Task<bool> DeleteOrganisationAsync(Guid id)
        {
            Organisation? organisation = await GetOrganisationAsync(id);

            if (organisation == null) return false;

            database.Organisations.Remove(organisation);

            await database.SaveChangesAsync();

            // Delete all organisation relationships with this organisation
            await RemoveAllOrganisationRelationshipsContainingIdAsync(id);

            // Delete organisation from all direct resource relations
            await RemoveOrganisationFromAllResourcesAsync(id);

            // Delete all person-organisation relations containing this organisation
            await RemoveAllPersonOrganisationRelationsWithOrganisationIdAsync(id);

            // Delete all resource-organisation relations containing this organisation
            await RemoveRelatedOrganisationFromAllResourcesAsync(id);

            return true;
        }

        public async Task<bool> DeleteOrganisationAsync(string id)
        { return await DeleteOrganisationAsync(Guid.Parse(id)); }

        // --- Region

        public async Task<bool> DeleteRegionAsync(Guid id)
        {
            Region? region = await GetRegionAsync(id);

            if (region == null) return false;

            database.Regions.Remove(region);

            await database.SaveChangesAsync();

            // Delete region from all resources
            await RemoveRegionFromAllResourcesAsync(id);

            return true;
        }

        public async Task<bool> DeleteRegionAsync(string id)
        { return await DeleteRegionAsync(Guid.Parse(id)); }

        // --- Audio Metadata

        public async Task<bool> DeleteAudioMetadataAsync(Guid id)
        {
            AudioMetadata? metadata = await GetAudioMetadataAsync(id);

            if (metadata == null) return false;

            database.AudioMetadata.Remove(metadata);

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAudioMetadataAsync(string id)
        { return await DeleteAudioMetadataAsync(Guid.Parse(id)); }

        // --- Video Metadata

        public async Task<bool> DeleteVideoMetadataAsync(Guid id)
        {
            VideoMetadata? metadata = await GetVideoMetadataAsync(id);

            if (metadata == null) return false;

            database.VideoMetadata.Remove(metadata);

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteVideoMetadataAsync(string id)
        { return await DeleteVideoMetadataAsync(Guid.Parse(id)); }

        // --- Website Metadata

        public async Task<bool> DeleteWebsiteMetadataAsync(Guid id)
        {
            WebsiteMetadata? metadata = await GetWebsiteMetadataAsync(id);

            if (metadata == null) return false;

            database.WebsiteMetadata.Remove(metadata);

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteWebsiteMetadataAsync(string id)
        { return await DeleteWebsiteMetadataAsync(Guid.Parse(id)); }

        // --- Document Metadata

        public async Task<bool> DeleteDocumentMetadataAsync(Guid id)
        {
            DocumentMetadata? metadata = await GetDocumentMetadataAsync(id);

            if (metadata == null) return false;

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteDocumentMetadataAsync(string id)
        { return await DeleteDocumentMetadataAsync(Guid.Parse(id)); }

        // --- User Tag

        public async Task<bool> DeleteUserTagAsync(Guid id)
        {
            UserTag? tag = await GetUserTagAsync(id);

            if (tag == null) return false;

            database.UserTags.Remove(tag);

            await database.SaveChangesAsync();

            // Remove all resource-usertag relations containing this tag
            await RemoveUserTagFromAllResourcesAsync(id);

            return true;
        }
        
        public async Task<bool> DeleteUserTagAsync(string id)
        { return await DeleteUserTagAsync(Guid.Parse(id)); }

        // --- Admin Tag

        public async Task<bool> DeleteAdminTagAsync(Guid id)
        {
            AdminTag? tag = await GetAdminTagAsync(id);

            if (tag == null) return false;

            database.AdminTags.Remove(tag);

            await database.SaveChangesAsync();

            // Remove all resource-admintag relations containing this tag
            await RemoveAdminTagFromAllResourcesAsync(id);

            return true;
        }
        
        public async Task<bool> DeleteAdminTagAsync(string id)
        { return await DeleteAdminTagAsync(Guid.Parse(id)); }

        // --- Resource type

        public async Task<bool> DeleteResourceTypeAsync(Guid id)
        {
            ResourceType? type = await GetResourceTypeAsync(id);

            if (type == null) return false;

            database.ResourceTypes.Remove(type);

            Guid unkownTypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId);

            // Set the type of all resources with this type to unknown
            foreach (Resource resource in await GetAllWhereAsync(database.Resources, i => i.TypeId == id))
            {
                resource.TypeId = unkownTypeId;
            }

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteResourceTypeAsync(string id)
        { return await DeleteResourceTypeAsync(Guid.Parse(id)); }
    }
}
