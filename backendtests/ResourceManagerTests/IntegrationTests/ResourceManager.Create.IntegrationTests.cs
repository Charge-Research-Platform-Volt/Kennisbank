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
public class ResourceManagerCreateTests : TestBase
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
        var userStore = new UserStore<User>(Context);
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
            .Include(r => r.TagRelations)
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
        Assert.That(resource.TagRelations, Is.Empty);
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
        TagCreateDto testTagDto = new()
        {
            Name = "testTag",
            CreatedBy = testUserId,
        };

        Guid testTagId = await _resourceManager.CreateTagAsync(testTagDto);

        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        ResourceCreateDto dto = new ResourceCreateDto
        {
            Title = "Resource with Existing Tag",
            Description = "Should link to Tag1",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags = [testTagId.ToString()]
        };

        // Act
        Guid resourceId = await _resourceManager.CreateResourceAsync(dto);

        // Assert
        Resource? resource = await Context.Resources
            .Include(r => r.TagRelations)
            .FirstOrDefaultAsync(r => r.Id == resourceId);

        Assert.That(resource, Is.Not.Null);
        Assert.That(resource.TagRelations, Has.Count.EqualTo(1));
        Assert.That(resource.TagRelations.First().TagId, Is.EqualTo(testTagId));
    }

    [Test]
    public async Task CreateResourceAsync_WithMultipleRelations_CreatesAllLinks()
    {
        // Arrange
        TagCreateDto testTagDto = new TagCreateDto
        {
            Name = "Tag1",
            CreatedBy = testUserId
        };
        Guid testTagId = await _resourceManager.CreateTagAsync(testTagDto);

        PersonCreateDto testPersonDto = new PersonCreateDto
        {
            Name = "Test Name",
            Occupation = "Test Occupation"
        };
        Guid testPersonId = await _resourceManager.CreatePersonAsync(testPersonDto);

        OrganisationCreateDto testOrganisationDto = new OrganisationCreateDto
        {
            Name = "Utrecht University"
        };
        Guid testOrganisationId = await _resourceManager.CreateOrganisationAsync(testOrganisationDto);

        RegionCreateDto testRegionDto = new RegionCreateDto
        {
            Name = "Utrecht"
        };
        Guid testRegionId = await _resourceManager.CreateRegionAsync(testRegionDto);

        ResourceType? type = await Context.ResourceTypes.FirstAsync();

        var dto = new ResourceCreateDto
        {
            Title = "Test Resource",
            Description = "Multiple linked entities",
            TypeId = type.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            Tags = [testTagId.ToString()],
            Authors = [testPersonId.ToString()],
            Organisations = [(testOrganisationId.ToString(), "boss")],
            Regions = [testRegionId.ToString()]
        };

        // Act
        Guid resourceId = await _resourceManager.CreateResourceAsync(dto);

        // Assert
        Resource? resource = await Context.Resources
            .Include(r => r.TagRelations)
            .Include(r => r.Authors)
            .Include(r => r.Organisations)
            .Include(r => r.Regions)
            .FirstAsync(r => r.Id == resourceId);


        Assert.That(resource.TagRelations.Any(t => t.TagId == testTagId));
        Assert.That(resource.Authors.Any(a => a.PersonId == testPersonId));
        Assert.That(resource.Organisations.Any(o => o.OrganisationId == testOrganisationId));
        Assert.That(resource.Regions.Any(r => r.RegionId == testRegionId));
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


