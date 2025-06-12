// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
//
// Author: Abel Dieterich

using KnowledgeBank.Models;
using Org.BouncyCastle.Tls;
using Serilog;

namespace KnowledgeBank.Data
{
    // This part is for creating resources
    public partial class ResourceManager
    {
        // --- Resource

        public async Task<Guid> CreateResourceAsync(ResourceCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
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
                CreationDate = DateTime.UtcNow,
                Archived = false,
            };

            // Derive filetype when it is a file resource
            if (dto is FileResourceCreateDto fDto)
            {
                if (fDto.File == null) throw new Exception("File cannot be null!");
            
                resource.FileType = Filetype.ConvertExtensionToFiletype(Path.GetExtension(fDto.File.FileName));
                Log.Debug("Creating file resource with filetype {FileType}", resource.FileType);
                resource.Hash = fDto.Hash;
            }

            // Set filetype to website when it is a website resource
            if (dto is WebsiteCreateDto wDto)
                resource.FileType = "website";

            // Add resource to database
            await database.Resources.AddAsync(resource);

            // Add tag relations to database
            await AddTagToResourceRangeAsync(resourceId, dto.Tags);

            // Add author relations to database
            await AddAuthorToResourceRangeAsync(resourceId, dto.Authors);

            // Add direct organisation relations to database
            await AddOrganisationToResourceRangeAsync(resourceId, dto.Organisations.Select((entry) => entry.Id).ToArray(), dto.Organisations.Select((entry) => entry.Relation).ToArray());

            // Add region relations to database
            await AddRegionToResourceRangeAsync(resourceId, dto.Regions);

            // Add indirect related organisation relations to database
            await AddRelatedOrganisationToResourceRangeAsync(resourceId, dto.RelatedOrganisations.Select((entry) => entry.Id).ToArray(), dto.RelatedOrganisations.Select((entry) => entry.Relation).ToArray());

            // Add non-author related person relations to database
            await AddRelatedPersonToResourceRangeAsync(resourceId, dto.RelatedPersons.Select((entry) => entry.Id).ToArray(), dto.RelatedPersons.Select((entry) => entry.Relation).ToArray());

            // Add all sources to database
            await AddSourceToResourceRangeAsync(resourceId, dto.Sources);

            // Add related source relations to database
            await AddRelatedSourceToResourceRangeAsync(resourceId, dto.RelatedSources);
            
            if (startedTransaction) await Commit();

            return resourceId;
        }

        // --- Document

        public async Task<Guid> CreateDocumentAsync(DocumentCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
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

            if (startedTransaction) await Commit();

            return resourceId;
        }

        // --- Audio

        public async Task<Guid> CreateAudioAsync(AudioCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
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

            // URL is required in audio, so add it to sources
            // await AddSourceToResourceAsync(resourceId, dto.URL);

            if (startedTransaction) await Commit();

            return resourceId;
        }

        // --- Video

        public async Task<Guid> CreateVideoAsync(VideoCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
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

            if (startedTransaction) await Commit();

            return resourceId;
        }

        // --- Website

        public async Task<Guid> CreateWebsiteAsync(WebsiteCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
            // Create the base resource
            Guid resourceId = await CreateResourceAsync(dto);

            // Add metadata
            WebsiteMetadata website = new()
                {
                    ResourceId = resourceId,
                    Url = dto.Url,
                    AccessedOn = dto.AccessedOn,
                };

                await database.WebsiteMetadata.AddAsync(website);

            if (startedTransaction) await Commit();

            return resourceId;
        }

        // --- Person

        public async Task<Guid> CreatePersonAsync(PersonCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
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

            // Add organisation relations if present
            await AddPersonToOrganisationRangeAsync(personId, dto.OrganisationRelations.Select((entry) => entry.Relation).ToArray(), dto.OrganisationRelations.Select((entry) => entry.Id).ToArray());

            // Add person relations if present
            await AddPersonRelationshipRangeAsync(personId, dto.PersonRelations.Select((entry) => entry.Relation).ToArray(), dto.PersonRelations.Select((entry) => entry.Id).ToArray());

            if (startedTransaction) await Commit();

            return personId;
        }

        // --- Organisation

        public async Task<Guid> CreateOrganisationAsync(OrganisationCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
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

            // Add organisation relations if present
            await AddOrganisationRelationshipRangeAsync(organisationId, dto.OrganisationRelations.Select((entry) => entry.Relation).ToArray(), dto.OrganisationRelations.Select((entry) => entry.Id).ToArray());

            if (startedTransaction) await Commit();

            return organisationId;
        }

        // --- Region

        public async Task<Guid> CreateRegionAsync(RegionCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
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

            if (startedTransaction) await Commit();

            return regionId;
        }

        // --- Tag

        public async Task<Guid> CreateTagAsync(TagCreateDto dto, bool isStandardized = false)
        {
            bool startedTransaction = await BeginTransaction();
        
            // Generate new ID for the tag
            Guid tagId = Guid.NewGuid();

            // Create a tag instance using the DTO
            Tag tag = new()
            {
                Id = tagId,
                Name = dto.Name,
                IsStandardized = isStandardized,
                IsApproved = isStandardized ? true : dto.IsApproved,
                CreatedBy = Guid.Parse(dto.CreatedBy),
                CreatedOn = DateTime.UtcNow,
            };

            // Set approved by if present or if isStandardized (createdBy == approvedBy)
            if (isStandardized)
            {
                tag.ApprovedBy = Guid.Parse(dto.CreatedBy);
                tag.ApprovedOn = DateTime.UtcNow;
            }
            else if (!string.IsNullOrEmpty(dto.ApprovedBy))
            {
                tag.ApprovedBy = Guid.Parse(dto.ApprovedBy);
                tag.ApprovedOn = DateTime.UtcNow;
            }

            // Add tag to database
            await database.Tags.AddAsync(tag);

            if (startedTransaction) await Commit();

            return tagId;
        }

        // --- Resource Type

        public async Task<Guid> CreateResourceTypeAsync(ResourceTypeCreateDto dto)
        {
            bool startedTransaction = await BeginTransaction();
        
            // Generate new ID for the resource type
            Guid typeId = Guid.NewGuid();

            // Create a resource type instance using the DTO
            ResourceType resourceType = new()
            {
                Id = typeId,
                Name = dto.Name,
            };

            // Add resource type to database
            await database.ResourceTypes.AddAsync(resourceType);

            if (startedTransaction) await Commit();

            return typeId;
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


