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
            List<ResourceTagRelation> tagRelations = new List<ResourceTagRelation>();

            foreach (string tagId in dto.Tags)
            {
                tagRelations.Add( new()
                {
                    ResourceId = resourceId,
                    TagId = Guid.Parse(tagId)
                });
            }

            await database.ResourceTagRelations.AddRangeAsync(tagRelations.ToArray());

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

            return resourceId;
        }
        #endregion

        #region Relations
        
        #endregion

        #region Changes
        public async Task<bool> RenameResourceAsync(ResourceRenameDto dto)
        {
            Resource? resource = await database.Resources.FindAsync(Guid.Parse(dto.Id));

            if (resource == null) return false;

            resource.Title = dto.Title;

            return true;
        }

        public async Task<bool> DeleteResourceByIdAsync(Guid id)
        {
            Resource? resource = await GetResourceByIdAsync(id);

            return deleteResourceByIdAsync(resource);
        }

        public async Task<bool> DeleteResourceByIdAsync(string id)
        {
            Resource? resource = await GetResourceByIdAsync(id);

            return deleteResourceByIdAsync(resource);
        }

        private bool deleteResourceByIdAsync(Resource? resource)
        {
            if (resource == null) return false;

            database.Resources.Remove(resource);

            return true;
        }
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
        {
            return await database.Resources.FindAsync(id);
        }

        public async Task<Resource?> GetResourceByIdAsync(string id)
        {
            return await GetResourceByIdAsync(Guid.Parse(id));
        }

        public async Task<Resource[]?> GetAllResourcesAsync()
        {
            return await database.Resources.OrderByDescending(r => r.CreationDate).ToArrayAsync();
        }

        public async Task<Resource[]?> GetResourcePageAsync(int pageIndex = 1, int pageSize = 100)
        {
            if (pageIndex < 1 || pageSize < 1) return null;

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            return await database.Resources.Skip(skip).Take(pageSize).ToArrayAsync();
        }
        #endregion

        public async Task Commit()
        {
            // Save changes to database
            await database.SaveResourceChangesAsync();
        }

        public void Discard()
        {
            // Get all entities being tracked by the context
            var changedEntries = database.ChangeTracker.Entries()
                .Where(e => e.State != EntityState.Unchanged);

            foreach (var entry in changedEntries)
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        // For newly added entities, remove them from being tracked
                        entry.State = EntityState.Detached;
                        break;

                    case EntityState.Modified:
                        // For modified entities, revert changes
                        entry.State = EntityState.Unchanged;
                        entry.CurrentValues.SetValues(entry.OriginalValues);
                        break;

                    case EntityState.Deleted:
                        // For deleted entities, re-attach them
                        entry.State = EntityState.Unchanged;
                        break;
                }
            }
        }
    }
}
