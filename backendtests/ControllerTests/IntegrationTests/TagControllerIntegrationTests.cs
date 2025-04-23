using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Controllers;
using KnowledgeBank.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

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

    // Switches between users
    private void SetControllerUser(ClaimsPrincipal user)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }
    
    #region GetAll Tests

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
        Tag[]? tags = result.Value as Tag[];

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

    
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


