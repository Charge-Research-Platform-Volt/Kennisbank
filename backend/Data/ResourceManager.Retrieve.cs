using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;

namespace backend.Data
{
    public partial class ResourceManager
    {
        #region Resource

        public async Task<Resource?> GetResourceAsync(Guid id)
        { return await GetAsync(id, database.Resources); }

        public async Task<Resource?> GetResourceAsync(string id)
        { return await GetAsync(id, database.Resources); }

        public async Task<Resource[]> GetAllResourcesAsync()
        { return await GetAllAsync(database.Resources, r => r.CreationDate); }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.Resources, pageIndex, pageSize); }

        #endregion

        #region Person

        public async Task<Person?> GetPersonAsync(Guid id)
        { return await GetAsync(id, database.Persons); }

        public async Task<Person?> GetPersonAsync(string id)
        { return await GetAsync(id, database.Persons); }

        public async Task<Person[]> GetAllPersonsAsync()
        { return await GetAllAsync(database.Persons, p => p.Name); }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.Persons, pageIndex, pageSize); }

        #endregion

        #region Organisation

        public async Task<Organisation?> GetOrganisationAsync(Guid id)
        { return await GetAsync(id, database.Organisations); }

        public async Task<Organisation?> GetOrganisationAsync(string id)
        { return await GetAsync(id, database.Organisations); }

        public async Task<Organisation[]> GetAllOrganisationsAsync()
        { return await GetAllAsync(database.Organisations, o => o.Name); }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.Organisations, pageIndex, pageSize); }

        #endregion

        #region Region

        public async Task<Region?> GetRegionAsync(Guid id)
        { return await GetAsync(id, database.Regions); }

        public async Task<Region?> GetRegionAsync(string id)
        { return await GetAsync(id, database.Regions); }

        public async Task<Region[]> GetAllRegionsAsync()
        { return await GetAllAsync(database.Regions, r => r.Name); }

        public async Task<Region[]> GetRegionPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.Regions, pageIndex, pageSize); }

        #endregion

        #region Audio

        public async Task<AudioMetadata?> GetAudioMetadataAsync(Guid id)
        { return await GetAsync(id, database.AudioMetadata); }

        public async Task<AudioMetadata?> GetAudioMetadataAsync(string id)
        { return await GetAsync(id, database.AudioMetadata); }

        public async Task<AudioMetadata[]> GetAllAudioMetadatasAsync()
        { return await GetAllAsync(database.AudioMetadata); }

        public async Task<AudioMetadata[]> GetAudioMetadataPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.AudioMetadata, pageIndex, pageSize); }

        #endregion

        #region Video

        public async Task<VideoMetadata?> GetVideoMetadataAsync(Guid id)
        { return await GetAsync(id, database.VideoMetadata); }

        public async Task<VideoMetadata?> GetVideoMetadataAsync(string id)
        { return await GetAsync(id, database.VideoMetadata); }

        public async Task<VideoMetadata[]> GetAllVideoMetadatasAsync()
        { return await GetAllAsync(database.VideoMetadata); }

        public async Task<VideoMetadata[]> GetVideoMetadataPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.VideoMetadata, pageIndex, pageSize); }

        #endregion

        #region Website

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(Guid id)
        { return await GetAsync(id, database.WebsiteMetadata); }

        public async Task<WebsiteMetadata?> GetWebsiteMetadataAsync(string id)
        { return await GetAsync(id, database.WebsiteMetadata); }

        public async Task<WebsiteMetadata[]> GetAllWebsiteMetadatasAsync()
        { return await GetAllAsync(database.WebsiteMetadata); }

        public async Task<WebsiteMetadata[]> GetWebsiteMetadataPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.WebsiteMetadata, pageIndex, pageSize); }

        #endregion

        #region User Tag

        public async Task<UserTag?> GetUserTagAsync(Guid id)
        { return await GetAsync(id, database.UserTags); }

        public async Task<UserTag?> GetUserTagAsync(string id)
        { return await GetAsync(id, database.UserTags); }

        public async Task<UserTag[]> GetAllUserTagsAsync()
        { return await GetAllAsync(database.UserTags); }

        public async Task<UserTag[]> GetUserTagPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.UserTags, pageIndex, pageSize); }

        #endregion

        #region Admin Tag

        public async Task<AdminTag?> GetAdminTagAsync(Guid id)
        { return await GetAsync(id, database.AdminTags); }

        public async Task<AdminTag?> GetAdminTagAsync(string id)
        { return await GetAsync(id, database.AdminTags); }

        public async Task<AdminTag[]> GetAllAdminTagsAsync()
        { return await GetAllAsync(database.AdminTags); }

        public async Task<AdminTag[]> GetAdminTagPageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.AdminTags, pageIndex, pageSize); }

        #endregion

        #region Resource Type

        public async Task<ResourceType?> GetResourceTypeAsync(Guid id)
        { return await GetAsync(id, database.ResourceTypes); }

        public async Task<ResourceType?> GetResourceTypeAsync(string id)
        { return await GetAsync(id, database.ResourceTypes); }

        public async Task<ResourceType[]> GetAllResourceTypesAsync()
        { return await GetAllAsync(database.ResourceTypes); }

        public async Task<ResourceType[]> GetResourceTypePageAsync(int pageIndex = 1, int pageSize = 100)
        { return await GetPageAsync(database.ResourceTypes, pageIndex, pageSize); }

        #endregion
    }
}
