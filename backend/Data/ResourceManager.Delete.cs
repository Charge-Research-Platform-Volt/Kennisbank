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

        // --- User Tag

        public async Task<bool> DeleteUserTagAsync(Guid id)
        {
            UserTag? tag = await GetUserTagAsync(id);

            if (tag == null) return false;

            database.UserTags.Remove(tag);

            await database.SaveChangesAsync();

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

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteResourceTypeAsync(string id)
        { return await DeleteResourceTypeAsync(Guid.Parse(id)); }
    }
}
