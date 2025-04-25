using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Controllers;
using KnowledgeBank.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class TagControllerTests : TestBase
{
    private TagsController _controller;
    private ResourceManager _resourceManager;
    private ClaimsPrincipal _regularUser;
    private ClaimsPrincipal _adminUser;
    private Guid _regularUserId;
    private Guid _adminUserId;

    [SetUp]
    public void SetupController()
    {
        _regularUserId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();

        // Create a regular user
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, _regularUserId.ToString())],
            "mock"));

        // Create an admin user
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, _adminUserId.ToString()),
                new Claim(ClaimTypes.Role, "admin")
            ],
            "mock"));

        _resourceManager = new ResourceManager(Context);
        _controller = new TagsController(_resourceManager);
        SetControllerUser(_regularUser); // Default to regular user
    }
    
    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        // Enable extension for text-search-vectors
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""resource-vectors"" ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");
        await DatabaseSeeder.SeedTemplate(context);
    }

    // Mocks switching between users. Need this because some endpoints manually check user
    private void SetControllerUser(ClaimsPrincipal user)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
    
    #region GetAll Tests
    
    // TODO: Tests for:
    //
    //      GetAll, GetAllPaged, GetAllStandardizedPaged, GetAllUser, GetAllUserPaged
    //
    // Can be removed once frontend works with GetTags instead of the old endpoints
    // The tests were all rewritten to work with GetTags

    [Test]
    [Description("GetAll returns all tags from the database")]
    public async Task GetAll_ReturnsOk_WithAllTags()
    {
        // Add some tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag3", CreatedBy = _regularUserId.ToString() });

        // Get all tags
        OkObjectResult? result = await _controller.GetAll() as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(3));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag1"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag2"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag3"));
    }

    [Test]
    [Description("GetAll returns empty array when no tags exist")]
    public async Task GetAll_ReturnsEmptyArray_WhenNoTagsExist()
    {
        // Get all tags
        OkObjectResult? result = await _controller.GetAll() as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(0));
    }

    #endregion
    
    #region GetAllPaged Tests

    [Test]
    [Description("GetAllPaged returns correct page of tags")]
    public async Task GetAllPaged_ReturnsCorrectPage()
    {
        // Add 25 tags
        for (int i = 1; i <= 25; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag{i:D2}", CreatedBy = _regularUserId.ToString() });
        }

        // Get second page with 10 items per page
        OkObjectResult? result = await _controller.GetAllPaged(2, 10) as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(10));
        Assert.That(tags[0].Name, Is.EqualTo("tag11"));
        Assert.That(tags[9].Name, Is.EqualTo("tag20"));
    }

    [Test]
    [Description("GetAllPaged returns partial page when not enough items")]
    public async Task GetAllPaged_ReturnsPartialPage_WhenNotEnoughItems()
    {
        // Add 5 tags
        for (int i = 1; i <= 5; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag{i:D2}", CreatedBy = _regularUserId.ToString() });
        }

        // Get first page with 10 items per page
        OkObjectResult? result = await _controller.GetAllPaged(1, 10) as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(5));
    }

    [Test]
    [Description("GetAllPaged returns empty array for page beyond available items")]
    public async Task GetAllPaged_ReturnsEmptyArray_WhenPageBeyondAvailableItems()
    {
        // Add 5 tags
        for (int i = 1; i <= 5; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag{i:D2}", CreatedBy = _regularUserId.ToString() });
        }

        // Get third page with 5 items per page (should be empty)
        OkObjectResult? result = await _controller.GetAllPaged(3, 5) as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(0));
    }

    #endregion
    
    #region GetAllStandardizedPaged Tests

    [Test]
    [Description("GetAllStandardizedPaged returns correct page of standardized tags")]
    public async Task GetAllStandardizedPaged_ReturnsCorrectPage()
    {
        // Add 15 standardized tags
        for (int i = 1; i <= 15; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"standard tag {i:D2}", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        }
        // Add some non-standardized tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _adminUserId.ToString() });

        // Get second page with 5 items per page
        OkObjectResult? result = await _controller.GetAllStandardizedPaged(2, 5) as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(5));
        Assert.That(tags[0].Name, Is.EqualTo("standard tag 06"));
        Assert.That(tags[4].Name, Is.EqualTo("standard tag 10"));
    }

    #endregion
    
    #region GetAllUser Tests

    [Test]
    [Description("GetAllUser returns only non-standardized tags")]
    public async Task GetAllUser_ReturnsOnlyUserTags()
    {
        // Add some standardized and non-standardized tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std1", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _adminUserId.ToString() });

        // Get all user tags
        OkObjectResult? result = await _controller.GetAllUser() as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(2));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag1"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag2"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("std1"));
    }

    #endregion
    
    #region GetAllUserPaged Tests
    
    [Test]
    [Description("GetAllUserPaged returns correct page of user tags")]
    public async Task GetAllUserPaged_ReturnsCorrectPage()
    {
        // Add 12 user tags
        for (int i = 1; i <= 12; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag {i:D2}", CreatedBy = _adminUserId.ToString() });
        }
        // Add some standardized tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std1", CreatedBy = _adminUserId.ToString() }, true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std2", CreatedBy = _adminUserId.ToString() }, true);

        // Get second page with 5 items per page
        OkObjectResult? result = await _controller.GetAllUserPaged(2, 5) as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Assert.That(tags.Length, Is.EqualTo(5));
        Assert.That(tags[0].Name, Is.EqualTo("tag 06"));
        Assert.That(tags[4].Name, Is.EqualTo("tag 10"));
    }

    #endregion
    
    #region GetTags Tests
    
    [Test]
    [Description("GetTags returns all tags when no filters are applied")]
    public async Task GetTags_NoFilters_ReturnsAllTags()
    {
        // Add some tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag3", CreatedBy = _regularUserId.ToString() });

        // Call GetTags with no filters
        TagFilterOptions filterOptions = new TagFilterOptions();
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(3));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag1"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag2"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag3"));
    }

    [Test]
    [Description("GetTags returns empty array when no tags exist")]
    public async Task GetTags_NoTags_ReturnsEmptyArray()
    {
        // Call GetTags with no filters
        TagFilterOptions filterOptions = new TagFilterOptions();
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(0));
    }

    [Test]
    [Description("GetTags with paging returns correct page of tags")]
    public async Task GetTags_WithPaging_ReturnsCorrectPage()
    {
        // Add 25 tags
        for (int i = 1; i <= 25; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag{i:D2}", CreatedBy = _regularUserId.ToString() });
        }

        // Request second page with 10 items per page
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            UsePaging = true,
            PageIndex = 2,
            PageSize = 10
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(10));
        Assert.That(tags[0].Name, Is.EqualTo("tag11"));
        Assert.That(tags[9].Name, Is.EqualTo("tag20"));
    }

    [Test]
    [Description("GetTags with paging returns partial page when not enough items")]
    public async Task GetTags_WithPaging_ReturnsPartialPage_WhenNotEnoughItems()
    {
        // Add 5 tags
        for (int i = 1; i <= 5; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag{i:D2}", CreatedBy = _regularUserId.ToString() });
        }

        // Request first page with 10 items per page
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            UsePaging = true,
            PageIndex = 1,
            PageSize = 10
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(5));
    }

    [Test]
    [Description("GetTags with paging returns empty array for page beyond available items")]
    public async Task GetTags_WithPaging_ReturnsEmptyArray_WhenPageBeyondAvailableItems()
    {
        // Add 5 tags
        for (int i = 1; i <= 5; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag{i:D2}", CreatedBy = _regularUserId.ToString() });
        }

        // Request third page with 5 items per page (should be empty)
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            UsePaging = true,
            PageIndex = 3,
            PageSize = 5
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(0));
    }

    [Test]
    [Description("GetTags with invalid paging parameters returns BadRequest")]
    public async Task GetTags_WithInvalidPagingParameters_ReturnsBadRequest()
    {
        // Test with invalid page index
        TagFilterOptions filterOptions1 = new TagFilterOptions
        {
            UsePaging = true,
            PageIndex = 0,  // Invalid: less than 1
            PageSize = 10
        };
        
        BadRequestObjectResult? result1 = await _controller.GetTags(filterOptions1) as BadRequestObjectResult;
        
        Assert.That(result1, Is.Not.Null);
        Assert.That(result1.StatusCode, Is.EqualTo(400));
        
        // Test with invalid page size
        TagFilterOptions filterOptions2 = new TagFilterOptions
        {
            UsePaging = true,
            PageIndex = 1,
            PageSize = 0   // Invalid: less than 1
        };
        
        BadRequestObjectResult? result2 = await _controller.GetTags(filterOptions2) as BadRequestObjectResult;
        
        Assert.That(result2, Is.Not.Null);
        Assert.That(result2.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [Description("GetTags filters by standardized tags")]
    public async Task GetTags_FilterByStandardized_ReturnsOnlyStandardizedTags()
    {
        // Add standardized and non-standardized tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std1", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std2", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _regularUserId.ToString() });

        // Request only standardized tags
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            IsStandardized = true
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(2));
        Assert.That(tags.Select(t => t.Name), Does.Contain("std1"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("std2"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("tag1"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("tag2"));
    }

    [Test]
    [Description("GetTags filters by user tags (non-standardized)")]
    public async Task GetTags_FilterByUserTags_ReturnsOnlyUserTags()
    {
        // Add standardized and non-standardized tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std1", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std2", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _regularUserId.ToString() });

        // Request only user tags (non-standardized)
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            IsStandardized = false
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(2));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag1"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("tag2"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("std1"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("std2"));
    }

    [Test]
    [Description("GetTags filters by user tags with paging")]
    public async Task GetTags_FilterByUserTagsWithPaging_ReturnsCorrectPage()
    {
        // Add 12 user tags
        for (int i = 1; i <= 12; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"tag{i:D2}", CreatedBy = _regularUserId.ToString() });
        }
        
        // Add some standardized tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std1", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std2", CreatedBy = _adminUserId.ToString() }, isStandardized: true);

        // Request second page of user tags with 5 items per page
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            IsStandardized = false,
            UsePaging = true,
            PageIndex = 2,
            PageSize = 5
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(5));
        Assert.That(tags[0].Name, Is.EqualTo("tag06"));
        Assert.That(tags[4].Name, Is.EqualTo("tag10"));
    }

    [Test]
    [Description("GetTags filters by standardized tags with paging")]
    public async Task GetTags_FilterByStandardizedTagsWithPaging_ReturnsCorrectPage()
    {
        // Add 15 standardized tags
        for (int i = 1; i <= 15; i++)
        {
            await _resourceManager.CreateTagAsync(new TagCreateDto { Name = $"standard tag {i:D2}", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        }
        
        // Add some non-standardized tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _adminUserId.ToString() });

        // Request second page of standardized tags with 5 items per page
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            IsStandardized = true,
            UsePaging = true,
            PageIndex = 2,
            PageSize = 5
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(5));
        Assert.That(tags[0].Name, Is.EqualTo("standard tag 06"));
        Assert.That(tags[4].Name, Is.EqualTo("standard tag 10"));
    }
    
    [Test]
    [Description("GetTags filter by creator user ID returns only creator's tags")]
    public async Task GetTags_FilterByCreator_ReturnsOnlyCreatorTags()
    {
        // Create tags with different creators
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "admin-tag1", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "admin-tag2", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "user-tag1", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "user-tag2", CreatedBy = _regularUserId.ToString() });

        // Request only admin's tags
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            CreatedBy = _adminUserId
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(2));
        Assert.That(tags.Select(t => t.Name), Does.Contain("admin-tag1"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("admin-tag2"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("user-tag1"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("user-tag2"));
    }

    [Test]
    [Description("GetTags text search filter returns matching tags")]
    public async Task GetTags_TextSearch_ReturnsMatchingTags()
    {
        // Add tags with different names
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "apple", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "applesauce", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "banana", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "cherry", CreatedBy = _regularUserId.ToString() });

        // Search for tags containing "apple"
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            SearchQuery = "apple"
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(2));
        Assert.That(tags.Select(t => t.Name), Does.Contain("apple"));
        Assert.That(tags.Select(t => t.Name), Does.Contain("applesauce"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("banana"));
        Assert.That(tags.Select(t => t.Name), Does.Not.Contain("cherry"));
    }

    [Test]
    [Description("GetTags returns tags with usage count when requested")]
    public async Task GetTags_WithUsageCount_ReturnsTagsWithUsageCounts()
    {
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        // Add a resource
        ResourceCreateDto testDto = new() 
        {
            Title = "Test Resource", 
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow
        };
        Guid resourceId = await _resourceManager.CreateResourceAsync(testDto);
        
        // Create two tags
        Guid tag1Id = await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _regularUserId.ToString() });
        Guid tag2Id = await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _regularUserId.ToString() });
        
        // Add tag to resource
        await _resourceManager.AddTagToResourceAsync(resourceId, tag1Id);
        
        ResourceCreateDto testDto2 = new() 
        {
            Title = "Test Resource 2", 
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow
        };
        Guid resource2Id = await _resourceManager.CreateResourceAsync(testDto2);
        await _resourceManager.AddTagToResourceAsync(resource2Id, tag2Id);
        
        // Get tags with usage count
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            IncludeUsageCount = true
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(2));
        
        Tag tag1 = tags.First(t => t.Name == "tag1");
        Tag tag2 = tags.First(t => t.Name == "tag2");
        
        Assert.That(tag1.UsageCount, Is.EqualTo(1));
        Assert.That(tag2.UsageCount, Is.EqualTo(1));
    }

    [Test]
    [Description("GetTags sorts by specified property")]
    public async Task GetTags_SortByProperty_ReturnsSortedTags()
    {
        // Add tags with different creation dates
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "C-tag", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "A-tag", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "B-tag", CreatedBy = _regularUserId.ToString() });

        // Sort by name ascending
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            SortBy = "Name",
            SortDescending = false
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(3));
        Assert.That(tags[0].Name, Is.EqualTo("A-tag"));
        Assert.That(tags[1].Name, Is.EqualTo("B-tag"));
        Assert.That(tags[2].Name, Is.EqualTo("C-tag"));
        
        // Sort by name descending
        TagFilterOptions filterOptions2 = new TagFilterOptions
        {
            SortBy = "Name",
            SortDescending = true
        };
        
        OkObjectResult? result2 = await _controller.GetTags(filterOptions2) as OkObjectResult;
        
        // Assert
        Assert.That(result2, Is.Not.Null);
        Assert.That(result2.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags2 = result2.Value as Tag[];
        Assert.That(tags2, Is.Not.Null);
        Assert.That(tags2.Length, Is.EqualTo(3));
        Assert.That(tags2[0].Name, Is.EqualTo("C-tag"));
        Assert.That(tags2[1].Name, Is.EqualTo("B-tag"));
        Assert.That(tags2[2].Name, Is.EqualTo("A-tag"));
    }

    [Test]
    [Description("GetTags filters by approval status")]
    public async Task GetTags_FilterByApprovalStatus_ReturnsOnlyApprovedTags()
    {
        // Create approved and non-approved tags
        Guid tag1Id = await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "approved-tag", CreatedBy = _regularUserId.ToString() });
        Guid tag2Id = await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "unapproved-tag", CreatedBy = _regularUserId.ToString() });
        
        // Approve tag1
        SetControllerUser(_adminUser); // Switch to admin to approve
        await _controller.ApproveTag(tag1Id.ToString());
        SetControllerUser(_regularUser); // Switch back to regular user
        
        // Get only approved tags
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            IsApproved = true
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(1));
        Assert.That(tags[0].Name, Is.EqualTo("approved-tag"));
    }

    [Test]
    [Description("GetTags filters by OnlyOwnedByCurrentUser flag")]
    public async Task GetTags_OnlyOwnedByCurrentUser_ReturnsOnlyUserTags()
    {
        // Create tags owned by different users
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "admin-tag", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "user-tag", CreatedBy = _regularUserId.ToString() });
        
        // Get only current user's tags
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            OnlyOwnedByCurrentUser = true
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(1));
        Assert.That(tags[0].Name, Is.EqualTo("user-tag"));
    }

    [Test]
    [Description("GetTags filters by creation date range")]
    public async Task GetTags_FilterByCreationDateRange_ReturnsTagsInRange()
    {
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "old-tag", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "new-tag", CreatedBy = _regularUserId.ToString() });
        
        // Filter by creation date - assuming both tags were created now (during test)
        // We'll use a range that includes both
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            CreatedFromDate = DateTime.UtcNow.AddMinutes(-5),
            CreatedToDate = DateTime.UtcNow.AddMinutes(5)
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(2));
    }

    [Test]
    [Description("GetTags combines multiple filter criteria")]
    public async Task GetTags_MultipleFilters_ReturnsMatchingTags()
    {
        // Add various tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std-apple", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "std-banana", CreatedBy = _adminUserId.ToString() }, isStandardized: true);
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "user-apple", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "user-cherry", CreatedBy = _regularUserId.ToString() });

        // Filter for standardized tags containing "apple"
        TagFilterOptions filterOptions = new TagFilterOptions
        {
            IsStandardized = true,
            SearchQuery = "apple"
        };
        
        OkObjectResult? result = await _controller.GetTags(filterOptions) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag[]? tags = result.Value as Tag[];
        Assert.That(tags, Is.Not.Null);
        Assert.That(tags.Length, Is.EqualTo(1));
        Assert.That(tags[0].Name, Is.EqualTo("std-apple"));
    }

    #endregion
    
    #region AddStandardTag Tests

    [Test]
    [Description("AddStandardTag returns Ok when admin adds a valid tag")]
    public async Task AddStandardTag_ReturnsOk_WhenAdminAddsValidTag()
    {
        // Set user to admin
        SetControllerUser(_adminUser);

        // Add a standard tag
        OkObjectResult? result = await _controller.AddStandardTag(new TagCreateDto { Name = "std1", CreatedBy = _adminUserId.ToString() }) as OkObjectResult;
        Assert.That(result, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Tag[]? tags = await _resourceManager.GetAllTagsAsync(predicate: t => t.IsStandardized);
        
        Assert.That(tags.Length, Is.EqualTo(1));
        Assert.That(tags[0].Name, Is.EqualTo("std1"));
        Assert.That(tags[0].IsStandardized, Is.True);
    }

    [Test]
    [Description("AddStandardTag returns BadRequest when name is empty")]
    public async Task AddStandardTag_ReturnsBadRequest_WhenNameIsEmpty()
    {
        // Set user to admin
        SetControllerUser(_adminUser);

        // Try to add a standard tag with empty name
        BadRequestObjectResult? result = await _controller.AddStandardTag(new TagCreateDto { Name = "", CreatedBy = _regularUserId.ToString() }) as BadRequestObjectResult;
        Assert.That(result, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(400));
        Tag[]? tags = await _resourceManager.GetAllTagsAsync();
        Assert.That(tags.Length, Is.EqualTo(0));
    }

    [Test]
    [Description("AddStandardTag returns Conflict when tag already exists")]
    public async Task AddStandardTag_ReturnsConflict_WhenTagAlreadyExists()
    {
        // Add a tag first
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "duplicate", CreatedBy = _regularUserId.ToString() });

        // Try to add the same tag again
        ObjectResult? result = await _controller.AddStandardTag(new TagCreateDto { Name = "duplicate" }) as ObjectResult;
        Assert.That(result, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(409));
        Tag[]? tags = await _resourceManager.GetAllTagsAsync();
        Assert.That(tags.Length, Is.EqualTo(1));
    }

    #endregion
    
    #region AddTag Tests

    [TestCase("test1")]
    [TestCase("bla1")]
    [Description("Simple test for adding tags")]
    public async Task AddTag_ReturnsOk_WithValidName(string input)
    {
        // Add tag
        ObjectResult? addResponse = await _controller.AddTag(new TagCreateDto { Name = input }) as ObjectResult;
        Assert.That(addResponse, Is.Not.Null);
        
        // Retrieve all tags
        Tag[] allTags = await _resourceManager.GetAllTagsAsync();

        // Check that statuscode is correct, count of tags is 1 and the name is correct
        Assert.That(addResponse.StatusCode, Is.EqualTo(200));
        Assert.That(allTags.Count(tag => tag.Name == input), Is.EqualTo(1));
        Assert.That(allTags.FirstOrDefault(tag => tag.Name == input)?.Name, Is.EqualTo(input));
    }
    
    [TestCase("")]
    [Description("Simple test for adding tags")]
    public async Task AddTag_ReturnsBadRequest_WithInvalidName(string input)
    {
        // Add tag
        BadRequestObjectResult addResponse = (BadRequestObjectResult)await _controller.AddTag(new TagCreateDto { Name = input });
        
        // Retrieve all tags
        Tag[] allTags = await _resourceManager.GetAllTagsAsync();

        // Check that statuscode is correct, count of tags is 0
        Assert.That(addResponse.StatusCode, Is.EqualTo(400));
        Assert.That(allTags.Count(tag => tag.Name == input), Is.EqualTo(0));
    }
    
    [Test]
    [Description("AddTag returns Conflict when tag already exists")]
    public async Task AddTag_ReturnsConflict_WhenTagAlreadyExists()
    {
        // Add a tag first
        await _controller.AddTag(new TagCreateDto { Name = "duplicate" });

        // Try to add the same tag again
        ObjectResult? result = await _controller.AddTag(new TagCreateDto { Name = "duplicate" }) as ObjectResult;
        Assert.That(result, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(409));
        Tag[]? tags = await _resourceManager.GetAllTagsAsync();
        Assert.That(tags.Length, Is.EqualTo(1));
    }
    
    #endregion

    #region DeleteTag Tests

    [TestCase("test")]
    [TestCase("adfjlkasejfiajdfkasdjfaseifajbl")]
    [Description("Simple test for deleting tags")]
    public async Task DeleteTag_ReturnsOk_WhenIdFound(string input)
    {
        // Add the tag
        await _controller.AddTag(new TagCreateDto{Name = input});
        // Retrieve all tags
        Tag[] allTags = await _resourceManager.GetAllTagsAsync();
        // Find the added tag
        string? addedTagId = allTags.FirstOrDefault(tag => tag.Name == input)?.Id.ToString();
        
        // Make sure the added tag has been found
        Assert.That(addedTagId, Is.Not.Null);
        
        // Now we delete and test if the database is empty again
        OkObjectResult delResponse = (OkObjectResult)await _controller.DeleteTag(addedTagId);

        Assert.That(delResponse.StatusCode, Is.EqualTo(200));
        Assert.That(Context.Tags.Count(), Is.EqualTo(0));
    }

    [TestCase("cd34f056-c81a-4906-9f38-315233e83126")]
    [Description("Returns 404 if the tag doesn't exist in the database")]
    public async Task DeleteTag_ReturnsNotFound_WhenTagDoesNotExist(string input)
    {
        // Try to delete tag in an empty database => should fail
        NotFoundObjectResult failedDelResponse = (NotFoundObjectResult)await _controller.DeleteTag(input);

        // Assert that error code is 404 (tag not found)
        Assert.That(failedDelResponse.StatusCode, Is.EqualTo(404));
    }

    [Test]
    [Description("DeleteTag returns Forbidden when user tries to delete tag assigned to resources")]
    public async Task DeleteTag_ReturnsForbidden_WhenDeletingTagAssignedToResources()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "test-tag", CreatedBy = _regularUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "test-tag")).First();
        
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        // Add a resource
        ResourceCreateDto testDto = new() 
        {
            Title = "Test Resource", 
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow
        };

        await _resourceManager.CreateResourceAsync(testDto);
        
        // Find the resource 
        Resource? testResource = await _resourceManager.GetResourceAsync(predicate: r => r.Title == testDto.Title);
        Assert.That(testResource, Is.Not.Null);

        // Add resource tag relation
        await _resourceManager.AddTagToResourceAsync(testResource.Id, tag.Id);
        
        // Try to delete the tag
        ObjectResult? deleteResult = await _controller.DeleteTag(tag.Id.ToString()) as ObjectResult;
        Assert.That(deleteResult, Is.Not.Null);
        
        // Assert
        Assert.That(deleteResult.StatusCode, Is.EqualTo(403));
        Assert.That((await _resourceManager.GetAllTagsAsync()).Length, Is.EqualTo(1));
    }

    [Test]
    [Description("DeleteTag returns OK when admin deletes tag assigned to resources")]
    public async Task DeleteTag_ReturnsOk_WhenAdminDeletesTagAssignedToResources()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "test-tag", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "test-tag")).First();
        
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        // Add a resource
        ResourceCreateDto testDto = new() 
        {
            Title = "Test Resource", 
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow
        };

        await _resourceManager.CreateResourceAsync(testDto);
        
        // Find the resource 
        Resource? testResource = await _resourceManager.GetResourceAsync(predicate: r => r.Title == testDto.Title);
        Assert.That(testResource, Is.Not.Null);

        // Add resource tag relation
        await _resourceManager.AddTagToResourceAsync(testResource.Id, tag.Id);
        
        // Delete the tag as admin
        SetControllerUser(_adminUser);
        OkObjectResult? deleteResult = await _controller.DeleteTag(tag.Id.ToString()) as OkObjectResult;
        Assert.That(deleteResult, Is.Not.Null);
        
        // Assert
        Assert.That(deleteResult.StatusCode, Is.EqualTo(200));
        Assert.That((await _resourceManager.GetAllTagsAsync()).Length, Is.EqualTo(0));
    }
    
    [Test]
    [Description("DeleteTag returns Forbidden when user has not created the tag")]
    public async Task DeleteTag_ReturnsForbidden_WhenIncorrectUser()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "test-tag", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "test-tag")).First();
        
        // Try to delete tag
        ObjectResult? result = await _controller.DeleteTag(tag.Id.ToString()) as ObjectResult;
        Assert.That(result, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(403));
        Tag? unchangedTag = await _resourceManager.GetTagAsync(tag.Id.ToString());
        Assert.That(unchangedTag, Is.Not.Null);
    }

    #endregion
    
    #region ChangeTagName Tests

    [Test]
    [Description("ChangeTagName returns Ok when tag name is changed successfully")]
    public async Task ChangeTagName_ReturnsOk_WhenNameChangedSuccessfully()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "old-name", CreatedBy = _regularUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "old-name")).First();
        
        // Change the tag name
        OkObjectResult? result = await _controller.ChangeTagName(tag.Id.ToString(), "new-name") as OkObjectResult;
        Assert.That(result, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        Tag? updatedTag = await _resourceManager.GetTagAsync(tag.Id.ToString());
        Assert.That(updatedTag, Is.Not.Null);

        Assert.That(updatedTag.Name, Is.EqualTo("new-name"));
    }

    [Test]
    [Description("ChangeTagName returns BadRequest when id is empty")]
    public async Task ChangeTagName_ReturnsBadRequest_WhenIdIsEmpty()
    {
        // Try to change tag name with empty id
        BadRequestObjectResult? result = await _controller.ChangeTagName("", "new-name") as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [Description("ChangeTagName returns BadRequest when new name is empty")]
    public async Task ChangeTagName_ReturnsBadRequest_WhenNewNameIsEmpty()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "old-name", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "old-name")).First();
        
        // Try to change tag name to empty string
        BadRequestObjectResult? result = await _controller.ChangeTagName(tag.Id.ToString(), "") as BadRequestObjectResult;
        Assert.That(result, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(400));
        Tag? unchangedTag = await _resourceManager.GetTagAsync(tag.Id.ToString());
        Assert.That(unchangedTag, Is.Not.Null);
        Assert.That(unchangedTag.Name, Is.EqualTo("old-name"));
    }

    [Test]
    [Description("ChangeTagName returns NotFound when tag does not exist")]
    public async Task ChangeTagName_ReturnsNotFound_WhenTagDoesNotExist()
    {
        // Try to change name of non-existent tag
        NotFoundObjectResult? result = await _controller.ChangeTagName(Guid.NewGuid().ToString(), "new-name") as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
    }

    [Test]
    [Description("ChangeTagName returns Conflict when new name already exists")]
    public async Task ChangeTagName_ReturnsConflict_WhenNewNameAlreadyExists()
    {
        // Add two tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _regularUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _regularUserId.ToString() });
        Tag? tag1 = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag1")).First();
        
        // Try to change tag1's name to tag2
        ObjectResult? result = await _controller.ChangeTagName(tag1.Id.ToString(), "tag2") as ObjectResult;
        Assert.That(result, Is.Not.Null);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(409));
        Tag? unchangedTag = await _resourceManager.GetTagAsync(tag1.Id.ToString());
        Assert.That(unchangedTag, Is.Not.Null);
        Assert.That(unchangedTag.Name, Is.EqualTo("tag1"));
    }
    
    [Test]
    [Description("ChangeTagName returns Forbidden when user has not created the tag")]
    public async Task ChangeTagName_ReturnsForbidden_WhenIncorrectUser()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "old-name", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "old-name")).First();
        
        // Try to change tag name
        ObjectResult? result = await _controller.ChangeTagName(tag.Id.ToString(), "new-name") as ObjectResult;
        Assert.That(result, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(403));
        Tag? unchangedTag = await _resourceManager.GetTagAsync(tag.Id.ToString());
        Assert.That(unchangedTag, Is.Not.Null);
        Assert.That(unchangedTag.Name, Is.EqualTo("old-name"));
    }

    #endregion
    
    #region ApproveTag Tests

    [Test]
    [Description("ApproveTag returns Ok when admin approves a tag")]
    public async Task ApproveTag_ReturnsOk_WhenAdminApprovesTag()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "test-tag", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "test-tag")).First();
        
        // Approve the tag as admin
        SetControllerUser(_adminUser);
        OkObjectResult? result = await _controller.ApproveTag(tag.Id.ToString()) as OkObjectResult;
        Assert.That(result, Is.Not.Null);
        
        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Tag? approvedTag = await _resourceManager.GetTagAsync(tag.Id.ToString());
        Assert.That(approvedTag, Is.Not.Null);
        Assert.That(approvedTag.IsApproved, Is.True);
        Assert.That(approvedTag.ApprovedBy, Is.EqualTo(_adminUserId));
        Assert.That(approvedTag.ApprovedOn, Is.Not.Null);
    }

    [Test]
    [Description("ApproveTag returns BadRequest when id is empty")]
    public async Task ApproveTag_ReturnsBadRequest_WhenIdIsEmpty()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Try to approve tag with empty id
        BadRequestObjectResult? result = await _controller.ApproveTag("") as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [Description("ApproveTag returns NotFound when tag does not exist")]
    public async Task ApproveTag_ReturnsNotFound_WhenTagDoesNotExist()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Try to approve non-existent tag
        NotFoundObjectResult? result = await _controller.ApproveTag(Guid.NewGuid().ToString()) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
    }

    #endregion
    
    #region MakeStandardized Tests

    [Test]
    [Description("MakeStandardized returns Ok when admin standardizes a tag")]
    public async Task MakeStandardized_ReturnsOk_WhenAdminStandardizesTag()
    {
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "test-tag", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "test-tag")).First();
        
        // Standardize the tag as admin
        SetControllerUser(_adminUser);
        OkObjectResult? result = await _controller.MakeStandardized(tag.Id.ToString()) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        Tag? standardizedTag = await _resourceManager.GetTagAsync(tag.Id.ToString());
        Assert.That(standardizedTag, Is.Not.Null);
        Assert.That(standardizedTag.IsStandardized, Is.True);
    }

    [Test]
    [Description("MakeStandardized returns BadRequest when id is empty")]
    public async Task MakeStandardized_ReturnsBadRequest_WhenIdIsEmpty()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Try to standardize tag with empty id
        BadRequestObjectResult? result = await _controller.MakeStandardized("") as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [Description("MakeStandardized returns NotFound when tag does not exist")]
    public async Task MakeStandardized_ReturnsNotFound_WhenTagDoesNotExist()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Try to standardize non-existent tag
        NotFoundObjectResult? result = await _controller.MakeStandardized(Guid.NewGuid().ToString()) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
    }

    #endregion
    
    #region MergeTag Tests

    [Test]
    [Description("Merge returns Ok when admin successfully merges two tags")]
    public async Task Merge_ReturnsOk_WhenAdminMergesTags()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Add two tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _adminUserId.ToString() });
        
        Tag? tag1 = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag1")).First();
        Tag? tag2 = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag2")).First();
        
        // Add a resource
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resourceDto = new()
        {
            Title = "Test Resource",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow
        };
        await _resourceManager.CreateResourceAsync(resourceDto);
        
        // Find the resource 
        Resource? resource = await _resourceManager.GetResourceAsync(predicate: r => r.Title == resourceDto.Title);
        Assert.That(resource, Is.Not.Null);
        
        // Associate tag2 with the resource
        await _resourceManager.AddTagToResourceAsync(resource.Id, tag2.Id);
        
        // Merge tag2 into tag1
        OkObjectResult? result = await _controller.Merge(tag1.Id.ToString(), tag2.Id.ToString()) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        // Check that tag2 is deleted
        Tag? deletedTag = await _resourceManager.GetTagAsync(tag2.Id.ToString());
        Assert.That(deletedTag, Is.Null);
        
        // Check that tag1 still exists
        Tag? remainingTag = await _resourceManager.GetTagAsync(tag1.Id.ToString());
        Assert.That(remainingTag, Is.Not.Null);
        
        // Check that the resource now has tag1 instead of tag2
        ResourceTagRelation[] relations = await _resourceManager.GetAllResourceTagRelationsAsync(
            predicate: r => r.ResourceId == resource.Id);
        
        Assert.That(relations.Length, Is.EqualTo(1));
        Assert.That(relations[0].TagId, Is.EqualTo(tag1.Id));
    }

    [Test]
    [Description("Merge returns Ok when admin merges tags with overlapping resources")]
    public async Task Merge_ReturnsOk_WhenTagsHaveOverlappingResources()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Add two tags
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag1", CreatedBy = _adminUserId.ToString() });
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag2", CreatedBy = _adminUserId.ToString() });
        
        Tag? tag1 = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag1")).First();
        Tag? tag2 = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag2")).First();
        
        // Add two resources
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();
        ResourceCreateDto resource1Dto = new()
        {
            Title = "Resource 1",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow
        };
        ResourceCreateDto resource2Dto = new()
        {
            Title = "Resource 2",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow
        };
        
        await _resourceManager.CreateResourceAsync(resource1Dto);
        await _resourceManager.CreateResourceAsync(resource2Dto);
        
        // Find the resources
        Resource? resource1 = await _resourceManager.GetResourceAsync(predicate: r => r.Title == resource1Dto.Title);
        Resource? resource2 = await _resourceManager.GetResourceAsync(predicate: r => r.Title == resource2Dto.Title);
        Assert.That(resource1, Is.Not.Null);
        Assert.That(resource2, Is.Not.Null);
        
        // Associate tag1 with resource1
        await _resourceManager.AddTagToResourceAsync(resource1.Id, tag1.Id);
        
        // Associate tag2 with both resources
        await _resourceManager.AddTagToResourceAsync(resource1.Id, tag2.Id);
        await _resourceManager.AddTagToResourceAsync(resource2.Id, tag2.Id);
        
        // Merge tag2 into tag1
        OkObjectResult? result = await _controller.Merge(tag1.Id.ToString(), tag2.Id.ToString()) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        // Check that tag2 is deleted
        Tag? deletedTag = await _resourceManager.GetTagAsync(tag2.Id.ToString());
        Assert.That(deletedTag, Is.Null);
        
        // Check that resource1 still has only one tag1 (no duplicates)
        ResourceTagRelation[] relations1 = await _resourceManager.GetAllResourceTagRelationsAsync(
            predicate: r => r.ResourceId == resource1.Id);
        
        Assert.That(relations1.Length, Is.EqualTo(1));
        Assert.That(relations1[0].TagId, Is.EqualTo(tag1.Id));
        
        // Check that resource2 now has tag1
        ResourceTagRelation[] relations2 = await _resourceManager.GetAllResourceTagRelationsAsync(
            predicate: r => r.ResourceId == resource2.Id);
        
        Assert.That(relations2.Length, Is.EqualTo(1));
        Assert.That(relations2[0].TagId, Is.EqualTo(tag1.Id));
    }

    [Test]
    [Description("Merge returns BadRequest when tag IDs are identical")]
    public async Task Merge_ReturnsBadRequest_WhenTagIdsAreIdentical()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Add a tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag")).First();
        
        // Try to merge tag with itself
        BadRequestObjectResult? result = await _controller.Merge(tag.Id.ToString(), tag.Id.ToString()) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [TestCase("", "valid-id")]
    [TestCase("valid-id", "")]
    [TestCase("", "")]
    [Description("Merge returns BadRequest when one or both tag IDs are empty")]
    public async Task Merge_ReturnsBadRequest_WhenTagIdIsEmpty(string id1, string id2)
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Replace "valid-id" with an actual valid GUID if needed
        if (id1 == "valid-id") id1 = Guid.NewGuid().ToString();
        if (id2 == "valid-id") id2 = Guid.NewGuid().ToString();
        
        // Try to merge with empty ID(s)
        BadRequestObjectResult? result = await _controller.Merge(id1, id2) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [TestCase("not-a-guid", "00000000-0000-0000-0000-000000000000")]
    [TestCase("00000000-0000-0000-0000-000000000000", "not-a-guid")]
    [TestCase("not-a-guid-1", "not-a-guid-2")]
    [Description("Merge returns BadRequest when one or both tag IDs are not valid GUIDs")]
    public async Task Merge_ReturnsBadRequest_WhenTagIdIsNotValidGuid(string id1, string id2)
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Try to merge with invalid GUID format
        BadRequestObjectResult? result = await _controller.Merge(id1, id2) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [Description("Merge returns NotFound when first tag does not exist")]
    public async Task Merge_ReturnsNotFound_WhenFirstTagDoesNotExist()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Add one tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag")).First();
        
        // Try to merge non-existent tag with existing tag
        NotFoundObjectResult? result = await _controller.Merge(Guid.NewGuid().ToString(), tag.Id.ToString()) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
    }

    [Test]
    [Description("Merge returns NotFound when second tag does not exist")]
    public async Task Merge_ReturnsNotFound_WhenSecondTagDoesNotExist()
    {
        // Set user to admin
        SetControllerUser(_adminUser);
        
        // Add one tag
        await _resourceManager.CreateTagAsync(new TagCreateDto { Name = "tag", CreatedBy = _adminUserId.ToString() });
        Tag? tag = (await _resourceManager.GetAllTagsAsync(predicate: t => t.Name == "tag")).First();
        
        // Try to merge existing tag with non-existent tag
        NotFoundObjectResult? result = await _controller.Merge(tag.Id.ToString(), Guid.NewGuid().ToString()) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
    }

    #endregion
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


