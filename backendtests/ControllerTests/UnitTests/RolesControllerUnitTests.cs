using backend.Models;
using KnowledgeBank.Controllers;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Newtonsoft.Json;
using System.Security.Claims;

namespace backend.Tests.Unit;

public class RolesControllerUnitTests
{
    private readonly Mock<UserManager<User>> _mockUserManager;
    private readonly Mock<RoleManager<IdentityRole>> _mockRoleManager;
    private RolesController _controller;

    public RolesControllerUnitTests()
    {
        // Setup UserManager mock
        Mock<IUserStore<User>> userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null);

        // Setup RoleManager mock
        Mock<IRoleStore<IdentityRole>> roleStoreMock = new Mock<IRoleStore<IdentityRole>>();
        _mockRoleManager = new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object, null, null, null, null);

        _controller = new RolesController(_mockRoleManager.Object, _mockUserManager.Object);
    }

    [Test]
    public async Task GetCurrentUserRole_UserNotAuthenticated_ReturnsEmptyRole()
    {
        // Arrange
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        object expectedReturnValue = new { role = "", isAuthenticated = false };

        // Act
        IActionResult result = await _controller.GetCurrentUserRole();

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        Assert.That(ObjectComparer.AreObjectsEqual(okResult.Value, expectedReturnValue));

    }

    [Test]
    [TestCase("admin", true)]
    [TestCase("user", true)]
    public async Task GetCurrentUserRole_AuthenticatedWithRole_ReturnsCorrectRole(string expectedRole, bool expectedAuth)
    {
        // Arrange
        User user = new User { Id = "user123" };
        List<Claim> claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "user123")
        };
        ClaimsIdentity identity = new ClaimsIdentity(claims, "TestAuth");
        ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };
        object expectedReturnValue = new { role = expectedRole, isAuthenticated = expectedAuth };


        _mockUserManager.Setup(m => m.FindByIdAsync("user123"))
            .ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { expectedRole });

        // Act
        IActionResult result = await _controller.GetCurrentUserRole();

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        Assert.That(ObjectComparer.AreObjectsEqual(okResult.Value, expectedReturnValue));
    }

    [Test]
    public async Task AssignRole_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        RoleAssignDto dto = new RoleAssignDto { UserId = "user123", RoleName = "admin" };

        _mockUserManager.Setup(m => m.FindByIdAsync(dto.UserId))
            .ReturnsAsync((User)null);

        // Act
        IActionResult result = await _controller.AssignRole(dto);

        // Assert
        NotFoundObjectResult? notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        Assert.That(JsonConvert.SerializeObject(notFoundResult.Value), Is.EqualTo("{\"message\":\"Invalid user ID.\"}"));
    }

    [Test]
    public async Task AssignRole_UserFound_ReturnsOk()
    {
        // Arrange
        RoleAssignDto dto = new RoleAssignDto { UserId = "user123", RoleName = "user" };
        User user = new User { Id = dto.UserId, UserName = "user123" };
        IdentityRole role = new IdentityRole { Name = "admin" };

        _mockUserManager.Setup(m => m.FindByIdAsync(dto.UserId))
            .ReturnsAsync(user);

        _mockUserManager.Setup(m => m.IsInRoleAsync(user, dto.RoleName))
            .ReturnsAsync(false);

        _mockUserManager.Setup(m => m.RemoveFromRolesAsync(It.IsAny<User>(), It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockUserManager.Setup(m => m.AddToRoleAsync(It.IsAny<User>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        _mockRoleManager.Setup(m => m.FindByNameAsync(dto.RoleName))
            .ReturnsAsync(role);

        _mockRoleManager.Setup(m => m.RoleExistsAsync(dto.RoleName))
            .ReturnsAsync(true);

        // Act
        IActionResult result = await _controller.AssignRole(dto);

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        string expectedJson = JsonConvert.SerializeObject(new { message = $"User '{user.UserName}' added to role '{dto.RoleName}' successfully." });
        Assert.That(JsonConvert.SerializeObject(okResult.Value), Is.EqualTo(expectedJson));
    }

    [Test]
    public async Task AssignRole_RoleNotFound_ReturnsNotFound()
    {
        // Arrange
        RoleAssignDto dto = new RoleAssignDto { UserId = "user123", RoleName = "nonExistentRole" };
        User user = new User { Id = dto.UserId, UserName = "user123" }; 

        _mockUserManager.Setup(m => m.FindByIdAsync(dto.UserId))
            .ReturnsAsync(user); 

        _mockRoleManager.Setup(m => m.FindByNameAsync(dto.RoleName))
            .ReturnsAsync((IdentityRole)null); 

        _mockRoleManager.Setup(m => m.RoleExistsAsync(dto.RoleName))
            .ReturnsAsync(false);

        // Act
        IActionResult result = await _controller.AssignRole(dto);

        // Assert
        NotFoundObjectResult? notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        Assert.That(JsonConvert.SerializeObject(notFoundResult.Value), Is.EqualTo("{\"message\":\"Invalid role name.\"}"));
    }

    [Test]
    public async Task RetrieveUserRole_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        string userId = "user123";
        _mockUserManager.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync((User)null);

        // Act
        IActionResult result = await _controller.RetrieveUserRole(userId);

        // Assert
        NotFoundObjectResult? notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        Assert.That(notFoundResult.Value, Is.EqualTo("User not found."));
    }

    [Test]
    public async Task RetrieveUserRole_UserFound_ReturnsOkWithRole()
    {
        // Arrange
        string userId = "user123";
        User user = new User { Id = userId, UserName = "user123" };
        List<string> roles = new List<string> { "admin" }; 

        _mockUserManager.Setup(m => m.FindByIdAsync(userId))
            .ReturnsAsync(user); 

        _mockUserManager.Setup(m => m.GetRolesAsync(user))
            .ReturnsAsync(roles);

        // Act
        IActionResult result = await _controller.RetrieveUserRole(userId);

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        Assert.That(okResult.Value, Is.EqualTo("admin")); 
    }

    [Test]
    public async Task RetrieveUsersInRole_RoleNotFound_ReturnsNotFound()
    {
        // Arrange
        string roleName = "admin";
        _mockRoleManager.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(false);

        // Act
        IActionResult result = await _controller.RetrieveUsersInRole(roleName);

        // Assert
        NotFoundObjectResult? notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));
        Assert.That(notFoundResult.Value, Is.EqualTo("Role does not exist."));
    }

    [Test]
    public async Task RetrieveUsersInRole_RetrievesUsersInRole_ReturnsOkResult()
    {
        // Arrange
        string roleName = "admin";
        List<User> users = new List<User>
        {
            new User { Id = "user123", UserName = "user123" },
            new User { Id = "user124", UserName = "user124" }
        };

        _mockRoleManager.Setup(r => r.RoleExistsAsync(roleName))
            .ReturnsAsync(true);
        _mockUserManager.Setup(m => m.GetUsersInRoleAsync(roleName))
            .ReturnsAsync(users);

        // Act
        IActionResult result = await _controller.RetrieveUsersInRole(roleName);

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
        Assert.That(okResult.Value, Is.EqualTo(users));
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


