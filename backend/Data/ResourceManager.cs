using Azure.Core;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic.FileIO;

namespace backend.Data
{
    public class ResourceManager
    {
        private readonly DatabaseContext database;

        public ResourceManager(DatabaseContext dbContext)
        {
            database = dbContext;
        }

        #region Creation
        public async Task<Guid> CreateResourceAsync(ResourceCreateDto dto)
        {
            Guid resourceId = Guid.NewGuid();

            // Construct resource 
            Resource resource = new()
            {
                Id = resourceId,
                Title = dto.Title,
                Description = dto.Description,
                TypeId = new Guid(dto.TypeId),
                LanguageCode = dto.LanguageCode,
                PublicationCode = dto.PublicationCode,
                PublicationDate = dto.PublicationDate,
                License = dto.License,
                Note = dto.Note,
                FileType = "unknown",
                CreationDate = DateTime.UtcNow
            };

            // Derive filetype when it is a file resource
            if (dto is FileResourceCreateDto fDto)
            {
                resource.FileType = Filetype.ConvertExtensionToFiletype(Path.GetExtension(fDto.File.FileName));
                resource.Hash = fDto.Hash;
            }

            // Set filetype to website when it is a website resource
            if (dto is WebsiteCreateDto wDto)
                resource.FileType = "website";

            // Add resource to database
            await database.Resources.AddAsync(resource);

            // Add tag relations to database
            await TagResourceRangeAsync(resourceId, dto.Tags);

            await database.SaveResourceChangesAsync();

            return resourceId;
        }

        public async Task<Guid> CreateDocumentAsync(DocumentCreateDto dto)
        {
            // Create the base resource
            Guid resourceId = await CreateResourceAsync(dto);

            // If metadata is present add to document metadata table
            if (!string.IsNullOrEmpty(dto.Abstract))
            {
                DocumentMetadata doc = new()
                {
                    ResourceId = resourceId,
                    Abstract = dto.Abstract
                };

                await database.DocumentMetadata.AddAsync(doc);
            }

            await database.SaveChangesAsync();

            return resourceId;
        }

        public async Task<Guid> CreateAudioAsync(AudioCreateDto dto)
        {
            // Create the base resource
            Guid resourceId = await CreateResourceAsync(dto);

            // Add metadata if present
            if (dto.Length != null)
            {
                AudioMetadata audio = new()
                {
                    ResourceId = resourceId,
                    Length = dto.Length
                };

                await database.AudioMetadata.AddAsync(audio);
            }

            await database.SaveChangesAsync();

            return resourceId;
        }

        public async Task<Guid> CreateVideoAsync(VideoCreateDto dto)
        {
            // Create the base resource
            Guid resourceId = await CreateResourceAsync(dto);

            // Add metadata if present
            if (dto.Length != null)
            {
                VideoMetadata video = new()
                { 
                    ResourceId = resourceId, 
                    Length = dto.Length 
                };

                await database.VideoMetadata.AddAsync(video);
            }

            await database.SaveChangesAsync();

            return resourceId;
        }

        public async Task<Guid> CreateWebsiteAsync(WebsiteCreateDto dto)
        {
            // Create the base resource
            Guid resourceId = await CreateResourceAsync(dto);

            // Add metadata if present
            if (false)
            {
                // Since we do not have any metadata, we make the if statement inpossible
                // Set the if statement and remove the warning disable when we do have metadata
#pragma warning disable CS0162 // Unreachable code detected
                WebsiteMetadata website = new()
                {
                    ResourceId = resourceId,
                };
#pragma warning restore CS0162 // Unreachable code detected

                await database.WebsiteMetadata.AddAsync(website);
            }

            // Add source relation to database (this is the url of the website)
            

            await database.SaveChangesAsync();

            return resourceId;
        }

        public async Task<Guid> CreatePersonAsync(PersonCreateDto dto)
        {
            // Generate new ID for the person
            Guid personId = Guid.NewGuid();

            // Create a person instance using the DTO
            Person person = new()
            {
                Id = personId,
                Name = dto.Name,
                Occupation = dto.Occupation,
                Description = dto.Description,
                EmailAddress = dto.EmailAddress,
                Linkedin = dto.Linkedin,
            };

            // Add person to database
            await database.Persons.AddAsync(person);

            await database.SaveChangesAsync();

            return personId;
        }

