using KnowledgeBank.Models;

namespace backend.Data
{
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

            // Add admin tag relations to database
            await AddAdminTagToResourceRangeAsync(resourceId, dto.AdminTags);

            // Add user tag relations to database
            await AddUserTagToResourceRangeAsync(resourceId, dto.UserTags);

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

        // --- Admin Tag

        public async Task<Guid> CreateAdminTagAsync(AdminTagCreateDto dto)
        {
            // Generate new ID for the admin tag
            Guid tagId = Guid.NewGuid();

            // Create an admin tag instance using the DTO
            AdminTag adminTag = new()
            {
                Id = tagId,
                Name = dto.Name,
            };

            // Add admin tag to database
            await database.AdminTags.AddAsync(adminTag);

            await database.SaveChangesAsync();

            return tagId;
        }

        // --- User Tag

        public async Task<Guid> CreateUserTagAsync(UserTagCreateDto dto)
        {
            // Generate new ID for the user tag
            Guid tagId = Guid.NewGuid();

            // Create an user tag instance using the DTO
            UserTag userTag = new()
            {
                Id = tagId,
                Name = dto.Name,
                User = dto.User,
                CreatedOn = DateTime.UtcNow,
                ApprovedOn = dto.ApprovedOn,
            };

            // Add user tag to database
            await database.UserTags.AddAsync(userTag);

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
