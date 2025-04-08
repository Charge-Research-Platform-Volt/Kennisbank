using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Controllers;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class ResourceManagerDeleteTests : TestBase
{
    private ResourceManager _resourceManager;
    private Mock<IAzureBlobService> _mockBlobService;

    [SetUp]
    public void SetupController()
    {
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
            CreatedBy = "test"
        };
        await _resourceManager.CreateTagAsync(testTagDto);
        Tag? testTag = await Context.Tags.FirstOrDefaultAsync(t => t.Name == "DeleteTestTag");

        PersonCreateDto testPersonDto = new PersonCreateDto
        {
            Name = "Delete Test Author",
            Occupation = "Test Occupation"
        };
        await _resourceManager.CreatePersonAsync(testPersonDto);
        Person? testPerson = await Context.Persons.FirstOrDefaultAsync(p => p.Name == "Delete Test Author");

        OrganisationCreateDto testOrganisationDto = new OrganisationCreateDto
        {
            Name = "Delete Test Organisation"
        };
        await _resourceManager.CreateOrganisationAsync(testOrganisationDto);
        Organisation? testOrganisation = await Context.Organisations.FirstOrDefaultAsync(o => o.Name == "Delete Test Organisation");

        RegionCreateDto testRegionDto = new RegionCreateDto
        {
            Name = "Delete Test Region"
        };
        await _resourceManager.CreateRegionAsync(testRegionDto);
        Region? testRegion = await Context.Regions.FirstOrDefaultAsync(r => r.Name == "Delete Test Region");

        ResourceType? type = await Context.ResourceTypes.FirstAsync();

        // Create resource with all relationships
        var resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Relations to Delete",
            Description = "This resource and all its relations will be deleted",
            TypeId = type.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags = [testTag.Id.ToString()],
            Authors = [testPerson.Id.ToString()],
            Organisations = [(testOrganisation.Id.ToString(), "partner")],
            Regions = [testRegion.Id.ToString()]
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
        var resourceAuthorRelations = await Context.ResourceAuthorRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        var resourceOrganisationRelations = await Context.ResourceOrganisationRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        var resourceRegionRelations = await Context.ResourceRegionRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        var resourceTagRelations = await Context.ResourceTagRelations.Where(r => r.ResourceId == resourceId).ToListAsync();
        
        Assert.That(resourceAuthorRelations, Is.Empty);
        Assert.That(resourceOrganisationRelations, Is.Empty);
        Assert.That(resourceRegionRelations, Is.Empty);
        Assert.That(resourceTagRelations, Is.Empty);
        
        // Verify the related entities themselves still exist
        var existingTag = await Context.Tags.FirstOrDefaultAsync(t => t.Id == testTag.Id);
        var existingPerson = await Context.Persons.FirstOrDefaultAsync(p => p.Id == testPerson.Id);
        var existingOrg = await Context.Organisations.FirstOrDefaultAsync(o => o.Id == testOrganisation.Id);
        var existingRegion = await Context.Regions.FirstOrDefaultAsync(r => r.Id == testRegion.Id);
        
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
        await _resourceManager.CreatePersonAsync(personDto);
        Person? person = await Context.Persons.FirstOrDefaultAsync(p => p.Name == "Person To Delete");
        
        // Create a resource with this person as author
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Author to Delete",
            Description = "Testing person deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Authors = [person.Id.ToString()]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeletePersonAsync(person.Id);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Person should be deleted
        Person? deletedPerson = await Context.Persons.FirstOrDefaultAsync(p => p.Id == person.Id);
        Assert.That(deletedPerson, Is.Null);
        
        // Author relation should be deleted
        var authorRelation = await Context.ResourceAuthorRelations.FirstOrDefaultAsync(r => r.PersonId == person.Id);
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
        await _resourceManager.CreateOrganisationAsync(orgDto);
        Organisation? org = await Context.Organisations.FirstOrDefaultAsync(o => o.Name == "Organisation To Delete");
        
        // Create a resource with this organization
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Organisation to Delete",
            Description = "Testing organisation deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Organisations = [(org.Id.ToString(), "publisher")]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeleteOrganisationAsync(org.Id);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Organisation should be deleted
        Organisation? deletedOrg = await Context.Organisations.FirstOrDefaultAsync(o => o.Id == org.Id);
        Assert.That(deletedOrg, Is.Null);
        
        // Organization relation should be deleted
        var orgRelation = await Context.ResourceOrganisationRelations.FirstOrDefaultAsync(r => r.OrganisationId == org.Id);
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
            CreatedBy = "test"
        };
        await _resourceManager.CreateTagAsync(tagDto);
        Tag? tag = await Context.Tags.FirstOrDefaultAsync(t => t.Name == "Tag To Delete");
        
        // Create a resource with this tag
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Tag to Delete",
            Description = "Testing tag deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags = [tag.Id.ToString()]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeleteTagAsync(tag.Id);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Tag should be deleted
        Tag? deletedTag = await Context.Tags.FirstOrDefaultAsync(t => t.Id == tag.Id);
        Assert.That(deletedTag, Is.Null);
        
        // Tag relation should be deleted
        var tagRelation = await Context.ResourceTagRelations.FirstOrDefaultAsync(r => r.TagId == tag.Id);
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
        await _resourceManager.CreateRegionAsync(regionDto);
        Region? region = await Context.Regions.FirstOrDefaultAsync(r => r.Name == "Region To Delete");
        
        // Create a resource with this region
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new ResourceCreateDto
        {
            Title = "Resource with Region to Delete",
            Description = "Testing region deletion cascade",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Regions = [region.Id.ToString()]
        };
        
        Guid resourceId = await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Act
        bool result = await _resourceManager.DeleteRegionAsync(region.Id);
        
        // Assert
        Assert.That(result, Is.True);
        
        // Region should be deleted
        Region? deletedRegion = await Context.Regions.FirstOrDefaultAsync(r => r.Id == region.Id);
        Assert.That(deletedRegion, Is.Null);
        
        // Region relation should be deleted
        var regionRelation = await Context.ResourceRegionRelations.FirstOrDefaultAsync(r => r.RegionId == region.Id);
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
        var documentMetadata = new DocumentMetadata
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