        public async Task<Guid> CreateOrganisationAsync(OrganisationCreateDto dto)
        {
            // Generate new ID for the organisation
            Guid organisationId = Guid.NewGuid();

            // Create a organisation instance using the DTO
            Organisation organisation = new()
            {
                Id = organisationId,
                Name = dto.Name,
                Description = dto.Description,
                Website = dto.Website,
                EmailAddress = dto.EmailAddress,
            };

            // Add organisation to database
            await database.Organisations.AddAsync(organisation);

            await database.SaveChangesAsync();

            return organisationId;
        }

        public async Task<Guid> CreateRegionAsync(RegionCreateDto dto)
        {
            // Generate new ID for the region
            Guid regionId = Guid.NewGuid();

            // Create a region instance using the DTO
            Region region = new()
            {
                Id = regionId,
                Name = dto.Name,
            };

            // Add region to database
            await database.Regions.AddAsync(region);

            await database.SaveChangesAsync();

            return regionId;
        }
        #endregion

        #region Relations
        public async Task TagResourceRangeAsync(Guid resourceId, Guid[] tagIds)
        {
            List<ResourceTagRelation> tagRelations = new List<ResourceTagRelation>();

            foreach (Guid tagId in tagIds)
            {
                tagRelations.Add(new()
                {
                    ResourceId = resourceId,
                    TagId = tagId
                });
            }

            await database.ResourceTagRelations.AddRangeAsync(tagRelations.ToArray());

            await database.SaveChangesAsync();
        }

        public async Task TagResourceRangeAsync(Guid resourceId, string[] tagIds)
        { await TagResourceRangeAsync(resourceId, StringToGuidArray(tagIds)); }

        public async Task TagResourceRangeAsync(string resourceId, Guid[] tagIds)
        { await TagResourceRangeAsync(Guid.Parse(resourceId), tagIds); }

        public async Task TagResourceRangeAsync(string resourceId, string[] tagIds)
        { await TagResourceRangeAsync(Guid.Parse(resourceId), StringToGuidArray(tagIds)); }

        public async Task TagResourceAsync(Guid resourceId, Guid tagId)
        {
            ResourceTagRelation tagEntry = new()
            {
                ResourceId = resourceId,
                TagId = tagId
            };

            await database.ResourceTagRelations.AddAsync(tagEntry);

            await database.SaveChangesAsync();
        }

        public async Task TagResourceAsync(string resourceId, Guid tagId)
        { await TagResourceAsync(Guid.Parse(resourceId), tagId); }

        public async Task TagResourceAsync(Guid resourceId, string tagId)
        { await TagResourceAsync(resourceId, Guid.Parse(tagId)); }

        public async Task TagResourceAsync(string resourceId, string tagId)
        { await TagResourceAsync(Guid.Parse(resourceId), Guid.Parse(tagId)); }

        public async Task AddSourceToResource(Guid resourceId, string sourceUrl)
        {
            ResourceSourceRelation sourceEntry = new()
            {
                ResourceId = resourceId,
                Url = sourceUrl,
            };

            await database.ResourceSourceRelations.AddAsync(sourceEntry);

            await database.SaveChangesAsync();
        }

        public async Task AddSourceToResource(string resourceId, string sourceUrl)
        { await AddSourceToResource(Guid.Parse(resourceId), sourceUrl); }
        #endregion

        #region Changes
        public async Task<bool> RenameResourceAsync(ResourceRenameDto dto)
        {
            Resource? resource = await GetResourceByIdAsync(dto.Id);

            if (resource == null) return false;

            resource.Title = dto.Title;

            await database.SaveResourceChangesAsync();

            return true;
        }

        public async Task<bool> DeleteResourceByIdAsync(Guid id)
        {
            Resource? resource = await GetResourceByIdAsync(id);

            if (resource == null) return false;

            database.Resources.Remove(resource);

            await database.SaveResourceChangesAsync();

            return true;
        }

