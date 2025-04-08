using KnowledgeBank.Models;

namespace KnowledgeBank.Data
{
    // This part is for creating resources
    public partial class ResourceManager
    {
        // --- Resource

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

            await database.SaveResourceChangesAsync();

            // Add tag relations to database
            await AddTagToResourceRangeAsync(resourceId, dto.Tags);

            // Add author relations to database
            await AddAuthorToResourceRangeAsync(resourceId, dto.Authors);

            // Add direct organisation relations to database
            await AddOrganisationToResourceRangeAsync(resourceId, FirstsOfTupleArray(dto.Organisations), SecondsOfTupleArray(dto.Organisations));

            // Add region relations to database
            await AddRegionToResourceRangeAsync(resourceId, dto.Regions);

            // Add indirect related organisation relations to database
            await AddRelatedOrganisationToResourceRangeAsync(resourceId, FirstsOfTupleArray(dto.RelatedOrganisations), SecondsOfTupleArray(dto.RelatedOrganisations));

            // Add non-author related person relations to database
            await AddRelatedPersonToResourceRangeAsync(resourceId, FirstsOfTupleArray(dto.RelatedPersons), SecondsOfTupleArray(dto.RelatedPersons));

            // Add all sources to database
            await AddSourceToResourceRangeAsync(resourceId, dto.Sources);

            // Add related source relations to database
            await AddRelatedSourceToResourceRangeAsync(resourceId, dto.RelatedSources);

            return resourceId;
        }

        // --- Document

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

        // --- Audio

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

            // URL is required in audio, so add it to sources
            await AddSourceToResourceAsync(resourceId, dto.URL);

            await database.SaveChangesAsync();

            return resourceId;
        }

        // --- Video

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

        // --- Website

        public async Task<Guid> CreateWebsiteAsync(WebsiteCreateDto dto)
        {
            // Create the base resource
            Guid resourceId = await CreateResourceAsync(dto);

            // Add metadata if present
            if (dto.AccessedOn != null)
            {
                WebsiteMetadata website = new()
                {
                    ResourceId = resourceId,
                    AccessedOn = dto.AccessedOn,
                };

                await database.WebsiteMetadata.AddAsync(website);
            }

            // Add source relation to database (this is the url of the website)
            await AddSourceToResourceAsync(resourceId, dto.Url);

            await database.SaveChangesAsync();

            return resourceId;
        }

        // --- Person

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

            // Add organisation relations if present
            await AddPersonToOrganisationRangeAsync(personId, SecondsOfTupleArray(dto.OrganisationRelations), FirstsOfTupleArray(dto.OrganisationRelations));

            // Add person relations if present
            await AddPersonRelationshipRangeAsync(personId, SecondsOfTupleArray(dto.PersonRelations), FirstsOfTupleArray(dto.PersonRelations));

            await database.SaveChangesAsync();

            return personId;
        }

        // --- Organisation

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

            // Add organisation relations if present
            await AddOrganisationRelationshipRangeAsync(organisationId, SecondsOfTupleArray(dto.OrganisationRelations), FirstsOfTupleArray(dto.OrganisationRelations));

            await database.SaveChangesAsync();

            return organisationId;
        }

        // --- Region

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

        // --- Tag

        public async Task<Guid> CreateTagAsync(TagCreateDto dto, bool isStandardized = false)
        {
            // Generate new ID for the tag
            Guid tagId = Guid.NewGuid();

            // Create a tag instance using the DTO
            Tag tag = new()
            {
                Id = tagId,
                Name = dto.Name,
                IsStandardized = isStandardized,
                IsApproved = dto.IsApproved,
                CreatedBy = dto.CreatedBy,
                CreatedOn = DateTime.UtcNow,
            };

            // Set approved by if present
            if (!string.IsNullOrEmpty(dto.ApprovedBy))
                tag.ApprovedBy = Guid.Parse(dto.ApprovedBy);

            // Add tag to database
            await database.Tags.AddAsync(tag);

            await database.SaveChangesAsync();

            return tagId;
        }

        // --- Resource Type

        public async Task<Guid> CreateResourceType(ResourceTypeCreateDto dto)
        {
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

            await database.SaveChangesAsync();

            return typeId;
        }
    }
}
