using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Controllers;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class ResourceManagerCreateTests : TestBase
{
    private ResourceManager _resourceManager;
    private Mock<IAzureBlobService> _mockBlobService;


    [SetUp]
    public void SetupController()
    {
        _mockBlobService = new Mock<IAzureBlobService>();
        _resourceManager = new ResourceManager(Context);
    }

    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        // Enable extension for text-search-vectors
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""resource-vectors"" ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");
        await DatabaseSeeder.SeedTemplate(context);
    }


    [Test]
    public async Task CreateResourceAsync_WithValidDto_CreatesResourceAndRelatedEntities()
    {
        // Arrange
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        ResourceCreateDto dto = new ResourceCreateDto
        {
            Title = "Integration Test Resource",
            Description = "This resource is created during an integration test.",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow,
        };

        // Act
        Guid resourceId = await _resourceManager.CreateResourceAsync(dto);

        // Assert
        Resource? resource = await Context.Resources
            .Include(r => r.Tags)
            .Include(r => r.Authors)
            .Include(r => r.Organisations)
            .Include(r => r.Regions)
            .FirstOrDefaultAsync(r => r.Id == resourceId);

        // Properties that were set
        Assert.That(resource, Is.Not.Null);
        Assert.That(resource.Title, Is.EqualTo(dto.Title));
        Assert.That(resource.Description, Is.EqualTo(dto.Description));
        Assert.That(resource.TypeId, Is.EqualTo(resourceType.Id));

        // Properties that were not set
        Assert.That(resource.Tags, Is.Empty);
        Assert.That(resource.Authors, Is.Empty);
        Assert.That(resource.Organisations, Is.Empty);
        Assert.That(resource.Regions, Is.Empty);
    }

    [Test]
    public void CreateResourceAsync_WithInvalidTypeId_ThrowsFormatException()
    {
        // Arrange
        ResourceCreateDto dto = new ResourceCreateDto
        {
            Title = "Invalid Type",
            Description = "Should fail",
            TypeId = "not-a-guid",
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow
        };

        // Act + Assert
        Assert.ThrowsAsync<FormatException>(() => _resourceManager.CreateResourceAsync(dto));
    }

    [Test]
    public async Task CreateResourceAsync_WithExistingTagId_CreatesTagRelation()
    {
        // Arrange
        TagCreateDto testTagDto = new TagCreateDto
        {
            Name = "Tag1",
            CreatedBy = "test"
        };
        await _resourceManager.CreateTagAsync(testTagDto);
        Tag? testTag = await Context.Tags.FirstOrDefaultAsync();

        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        ResourceCreateDto dto = new ResourceCreateDto
        {
            Title = "Resource with Existing Tag",
            Description = "Should link to Tag1",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags =  [testTag.Id.ToString()]
        };

        // Act
        Guid resourceId = await _resourceManager.CreateResourceAsync(dto);

        // Assert
        Resource? resource = await Context.Resources
            .Include(r => r.Tags)
            .FirstOrDefaultAsync(r => r.Id == resourceId);

        Assert.That(resource, Is.Not.Null);
        Assert.That(resource.Tags, Has.Count.EqualTo(1));
        Assert.That(resource.Tags.First().TagId, Is.EqualTo(testTag.Id));
    }

    [Test]
    public async Task CreateResourceAsync_WithMultipleRelations_CreatesAllLinks()
    {
        // Arrange
        TagCreateDto testTagDto = new TagCreateDto
        {
            Name = "Tag1",
            CreatedBy = "test"
        };
        await _resourceManager.CreateTagAsync(testTagDto);
        Tag? testTag = await Context.Tags.FirstOrDefaultAsync();

        PersonCreateDto testPersonDto = new PersonCreateDto
        {
            Name = "Test Name",
            Occupation = "Test Occupation"
        };
        await _resourceManager.CreatePersonAsync(testPersonDto);
        Person? testPerson = await Context.Persons.FirstOrDefaultAsync();

        OrganisationCreateDto testOrganisationDto = new OrganisationCreateDto
        {
            Name = "Utrecht University"
        };
        await _resourceManager.CreateOrganisationAsync(testOrganisationDto);
        Organisation? testOrganisation = await Context.Organisations.FirstOrDefaultAsync();

        RegionCreateDto testRegionDto = new RegionCreateDto
        {
            Name = "Utrecht"
        };
        await _resourceManager.CreateRegionAsync(testRegionDto);
        Region? testRegion = await Context.Regions.FirstOrDefaultAsync();

        ResourceType? type = await Context.ResourceTypes.FirstAsync();

        var dto = new ResourceCreateDto
        {
            Title = "Test Resource",
            Description = "Multiple linked entities",
            TypeId = type.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags = [testTag.Id.ToString()],
            Authors = [testPerson.Id.ToString()],
            Organisations = [(testOrganisation.Id.ToString(), "boss")],
            Regions = [testRegion.Id.ToString()]
        };

        // Act
        Guid resourceId = await _resourceManager.CreateResourceAsync(dto);

        // Assert
        Resource? resource = await Context.Resources
            .Include(r => r.Tags)
            .Include(r => r.Authors)
            .Include(r => r.Organisations)
            .Include(r => r.Regions)
            .FirstAsync(r => r.Id == resourceId);


        Assert.That(resource.Tags.Any(t => t.TagId == testTag.Id));
        Assert.That(resource.Authors.Any(a => a.PersonId == testPerson.Id));
        Assert.That(resource.Organisations.Any(o => o.OrganisationId == testOrganisation.Id));
        Assert.That(resource.Regions.Any(r => r.RegionId == testRegion.Id));
    }
}