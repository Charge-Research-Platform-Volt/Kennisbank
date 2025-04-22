using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Controllers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class ResourceManagerDeleteTests : TestBase
{
    private ResourceManager _resourceManager;
    private UserManager<User> _userManager;
    private Mock<IAzureBlobService> _mockBlobService;

    private string testUserId;

    [TearDown]
    public void TearDown()
    {
        _userManager?.Dispose();
    }

    [SetUp]
    public async Task SetupController()
    {
        UserStore<User> userStore = new UserStore<User>(Context);
        _userManager = new UserManager<User>(
           userStore,
            null,
            new PasswordHasher<User>(),
            new[] { new UserValidator<User>() },
            new[] { new PasswordValidator<User>() }, 
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null, 
            null 
        );

        User user = new() {
            UserName = "admin",
        };

        await _userManager.CreateAsync(user, "Admin123!");
        testUserId = Context.Users.FirstOrDefault(u => u.UserName == "admin").Id;

        _mockBlobService = new Mock<IAzureBlobService>();
        _resourceManager = new ResourceManager(Context);
    }

    protected override async Task SeedTestDatabase(DatabaseContext context)
    {
        // Enable extension for text-search-vectors
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""resource-vectors"" ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");
        await DatabaseSeeder.SeedTemplate(context);
    }

    [Test]
    public async Task DeleteResourceAsync_WithValidId_DeletesResourceAndRelatedEntities()
    {
        // Arrange
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        // Create resource to delete
        ResourceCreateDto dto = new ResourceCreateDto
        {
            Title = "Resource to Delete",
            Description = "This resource will be deleted during the test.",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow
        };

        Guid resourceId = await _resourceManager.CreateResourceAsync(dto);
        
        // Act
        bool result = await _resourceManager.DeleteResourceAsync(resourceId);

        // Assert
        Assert.That(result, Is.True);
        Resource? deletedResource = await Context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        Assert.That(deletedResource, Is.Null);
    }

    [Test]
    public async Task DeleteResourceAsync_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        Guid nonExistentId = Guid.NewGuid();

        // Act
        bool result = await _resourceManager.DeleteResourceAsync(nonExistentId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task DeleteResourceAsync_WithRelatedEntities_DeletesAllRelations()
    {
        // Arrange - Create necessary entities
        TagCreateDto testTagDto = new TagCreateDto
        {
            Name = "DeleteTestTag",
            CreatedBy = testUserId
        };

        Guid testTagId = await _resourceManager.CreateTagAsync(testTagDto);

        PersonCreateDto testPersonDto = new PersonCreateDto
        {
            Name = "Delete Test Author",
            Occupation = "Test Occupation"
        };
        Guid testPersonId = await _resourceManager.CreatePersonAsync(testPersonDto);

        OrganisationCreateDto testOrganisationDto = new OrganisationCreateDto
        {
            Name = "Delete Test Organisation"
        };
        Guid testOrganisationId = await _resourceManager.CreateOrganisationAsync(testOrganisationDto);

        RegionCreateDto testRegionDto = new RegionCreateDto
        {
            Name = "Delete Test Region"
        };
        Guid testRegionId = await _resourceManager.CreateRegionAsync(testRegionDto);

        ResourceType? type = await Context.ResourceTypes.FirstAsync();

        // Create resource with all relationships
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Relations to Delete",
            Description = "This resource and all its relations will be deleted",
            TypeId = type.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags = [testTagId.ToString()],
            Authors = [testPersonId.ToString()],
            Organisations = [(testOrganisationId.ToString(), "partner")],
            Regions = [testRegionId.ToString()]
        };

        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);

        // Act
        bool result = await _resourceManager.DeleteResourceAsync(resourceId);

        // Assert
        Assert.That(result, Is.True);
        
        // Check that resource is deleted
        Resource? deletedResource = await Context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        Assert.That(deletedResource, Is.Null);

        // Check that all relations are deleted
        List<ResourceAuthorRelation> resourceAuthorRelations = await Context.ResourceAuthorRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        List<ResourceOrganisationRelation> resourceOrganisationRelations = await Context.ResourceOrganisationRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        List<ResourceRegionRelation> resourceRegionRelations = await Context.ResourceRegionRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        List<ResourceTagRelation> resourceTagRelations = await Context.ResourceTagRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        
        Assert.That(resourceAuthorRelations, Is.Empty);
        Assert.That(resourceOrganisationRelations, Is.Empty);
        Assert.That(resourceRegionRelations, Is.Empty);
        Assert.That(resourceTagRelations, Is.Empty);

        // Verify the related entities themselves still exist
        Tag? existingTag = await Context.Tags.FirstOrDefaultAsync(t => t.Id == testTagId);
        Person? existingPerson = await Context.Persons.FirstOrDefaultAsync(p => p.Id == testPersonId);
        Organisation? existingOrg = await Context.Organisations.FirstOrDefaultAsync(o => o.Id == testOrganisationId);
        Region? existingRegion = await Context.Regions.FirstOrDefaultAsync(r => r.Id == testRegionId);
        
        Assert.That(existingTag, Is.Not.Null);
        Assert.That(existingPerson, Is.Not.Null);
        Assert.That(existingOrg, Is.Not.Null);
        Assert.That(existingRegion, Is.Not.Null);
    }

    [Test]
    public async Task DeletePersonAsync_WithValidId_DeletesPersonAndRelations()
    {
        // Arrange
        PersonCreateDto personDto = new PersonCreateDto
        {
            Name = "Person To Delete",
            Occupation = "Test Delete"
        };
        Guid personId = await _resourceManager.CreatePersonAsync(personDto);       
        // Create a resource with this person as author
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Author to Delete",
            Description = "Testing person deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Authors = [personId.ToString()]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeletePersonAsync(personId);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Person should be deleted
        Person? deletedPerson = await Context.Persons.FirstOrDefaultAsync(p => p.Id == personId);
        Assert.That(deletedPerson, Is.Null);

        // Author relation should be deleted
        ResourceAuthorRelation? authorRelation = await Context.ResourceAuthorRelations.FirstOrDefaultAsync(r => r.PersonId == personId);
        Assert.That(authorRelation, Is.Null);
        
        // Resource should still exist
        Resource? resource = await Context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        Assert.That(resource, Is.Not.Null);
    }

    [Test]
    public async Task DeleteOrganisationAsync_WithValidId_DeletesOrganisationAndRelations()
    {
        // Arrange
        OrganisationCreateDto orgDto = new OrganisationCreateDto
        {
            Name = "Organisation To Delete"
        };
        Guid orgId = await _resourceManager.CreateOrganisationAsync(orgDto);
        
        // Create a resource with this organization
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Organisation to Delete",
            Description = "Testing organisation deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Organisations = [(orgId.ToString(), "publisher")]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeleteOrganisationAsync(orgId);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Organisation should be deleted
        Organisation? deletedOrg = await Context.Organisations.FirstOrDefaultAsync(o => o.Id == orgId);
        Assert.That(deletedOrg, Is.Null);

        // Organization relation should be deleted
        ResourceOrganisationRelation? orgRelation = await Context.ResourceOrganisationRelations.FirstOrDefaultAsync(r => r.OrganisationId == orgId);
        Assert.That(orgRelation, Is.Null);
        
        // Resource should still exist
        Resource? resource = await Context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        Assert.That(resource, Is.Not.Null);
    }

    [Test]
    public async Task DeleteTagAsync_WithValidId_DeletesTagAndRelations()
    {
        // Arrange
        TagCreateDto tagDto = new TagCreateDto
        {
            Name = "Tag To Delete",
            CreatedBy = testUserId
        };
        Guid tagId = await _resourceManager.CreateTagAsync(tagDto);
        
        // Create a resource with this tag
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Tag to Delete",
            Description = "Testing tag deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags = [tagId.ToString()]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeleteTagAsync(tagId);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Tag should be deleted
        Tag? deletedTag = await Context.Tags.FirstOrDefaultAsync(t => t.Id == tagId);
        Assert.That(deletedTag, Is.Null);

        // Tag relation should be deleted
        ResourceTagRelation? tagRelation = await Context.ResourceTagRelations.FirstOrDefaultAsync(r => r.TagId == tagId);
        Assert.That(tagRelation, Is.Null);
        
        // Resource should still exist
        Resource? resource = await Context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        Assert.That(resource, Is.Not.Null);
    }

    [Test]
    public async Task DeleteRegionAsync_WithValidId_DeletesRegionAndRelations()
    {
        // Arrange
        RegionCreateDto regionDto = new RegionCreateDto
        {
            Name = "Region To Delete"
        };
        Guid regionId = await _resourceManager.CreateRegionAsync(regionDto);
        
        // Create a resource with this region
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Region to Delete",
            Description = "Testing region deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Regions = [regionId.ToString()]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeleteRegionAsync(regionId);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Region should be deleted
        Region? deletedRegion = await Context.Regions.FirstOrDefaultAsync(r => r.Id == regionId);
        Assert.That(deletedRegion, Is.Null);

        // Region relation should be deleted
        ResourceRegionRelation? regionRelation = await Context.ResourceRegionRelations.FirstOrDefaultAsync(r => r.RegionId == regionId);
        Assert.That(regionRelation, Is.Null);
        
        // Resource should still exist
        Resource? resource = await Context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        Assert.That(resource, Is.Not.Null);
    }

    [Test]
    public async Task DeleteDocumentMetadataAsync_DeletesMetadataForResource()
    {
        // Arrange
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        
        // Create resource
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Document Metadata",
            Description = "Testing metadata deletion",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);

        // Add document metadata
        DocumentMetadata documentMetadata = new DocumentMetadata
        {
            ResourceId = resourceId,
            Abstract = "Useless abstract for test",
        };
        
        await Context.DocumentMetadata.AddAsync(documentMetadata);
        await Context.SaveChangesAsync();
        
        // Act
        bool result = await _resourceManager.DeleteDocumentMetadataAsync(resourceId);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Metadata should be deleted
        DocumentMetadata? deletedMetadata = await Context.DocumentMetadata.FirstOrDefaultAsync(m => m.ResourceId == resourceId);
        Assert.That(deletedMetadata, Is.Null);
        
        // Resource should still exist
        Resource? resource = await Context.Resources.FirstOrDefaultAsync(r => r.Id == resourceId);
        Assert.That(resource, Is.Not.Null);
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