        public async Task<bool> DeleteResourceByIdAsync(string id)
        { return await DeleteResourceByIdAsync(Guid.Parse(id)); }

        public async Task<bool> DeletePersonByIdAsync(Guid id)
        {
            Person? person = await GetPersonByIdAsync(id);

            if (person == null) return false;

            database.Persons.Remove(person);

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeletePersonByIdAsync(string id)
        { return await DeletePersonByIdAsync(Guid.Parse(id)); }

        public async Task<bool> DeleteOrganisationByIdAsync(Guid id)
        {
            Organisation? organisation = await GetOrganisationByIdAsync(id);

            if (organisation == null) return false;

            database.Organisations.Remove(organisation);

            await database.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteOrganisationByIdAsync(string id)
        { return await DeleteOrganisationByIdAsync(Guid.Parse(id)); }
        #endregion

        #region Information
        public async Task<bool> HashExistsAsync(string hash)
        {
            if (string.IsNullOrEmpty(hash)) return false;

            Resource? resource = await database.Resources.Where(f => f.Hash == hash).FirstOrDefaultAsync();

            return resource == null;
        }
        #endregion

        #region Retrieval
        public async Task<Resource?> GetResourceByIdAsync(Guid id)
        { return await database.Resources.FindAsync(id); }

        public async Task<Resource?> GetResourceByIdAsync(string id)
        { return await GetResourceByIdAsync(Guid.Parse(id)); }

        public async Task<Resource[]> GetAllResourcesAsync()
        { return await database.Resources.OrderByDescending(r => r.CreationDate).ToArrayAsync(); }

        public async Task<Resource[]> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100)
        {
            // Return empty for invalid input
            if (pageIndex < 1 || pageSize < 1) return Array.Empty<Resource>();

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            return await database.Resources.Skip(skip).Take(pageSize).ToArrayAsync();
        }

        public async Task<Person?> GetPersonByIdAsync(Guid id)
        { return await database.Persons.FindAsync(id); }

        public async Task<Person?> GetPersonByIdAsync(string id)
        { return await GetPersonByIdAsync(Guid.Parse(id)); }

        public async Task<Person[]> GetAllPersonsAsync()
        { return await database.Persons.OrderBy(p => p.Name).ToArrayAsync(); }

        public async Task<Person[]> GetPersonPageAsync(int pageIndex = 1, int pageSize = 100)
        {
            // Return empty for invalid input
            if (pageIndex < 1 || pageSize < 1) return Array.Empty<Person>();

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            return await database.Persons.Skip(skip).Take(pageSize).ToArrayAsync();
        }

        public async Task<Organisation?> GetOrganisationByIdAsync(Guid id)
        { return await database.Organisations.FindAsync(id); }

        public async Task<Organisation?> GetOrganisationByIdAsync(string id)
        { return await GetOrganisationByIdAsync(Guid.Parse(id)); }

        public async Task<Organisation[]> GetAllOrganisationsAsync()
        { return await database.Organisations.OrderBy(p => p.Name).ToArrayAsync(); }

        public async Task<Organisation[]> GetOrganisationPageAsync(int pageIndex = 1, int pageSize = 100)
        {
            // Return empty for invalid input
            if (pageIndex < 1 || pageSize < 1) return Array.Empty<Organisation>();

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            return await database.Organisations.Skip(skip).Take(pageSize).ToArrayAsync();
        }
        #endregion

        public async Task BeginTransaction()
        {
            // Begin a transaction that can be committed or rolled back later
            await database.Database.BeginTransactionAsync();
        }

        public async Task Commit()
        {
            // Commit changes from transaction to database
            if (database.Database.CurrentTransaction != null)
                await database.Database.CurrentTransaction.CommitAsync();
        }

        public async Task Rollback()
        {
            // Roll back the transaction if one exists
            if (database.Database.CurrentTransaction != null)
                await database.Database.CurrentTransaction.RollbackAsync();
        }

        #region Private functions
        private Guid[] StringToGuidArray(string[] strings)
        {
            Guid[] guids = new Guid[strings.Length];

            for (int i = 0; i < strings.Length; i++)
            {
                guids[i] = Guid.Parse(strings[i]);
            }

            return guids;
        }
        #endregion
    }
}
