#nullable disable

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
using System.Xml;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Text.Json;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class ProjectControllerTests : TestBase
{
    private ProjectController _controller;
    private ResourceManager _resourceManager;
    private ProjectManager _projectManager;
    private UserStore<User> _userStore;
    private UserManager<User> _userManager;
    private ClaimsPrincipal _regularUser;
    private ClaimsPrincipal _adminUser;
    private Guid _regularUserId;
    private Guid _adminUserId;

    [SetUp]
    public async Task SetupController()
    {
        _userStore = new UserStore<User>(Context);

        _userManager = new UserManager<User>(
           _userStore,
            null,
            new PasswordHasher<User>(),
            new[] { new UserValidator<User>() },
            new[] { new PasswordValidator<User>() },
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null,
            null
        );

        User admin = new User("admin", "admin", "admin@test.nl");
        User user = new User("user", "user", "user@test.nl");

        await _userManager.CreateAsync(admin, "Test123!");
        await _userManager.CreateAsync(user, "Test123!");

        _regularUserId = Guid.Parse(Context.Users.Where(u => u.Email == "user@test.nl").Select(u => u.Id).First());
        _adminUserId = Guid.Parse(Context.Users.Where(u => u.Email == "admin@test.nl").Select(u => u.Id).First());

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
    protected override Task OnTestTearDown()
    {
        _userManager?.Dispose();
        _userStore?.Dispose();
        ValidProject.Creators = [];
        ValidProject2.Creators = [];
        return base.OnTestTearDown();
    }

    #region Test Data
    private static ProjectCreateDto ValidProject = new()
    {
        Title = "The most valid project ever",
        Description = "With a GREAT description to boot as well",
        ProjectType = "root",
        Tags = [],
        Creators = []
    };

    private static ProjectCreateDto ValidProject2 = new()
    {
        Title = "The (second) most valid project ever",
        Description = "With a (less) GREAT description",
        ProjectType = "root",
        Tags = [],
        Creators = []
    };

    private static ProjectCreateDto NoTitleProject = new()
    {
        Title = "",
        Description = "Well I guess there is no title now, should be invalid",
        ProjectType = "root",
        Tags = [],
        Creators = []
    };

    private static ProjectCreateDto WrongTypeProject = new()
    {
        Title = "At least this one has a title",
        ProjectType = "This is NOT a valid type",
        Tags = [],
        Creators = []
    };

    private static ProjectCreateDto InvalidTagsProject = new()
    {
        Title = "At least this one has a title",
        ProjectType = "This is NOT a valid type",
        Tags = ["ced98544-b8fe-4d1c-b9d1-d04c099c8b5c"],
        Creators = []
    };

    private static ResourceCreateDto MockResource = new()
    {
        Title = "mock resource",
        TypeId = DatabaseSeeder.UnknownResourceTypeId,
        LanguageCode = "NL",
        PublicationDate = DateTime.UtcNow,
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
            ProjectType = "root",
            Tags = [tagId.ToString()],
            Creators = []
        };
        ObjectResult objRes = (ObjectResult)await _controller.Create(project);
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(200));

        Assert.That((await _projectManager.GetAllProjectsAsync()).Length, Is.EqualTo(1)); // 1 project was added, we assume this is correct

        Assert.That((await _projectManager.GetAllCreators()).Length, Is.EqualTo(1)); // should be 1 creator
        Assert.That(Context.ProjectCreatorRelations.Count(rel => rel.CreatorId == _regularUserId.ToString()), Is.EqualTo(1));

        Assert.That((await _projectManager.GetAllTags()).Length, Is.EqualTo(1)); // only added 1 project-tag relation
        Assert.That(Context.ProjectTagRelations.Count(rel => rel.TagId == tagId), Is.EqualTo(1));
    }

    #endregion

    #region Delete Project

    [Test]
    [Description("Unable to delete project when it doesn't exist or the ID is invalid")]
    public async Task DeleteProjectFailTest()
    {
        SetControllerUser(_adminUser);
        ObjectResult objRes = (ObjectResult)await _controller.Delete("this is not a valid ID at ALL");
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(400)); // Cannot parse to GUID

        objRes = (ObjectResult)await _controller.Delete(null);
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(400)); // Handled by the first case

        objRes = (ObjectResult)await _controller.Delete("ced98544-b8fe-4d1c-b9d1-d04c099d8b5c");
        Assert.That(objRes.StatusCode ?? -1, Is.EqualTo(404)); // Valid GUID, but doesn't exist

        OkObjectResult res = (OkObjectResult)await _controller.Create(ValidProject);
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        SetControllerUser(_regularUser);

        objRes = (ObjectResult)await _controller.Delete(projectId);
        Assert.That(objRes.StatusCode, Is.EqualTo(403)); // Wrong user tries to delete the project
    }

    [Test]
    [Description("Deleting project deletes subfolders, and all references of itself and the subfolders")]
    public async Task DeleteProjectOk()
    {
        SetControllerUser(_adminUser);
        OkObjectResult res = (OkObjectResult)await _controller.Create(ValidProject);
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        ObjectResult objRes = (ObjectResult)await _controller.Delete(projectId);
        Assert.That(objRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.Projects.Count, Is.EqualTo(0));
    }

    [Test]
    [Description("Tests if the delete is cascading and deletes all references (project-folder, project-tag, project-creator and project-resource)")]
    public async Task DeleteProjectReferencesSuccess()
    {
        SetControllerUser(_regularUser);

        TagCreateDto newTag = new()
        {
            Name = "Amazing tag",
            CreatedBy = _regularUserId.ToString()
        };

        Guid tagId = await _resourceManager.CreateTagAsync(newTag); // Add a tag and creator to the root
        ProjectCreateDto dto = ValidProject;
        dto.Tags = [tagId.ToString()];

        OkObjectResult res = (OkObjectResult)await _controller.Create(ValidProject);
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        ObjectResult folderRes = (ObjectResult)await _controller.AddFolder("testfolder", projectId); // Add a folder to the root
        string folderId = ((ApiResponse)folderRes.Value).Body.ToString();

        Guid resourceId = await _resourceManager.CreateResourceAsync(MockResource);
        await _controller.AddResource(projectId, resourceId.ToString()); // Add a resource to the project
        await _controller.AddResource(folderId, resourceId.ToString()); // Add a resource to the folder

        await _controller.Delete(projectId); // Delete root folder

        // Now all tables relating to folders should be empty
        Assert.That(Context.Projects.Count, Is.EqualTo(0));
        Assert.That(Context.ProjectCreatorRelations.Count, Is.EqualTo(0));
        Assert.That(Context.ProjectFolderRelations.Count, Is.EqualTo(0));
        Assert.That(Context.ProjectResourceRelations.Count, Is.EqualTo(0));
        Assert.That(Context.ProjectTagRelations.Count, Is.EqualTo(0));

        // Reset values since dto is just a reference
        dto.Tags = [];
    }

    #endregion

    #region Project-Resource

    [Test]
    [Description("Tests if adding a valid resource results in a 200 status code and the relation can be found")]
    public async Task AddResourceOk()
    {
        OkObjectResult res = (OkObjectResult)await _controller.Create(ValidProject);
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Guid resourceId = await _resourceManager.CreateResourceAsync(MockResource);
        ObjectResult addRes = (ObjectResult)await _controller.AddResource(projectId, resourceId.ToString()); // Add a resource to the project

        Assert.That(addRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.ProjectResourceRelations.Count, Is.EqualTo(1));
        Assert.That(Context.ProjectResourceRelations.First().ResourceId == resourceId);
    }

    [Test]
    [Description("Tests for failed resource linking")]
    public async Task AddResourceFail()
    {
        ObjectResult addRes;

        // 400 status codes
        addRes = (ObjectResult)await _controller.AddResource(null, "id"); // invalid project id should be caught
        Assert.That(addRes.StatusCode, Is.EqualTo(400));

        addRes = (ObjectResult)await _controller.AddResource("id", null); // invalid resource id should be caught
        Assert.That(addRes.StatusCode, Is.EqualTo(400));

        // 404 status codes
        addRes = (ObjectResult)await _controller.AddResource("b8b73c43-4e81-4e09-adef-8a80f1c0da07", "38c89782-ebd4-4b20-94e4-50493e92c5cb"); // project id not found
        Assert.That(addRes.StatusCode, Is.EqualTo(404));

        OkObjectResult res = (OkObjectResult)await _controller.Create(ValidProject);
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();
        addRes = (ObjectResult)await _controller.AddResource(projectId, "38c89782-ebd4-4b20-94e4-50493e92c5cb"); // resource id not found
        Assert.That(addRes.StatusCode, Is.EqualTo(404));

        // 409 status codes
        Guid resourceId = await _resourceManager.CreateResourceAsync(MockResource);
        await _controller.AddResource(projectId, resourceId.ToString()); // Add a resource to the project
        addRes = (ObjectResult)await _controller.AddResource(projectId, resourceId.ToString()); // Add a resource to the project AGAIN, which should result in a conflict
        Assert.That(addRes.StatusCode, Is.EqualTo(409));
    }

    [Test]
    [Description("Tests for failed resource deletion")]
    public async Task DeleteResourceFail()
    {
        ObjectResult delRes;

        // 400 status codes
        delRes = (ObjectResult)await _controller.RemoveResource(null, "id"); // invalid project id should be caught
        Assert.That(delRes.StatusCode, Is.EqualTo(400));

        delRes = (ObjectResult)await _controller.RemoveResource("id", null); // invalid resource id should be caught
        Assert.That(delRes.StatusCode, Is.EqualTo(400));

        // 404 status codes
        delRes = (ObjectResult)await _controller.RemoveResource("b8b73c43-4e81-4e09-adef-8a80f1c0da07", "38c89782-ebd4-4b20-94e4-50493e92c5cb"); // project id not found
        Assert.That(delRes.StatusCode, Is.EqualTo(404));

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject);
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();
        delRes = (ObjectResult)await _controller.RemoveResource(projectId, "38c89782-ebd4-4b20-94e4-50493e92c5cb"); // resource id not found
        Assert.That(delRes.StatusCode, Is.EqualTo(404));
    }

    [Test]
    [Description("Tests if resource link deletion goes through given valid resource & project id")]
    public async Task DeleteResourceAdminOk()
    {
        SetControllerUser(_adminUser);

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Guid resourceId = await _resourceManager.CreateResourceAsync(MockResource);
        await _controller.AddResource(projectId, resourceId.ToString()); // Add a resource to the project

        ObjectResult delRes = (ObjectResult)await _controller.RemoveResource(projectId, resourceId.ToString()); // resource id not found
        Assert.That(delRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.ProjectResourceRelations.Count, Is.EqualTo(0));
    }

    [Test]
    [Description("Tests if resource deletion fails if user is not creator of resource and not an admin")]
    public async Task DeleteResourceCreatorCheckFail()
    {
        SetControllerUser(_adminUser);
        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Guid resourceId = await _resourceManager.CreateResourceAsync(MockResource);
        await _projectManager.AddResourceToProjectAsync(projectId, resourceId, _adminUserId); // Suppose it WAS added by someone else

        SetControllerUser(_regularUser); // Switch to normal user
        ObjectResult delRes = (ObjectResult)await _controller.RemoveResource(projectId, resourceId.ToString()); // now try to delete someone else's resource
        Assert.That(delRes.StatusCode, Is.EqualTo(403));
    }

    [Test]
    [Description("Tests if resource deletion succeeds if user is creator of resource and not an admin")]
    public async Task DeleteResourceCreatorCheckOk()
    {
        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Guid resourceId = await _resourceManager.CreateResourceAsync(MockResource);
        await _controller.AddResource(projectId, resourceId.ToString());

        ObjectResult delRes = (ObjectResult)await _controller.RemoveResource(projectId, resourceId.ToString()); // now try to delete your own resource
        Assert.That(delRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.ProjectResourceRelations.Count, Is.EqualTo(0));
    }
    #endregion

    #region Fetch Projects

    [Test]
    [Description("Get function returns all projects when no filters are applied")]
    public async Task ListProjects_NoFilters_ReturnsAllProjects()
    {
        // Add a project and a folder to test on
        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject);
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();
        await _controller.Create(ValidProject2);
        await _controller.AddFolder("NOT ROOT", projectId);

        // Call list with no filters
        FilterProjectDto filterOptions = new FilterProjectDto();
        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(2));
        Assert.That(projects.Select(t => t.Title), Does.Contain("The most valid project ever"));
        Assert.That(projects.Select(t => t.Title), Does.Contain("The (second) most valid project ever"));
    }

    [Test]
    [Description("Get function returns empty array when no projects exist")]
    public async Task GetProjects_NoProjects_ReturnsEmptyArray()
    {
        // Call List with no filters
        FilterProjectDto filterOptions = new FilterProjectDto();
        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        ProjectPageResponse tagPageResponse = (ProjectPageResponse)response.Body;

        Project[] projects = tagPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(0));
    }

    [Test]
    [Description("Get function with paging returns correct page of projects")]
    public async Task GetProjects_WithPaging_ReturnsCorrectPage()
    {
        // Add 25 projects
        for (int i = 1; i <= 25; i++)
        {
            await _controller.Create(new ProjectCreateDto
            {
                Title = $"Project {i}",
                ProjectType = "root"
            });
        }

        // Request second page with 10 items per page
        FilterProjectDto filterOptions = new FilterProjectDto
        {
            UsePaging = true,
            PageIndex = 2,
            PageSize = 10
        };

        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(10));
        Assert.That(projects[0].Title, Is.EqualTo("Project 11"));
        Assert.That(projects[9].Title, Is.EqualTo("Project 20"));
    }

    [Test]
    [Description("Get function with paging returns partial page of projects when there aren't enough items")]
    public async Task GetProjects_WithPaging_ReturnsPartialPage_WhenNotEnoughItems()
    {
        // Add 5 projects
        for (int i = 1; i <= 5; i++)
        {
            await _controller.Create(new ProjectCreateDto
            {
                Title = $"Project {i}",
                ProjectType = "root"
            });
        }

        // Request first page with 10 items per page
        FilterProjectDto filterOptions = new FilterProjectDto
        {
            UsePaging = true,
            PageIndex = 1,
            PageSize = 10
        };

        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(5));
        Assert.That(projects[0].Title, Is.EqualTo("Project 1"));
        Assert.That(projects[4].Title, Is.EqualTo("Project 5"));
    }

    [Test]
    [Description("Get function with paging returns bad request if page index is invalid")]
    public async Task GetProjects_WithPaging_Returns400_WithInvalidPageIndex()
    {
        // Add 5 projects
        for (int i = 1; i <= 5; i++)
        {
            await _controller.Create(new ProjectCreateDto
            {
                Title = $"Project {i}",
                ProjectType = "root"
            });
        }

        // Request third page
        FilterProjectDto filterOptions = new FilterProjectDto
        {
            UsePaging = true,
            PageIndex = 3,
            PageSize = 10
        };

        BadRequestObjectResult? result = await _controller.List(filterOptions) as BadRequestObjectResult;

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [Description("Get function with paging returns bad request if the filter is not correct")]
    public async Task GetProjects_WithPaging_Returns400_WithInvalidFilterParams()
    {
        // Request page 0
        FilterProjectDto filterOptions = new FilterProjectDto
        {
            UsePaging = true,
            PageIndex = 0,
            PageSize = 15
        };

        BadRequestObjectResult? result = await _controller.List(filterOptions) as BadRequestObjectResult;

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(400));

        // Request size 0
        filterOptions = new FilterProjectDto
        {
            UsePaging = true,
            PageIndex = 1,
            PageSize = 0
        };

        result = await _controller.List(filterOptions) as BadRequestObjectResult;

        // Assert again
        Assert.That(result.StatusCode, Is.EqualTo(400));
    }

    [Test]
    [Description("Get function filters correctly on projects given tags")]
    public async Task GetProjects_FiltersCorrectly_UsingTags()
    {
        TagCreateDto newTag = new()
        {
            Name = "Amazing tag",
            CreatedBy = _regularUserId.ToString()
        };

        Guid tagId = await _resourceManager.CreateTagAsync(newTag);

        // Create a project with this tag and one without
        ObjectResult res = (ObjectResult)await _controller.Create(new ProjectCreateDto
        {
            Title = "test project",
            ProjectType = "root",
            Tags = [tagId.ToString()]
        });

        await _controller.Create(ValidProject2);
        FilterProjectDto filterOptions = new FilterProjectDto
        {
            Tags = [tagId]
        };

        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(1));
        Assert.That(projects[0].Title, Is.EqualTo("test project"));
    }

    [Test]
    [Description("Get function filters correctly on projects given creators")]
    public async Task GetProjects_filtersCorrectly_UsingCreators()
    {
        // We need the custom user again since it is neccesary for a user being in the database
        User creatorUser = new User("test", "test", "test@test.nl");
        await _userManager.CreateAsync(creatorUser, "Test123!");
        string userId = Context.Users.First().Id;

        // Create a project with this creator and one without
        ObjectResult res = (ObjectResult)await _controller.Create(new ProjectCreateDto
        {
            Title = "test project",
            ProjectType = "root",
            Creators = [userId.ToString()]
        });

        await _controller.Create(ValidProject2);

        FilterProjectDto filterOptions = new FilterProjectDto
        {
            CreatedBy = userId.ToString()
        };

        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(1));
        Assert.That(projects[0].Title, Is.EqualTo("test project"));
    }

    [Test]
    [Description("Get function filters correctly on projects given dates to filter between")]
    public async Task GetProjects_filtersCorrectly_UsingDateTime()
    {
        // Create a project with some date 10 years ago and one with current date
        DateTime creationDate = DateTime.Parse("Jan 31, 2009").ToUniversalTime();
        ObjectResult res = (ObjectResult)await _controller.Create(new ProjectCreateDto
        {
            Title = "test project",
            ProjectType = "root",
        });

        // Manually alter creation date for filtering purposes
        await _projectManager.UpdateProjectAsync(((ApiResponse)res.Value).Body.ToString(), t => t.CreationDate, creationDate);

        await _controller.Create(ValidProject2);

        FilterProjectDto filterOptions = new FilterProjectDto
        {
            StartDate = DateTime.Parse("Jan 1, 2009").ToUniversalTime(),
            EndDate = DateTime.Parse("Feb 1, 2009").ToUniversalTime()
        };

        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(1));
        Assert.That(projects[0].Title, Is.EqualTo("test project"));
    }

    [Test]
    [Description("Get function filters correctly on projects given a search query to filter on")]
    public async Task GetProjects_filtersCorrectly_UsingSearchQuery()
    {
        await _controller.Create(ValidProject);
        await _controller.Create(ValidProject2);

        FilterProjectDto filterOptions = new FilterProjectDto
        {
            SearchQuery = "ThE MoSt"
        };

        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(1));
        Assert.That(projects[0].Title, Is.EqualTo(ValidProject.Title));
    }

    [Test]
    [Description("Get function filters correctly using multiple filters and paging")]
    public async Task GetProjects_filtersCorrectly_UsingMultiple_AndPaging()
    {
        // We need the custom user again since it is neccesary for a user being in the database
        User creatorUser = new User("test", "test", "test@test.nl");
        await _userManager.CreateAsync(creatorUser, "Test123!");
        string userId = Context.Users.First().Id;

        TagCreateDto newTag = new()
        {
            Name = "Amazing tag",
            CreatedBy = _regularUserId.ToString()
        };

        Guid tagId = await _resourceManager.CreateTagAsync(newTag);

        // Add 25 projects, giving uneven numbered projects the same tag
        for (int i = 1; i <= 25; i++)
        {
            if (i % 2 == 0)
            {
                await _controller.Create(new ProjectCreateDto
                {
                    Title = $"Project {i}",
                    ProjectType = "root",
                    Creators = [userId]
                });
            }
            else
            {
                await _controller.Create(new ProjectCreateDto
                {
                    Title = $"Project {i}",
                    ProjectType = "root",
                    Tags = [tagId.ToString()],
                    Creators = [userId]
                });
            }
        }

        // Search on tag + creator + search query of starting with 1
        // Returns 1, 11, 13, 15, 17 and 19

        // Request second page with 10 items per page
        FilterProjectDto filterOptions = new FilterProjectDto
        {
            UsePaging = true,
            PageIndex = 2,
            PageSize = 3,
            SearchQuery = "PrOjeCt 1",
            Tags = [tagId],
            CreatedBy = userId
        };

        OkObjectResult? result = await _controller.List(filterOptions) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);

        ProjectPageResponse projectPageResponse = (ProjectPageResponse)response.Body;
        Project[] projects = projectPageResponse.Projects;
        Assert.That(projects, Is.Not.Null);
        Assert.That(projects.Length, Is.EqualTo(3));
        Assert.That(projects[0].Title, Is.EqualTo("Project 15"));
        Assert.That(projects[2].Title, Is.EqualTo("Project 19"));
    }

    #endregion

    #region Get Content
    [Test]
    [Description("Fetching of content fails")]
    public async Task FetchProjectContentFail()
    {
        // 400 status codes
        ObjectResult fetchRes = (ObjectResult)await _controller.Info(null);
        Assert.That(fetchRes.StatusCode, Is.EqualTo(400));

        // 404 status codes
        fetchRes = (ObjectResult)await _controller.Info("77c74225-ce6c-478c-b2cb-87eaefab8e79");
        Assert.That(fetchRes.StatusCode, Is.EqualTo(404));
    }

    [Test]
    [Description("Tests if the fetching of content works correctly, resources of child folders should NOT be fetched")]
    public async Task FetchProjectContentSuccess()
    {
        string creatorUserName = Context.Users.Where(u => u.Id == _regularUserId.ToString()).Select(u => u.UserName).ToArray()[0];
        // We need the custom user again since it'll auto add the current user when creating a folder
        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        ObjectResult addRes = (ObjectResult)await _controller.AddFolder("folder", projectId); // create folder in project
        ApiResponse addResp = (ApiResponse)addRes.Value;
        string folderId = addResp.Body.ToString();

        Guid resourceId = await _resourceManager.CreateResourceAsync(MockResource);
        await _controller.AddResource(projectId, resourceId.ToString()); // Add a resource to the project

        Guid resourceId2 = await _resourceManager.CreateResourceAsync(MockResource);
        await _controller.AddResource(folderId, resourceId2.ToString()); // Add a resource to the subfolder

        // Check if root fetch of content is correct
        ObjectResult fetchRes = (ObjectResult)await _controller.Info(projectId);
        ProjectInfoDto dto = (ProjectInfoDto)((ApiResponse)fetchRes.Value).Body;
        Assert.That(dto.Project.Id.ToString(), Is.EqualTo(projectId));
        Assert.That(dto.Folders.Count, Is.EqualTo(1));
        Assert.That(dto.Folders.First().Folder.Id.ToString(), Is.EqualTo(folderId));
        Assert.That(dto.Resources.Count, Is.EqualTo(1));
        Assert.That(dto.Resources.First().Resource.Id, Is.EqualTo(resourceId));
        Assert.That(dto.Resources.First().AddedBy, Is.EqualTo(creatorUserName));

        // Then check if fetching content from the folder in root goes correctly
        fetchRes = (ObjectResult)await _controller.Info(folderId);
        dto = (ProjectInfoDto)((ApiResponse)fetchRes.Value).Body;
        Assert.That(dto.Project.Id.ToString(), Is.EqualTo(folderId));
        Assert.That(dto.Folders.Count, Is.EqualTo(0));
        Assert.That(dto.Resources.Count, Is.EqualTo(1));
        Assert.That(dto.Resources.First().Resource.Id, Is.EqualTo(resourceId2));
        Assert.That(dto.Resources.First().AddedBy, Is.EqualTo(creatorUserName));
    }
    #endregion

    #region Updating projects
    [Test]
    [Description("Tests if the update function fails under specific conditions")]
    public async Task UpdateProjectFail()
    {
        SetControllerUser(_adminUser);
        // 400 status codes
        ObjectResult updateRes = (ObjectResult)await _controller.Update(null, new Dictionary<string, object>()); // resource id invalid
        Assert.That(updateRes.StatusCode, Is.EqualTo(400));

        updateRes = (ObjectResult)await _controller.Update("3e0b6ade-9936-43f6-9890-c7e36a00ad7d", null); // invalid updates
        Assert.That(updateRes.StatusCode, Is.EqualTo(400));

        updateRes = (ObjectResult)await _controller.Update("3e0b6ade-9936-43f6-9890-c7e36a00ad7d", new Dictionary<string, object>()); // updates empty
        Assert.That(updateRes.StatusCode, Is.EqualTo(400));

        Dictionary<string, object> testDict = new();
        testDict.Add("", "");

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        updateRes = (ObjectResult)await _controller.Update(projectId, testDict); // catches invalid key
        Assert.That(updateRes.StatusCode, Is.EqualTo(400));

        testDict.Clear();
        testDict.Add("title", null);

        updateRes = (ObjectResult)await _controller.Update(projectId, testDict); // catches invalid value
        Assert.That(updateRes.StatusCode, Is.EqualTo(400));

        // 404 status codes
        updateRes = (ObjectResult)await _controller.Update("3e0b6ade-9936-43f6-9890-c7e36a00ad7d", testDict); // catches non-existent project first
        Assert.That(updateRes.StatusCode, Is.EqualTo(404));

        // 403 status codes
        // project is created without creators property, thus no one outside of admins has permission to update
        SetControllerUser(_regularUser);
        testDict.Clear();
        testDict.Add("title", "nopermissions?");

        updateRes = (ObjectResult)await _controller.Update(projectId, testDict); // catches permission denied
        Assert.That(updateRes.StatusCode, Is.EqualTo(403));
    }

    [TestCase("title", "testtesttest")]
    [TestCase("title", "new title who dis")]
    [Description("Tests if description updating goes well")]
    public async Task UpdateTitleSuccess(string property, object newValue)
    {
        SetControllerUser(_adminUser);

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Dictionary<string, object> testDict = new()
        {
            { property, newValue }
        };

        ObjectResult updateRes = (ObjectResult)await _controller.Update(projectId, testDict);
        Assert.That(updateRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.Projects.Count, Is.EqualTo(1));

        Project updated = await _projectManager.GetProjectAsync(projectId);
        Assert.That(updated.Title == newValue.ToString());
    }

    [TestCase("description", "testtesttest")]
    [TestCase("description", "new description who dis")]
    [Description("Tests if description updating goes well")]
    public async Task UpdateDescriptionSuccess(string property, object newValue)
    {
        SetControllerUser(_adminUser);

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Dictionary<string, object> testDict = new()
        {
            { property, newValue }
        };

        ObjectResult updateRes = (ObjectResult)await _controller.Update(projectId, testDict);
        Assert.That(updateRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.Projects.Count, Is.EqualTo(1));

        Project updated = await _projectManager.GetProjectAsync(projectId);
        Assert.That(updated.Description == newValue.ToString());
    }

    [Test]
    [Description("Tests if tags updating goes well")]
    public async Task UpdateTagsSuccess()
    {
        SetControllerUser(_adminUser);

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        TagCreateDto dto = new()
        {
            Name = "tag",
            CreatedBy = _adminUserId.ToString(),
            IsApproved = false,
        };
        Guid tagId = await _resourceManager.CreateTagAsync(dto);

        Dictionary<string, object> testDict = new()
        {
            { "tags", JsonDocument.Parse(JsonSerializer.Serialize(new List<string>{tagId.ToString()})).RootElement }
        };

        ObjectResult updateRes = (ObjectResult)await _controller.Update(projectId, testDict);
        Assert.That(updateRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.Projects.Count, Is.EqualTo(1));

        Project updated = await _projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectTagRelations"]);
        Assert.That(updated.ProjectTagRelations.First().TagId == tagId);
    }

    [Test]
    [Description("Tests if creators updating goes well")]
    public async Task UpdateCreatorsSuccess()
    {
        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Dictionary<string, object> testDict = new()
        {
            { "creators", JsonDocument.Parse(JsonSerializer.Serialize(new List<string>{_adminUserId.ToString()})).RootElement }
        };

        // Now we update the creators, which means we add to the existing list. 
        ObjectResult updateRes = (ObjectResult)await _controller.Update(projectId, testDict);
        Assert.That(updateRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.Projects.Count, Is.EqualTo(1));

        Project updated = await _projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectCreatorRelations"]);
        Assert.That(updated.ProjectCreatorRelations.Count, Is.EqualTo(2));
        Assert.That(updated.ProjectCreatorRelations.Select(r => r.CreatorId).Contains(_adminUserId.ToString()));
    }

    [Test]
    [Description("Tests if multiple updates work")]
    public async Task MultiUpdateSuccess()
    {
        TagCreateDto dto = new()
        {
            Name = "tag",
            CreatedBy = _adminUserId.ToString(),
            IsApproved = false,
        };

        Guid tagId = await _resourceManager.CreateTagAsync(dto);

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        Dictionary<string, object> testDict = new()
        {
            { "creators", JsonDocument.Parse(JsonSerializer.Serialize(new List<string>{_adminUserId.ToString()})).RootElement },
            { "tags", JsonDocument.Parse(JsonSerializer.Serialize(new List<string>{tagId.ToString()})).RootElement },
            { "title", "newtitle" },
            { "description", "newdesc" }
        };

        ObjectResult updateRes = (ObjectResult)await _controller.Update(projectId, testDict);
        Assert.That(updateRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.Projects.Count, Is.EqualTo(1));

        Project updated = await _projectManager.GetProjectAsync(projectId, includeProperties: ["ProjectTagRelations", "ProjectCreatorRelations"]);
        Assert.That(updated.ProjectTagRelations.First().TagId == tagId);
        Assert.That(updated.ProjectCreatorRelations.Select(r => r.CreatorId).Contains(_adminUserId.ToString()));
        Assert.That(updated.Title == "newtitle");
        Assert.That(updated.Description == "newdesc");

    }

    #endregion

    #region Adding folders

    [Test]
    [Description("Adding folder results in a 200 status code")]
    public async Task AddFolderOk()
    {
        // We need the custom user again since it'll auto add the current user when creating a folder
        User creatorUser = new User("test", "test", "test@test.nl");
        await _userManager.CreateAsync(creatorUser, "Test123!");
        string userId = Context.Users.First().Id;

        ClaimsPrincipal _testUser = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, "admin")
            ],
            "mock"));

        SetControllerUser(_testUser);

        ObjectResult res = (ObjectResult)await _controller.Create(ValidProject); // Create project
        ApiResponse response = (ApiResponse)res.Value;
        string projectId = response.Body.ToString();

        ObjectResult addRes = (ObjectResult)await _controller.AddFolder("Just a great folder name to put in a test that is about 10 lines", projectId);

        Assert.That(addRes.StatusCode, Is.EqualTo(200));
        Assert.That(Context.ProjectFolderRelations.Count, Is.EqualTo(1));
        Assert.That(Context.ProjectFolderRelations.First().ChildId.ToString() == ((ApiResponse)addRes.Value).Body.ToString());
    }

    [Test]
    [Description("Adding folder fails correctly")]
    public async Task AddFolderFail()
    {
        ObjectResult addRes;

        // 400 status codes
        addRes = (ObjectResult)await _controller.AddFolder(null, "id"); // invalid folder name should be caught
        Assert.That(addRes.StatusCode, Is.EqualTo(400));

        addRes = (ObjectResult)await _controller.AddFolder("id", null); // invalid project id should be caught
        Assert.That(addRes.StatusCode, Is.EqualTo(400));

        // 404 status codes
        addRes = (ObjectResult)await _controller.AddFolder("perfedtly fine fdler name", "bea642fe-2e58-48b1-83cd-8711e8635064"); // project id never added
        Assert.That(addRes.StatusCode, Is.EqualTo(404));
    }

    #endregion
}