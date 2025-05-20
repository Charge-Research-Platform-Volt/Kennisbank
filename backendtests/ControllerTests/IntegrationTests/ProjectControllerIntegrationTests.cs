using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Controllers;
using KnowledgeBank.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Responses;
using NUnit.Framework.Internal;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class ProjectControllerTests : TestBase
{
    private ProjectController _controller;
    private ResourceManager _resourceManager;
    private ProjectManager _projectManager;
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
        _projectManager = new ProjectManager(Context);
        _controller = new ProjectController(_projectManager, _resourceManager);
        SetControllerUser(_regularUser); // Default to regular user
    }

    protected override async Task SeedTestDatabase(DatabaseContext context)
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
    #region Test Data
    private static ProjectCreateDto ValidProject = new()
    {
        Title = "The most valid project ever",
        Description = "With a GREAT description to boot as well",
        CreationDate = DateTime.UtcNow,
        DeletionDate = DateTime.UtcNow,
        ProjectType = "root",
        Tags = [],
        Creators = []
    };

    private static ProjectCreateDto NoTitleProject = new()
    {
        Title = "",
        Description = "Well I guess there is no title now, should be invalid",
        CreationDate = DateTime.UtcNow,
        DeletionDate = DateTime.UtcNow,
        ProjectType = "root",
        Tags = [],
        Creators = []
    };

    private static ProjectCreateDto WrongTypeProject = new()
    {
        Title = "At least this one has a title",
        CreationDate = DateTime.UtcNow,
        DeletionDate = DateTime.UtcNow,
        ProjectType = "This is NOT a valid type",
        Tags = [],
        Creators = []
    };

    private static ProjectCreateDto InvalidTagsProject = new()
    {
        Title = "At least this one has a title",
        CreationDate = DateTime.UtcNow,
        DeletionDate = DateTime.UtcNow,
        ProjectType = "This is NOT a valid type",
        Tags = ["ced98544-b8fe-4d1c-b9d1-d04c099c8b5c"],
        Creators = []
    };

    // Static method to provide test cases
    public static IEnumerable<TestCaseData> ProjectCreateDtoTestCases()
    {
        yield return new TestCaseData(ValidProject, 200)
            .SetName("CreateProject_Valid_Returns200Ok");
        yield return new TestCaseData(NoTitleProject, 400)
            .SetName("CreateProject_NoTitle_ReturnsBadRequest");
        yield return new TestCaseData(WrongTypeProject, 400)
            .SetName("CreateProject_WrongType_ReturnsBadRequest");
        yield return new TestCaseData(InvalidTagsProject, 400)
            .SetName("CreateProject_InvalidTags_ReturnsBadRequest");
    }

    #endregion

    #region Create Project

    [TestCaseSource(nameof(ProjectCreateDtoTestCases))]
    [Description("Creating a project results in the correct status code")]
    public async Task CreateProjectTest(ProjectCreateDto dto, int statusCode)
    {
        ObjectResult objRes = (ObjectResult)await _controller.Create(dto);
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(statusCode));
    }

    [Test]
    [Description("Creating the same project twice will result in a conflict status code")]
    public async Task CreateProjectConflictTest()
    {
        await _controller.Create(ValidProject);
        ObjectResult objRes = (ObjectResult)await _controller.Create(ValidProject);
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(409));
    }

    [Test]
    [Description("Uploading valid dto results in project being created in the database")]
    public async Task CreateProjectOkTest()
    {
        TagCreateDto newTag = new()
        {
            Name = "Amazing tag",
            CreatedBy = _regularUserId.ToString()
        };

        Guid tagId = await _resourceManager.CreateTagAsync(newTag);
        ProjectCreateDto project = new()
        {
            Title = "The (second) most valid project ever",
            Description = "With a (less) GREAT description to boot as well",
            CreationDate = DateTime.UtcNow,
            DeletionDate = DateTime.UtcNow,
            ProjectType = "root",
            Tags = [tagId.ToString()],
            Creators = []
        };
        ObjectResult objRes = (ObjectResult)await _controller.Create(project);
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(200));

        Assert.That((await _projectManager.GetAllProjectsAsync()).Length, Is.EqualTo(1)); // 1 project was added, we assume this is correct

        Assert.That((await _projectManager.GetAllCreators()).Length, Is.EqualTo(0)); // no added users due to having to create a new one in the database (there is no function for it)
        Assert.That(Context.ProjectCreatorRelations.Count(rel => rel.CreatorId == _regularUserId.ToString()), Is.EqualTo(0));

        Assert.That((await _projectManager.GetAllTags()).Length, Is.EqualTo(1)); // only added 1 project-tag relation
        Assert.That(Context.ProjectTagRelations.Count(rel => rel.TagId == tagId), Is.EqualTo(1));
    }

    #endregion

    #region Delete project
    [Test]
    [Description("Unable to delete project when it doesn't exist or the ID is invalid")]
    public async Task DeleteProjectFailTest()
    {
        ObjectResult objRes = (ObjectResult)await _controller.Delete("this is not a valid ID at ALL");
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(500)); // Cannot parse to GUID

        objRes = (ObjectResult)await _controller.Delete(null);
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(400)); // Handled by the first case

        objRes = (ObjectResult)await _controller.Delete("ced98544-b8fe-4d1c-b9d1-d04c099d8b5c");
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(404)); // Valid GUID, but doesn't exist
    }

    [Test]
    [Description("Deleting project deletes subfolders, and all references of itself and the subfolders")]
    public async Task DeleteProjectOk()
    {

    }

    

    // test delete statuscodes
    // test normal delete deletes all references
    // test cascading delete deleetes everything as well
    #endregion
}