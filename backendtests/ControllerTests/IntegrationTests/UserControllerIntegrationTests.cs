using Moq;
using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using backend.Tests.Infrastructure;
using KnowledgeBank.Controllers;
using KnowledgeBank.Responses;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class UserControllerTests : TestBase
{
    private UserController _controller;
    private Mock<UserManager<User>> _userManagerMock;

    [SetUp]
    public void SetupController()
    {

        // mock the UserManager<User> dependency
        _userManagerMock = new Mock<UserManager<User>>(
           Mock.Of<IUserStore<User>>(),
           null, null, null, null, null, null, null, null
       );

        _controller = new UserController(Context, _userManagerMock.Object);
    }

    private void SetUserIdentity(UserController controller, string userId)
    {
        var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userId) };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var principal = new ClaimsPrincipal(identity);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }


    [Test]
    public async Task GetAllUsers_ReturnsResults_WhenDataExists()
    {
        // Arrange
        User testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        Context.AppUsers.Add(testUser);
        await Context.SaveChangesAsync();
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync((User user) =>
        {
            return user.Email == "admin@admin.nl" ? new List<string> { "admin" } : new List<string> { "user" };
        });

        // Act
        IActionResult result = await _controller.GetAllUsers();

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        UserResponse[]? response = okResult.Value as UserResponse[];
        Assert.That(response.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetUsersPaged_ReturnsResults_WhenDataExists()
    {
        // Arrange
        User testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        Context.AppUsers.Add(testUser);
        await Context.SaveChangesAsync();
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync((User user) =>
        {
            return user.Email == "admin@admin.nl" ? new List<string> { "admin" } : new List<string> { "user" };
        });

        // Act
        IActionResult result = await _controller.GetUsersPaged(1, 10);

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        if (okResult.Value is UserPageResponse response)
        {
            Assert.That(response.Users.Length, Is.GreaterThan(0));
            Assert.That(response.PageCount, Is.EqualTo(1));
            return;
        }

        Assert.Fail("Expected UserPageResponse, but got a different type.");
    }

    [Test]
    public async Task GetUsersPagedWithSearch_ReturnsResults_WhenDataExists()
    {
        // Arrange
        User testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        User otherUser = new User { Id = Guid.NewGuid().ToString(), UserName = "otheruser", Email = "otheruser@example.com" };
        Context.AppUsers.Add(testUser);
        Context.AppUsers.Add(otherUser);
        await Context.SaveChangesAsync();
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync((User user) =>
        {
            return user.Email == "admin@admin.nl" ? new List<string> { "admin" } : new List<string> { "user" };
        });

        // Act
        IActionResult result = await _controller.GetUsersPaged(1, 10, "test");

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        if (okResult.Value is UserPageResponse response)
        {
            Assert.That(response.Users.Length, Is.GreaterThan(0));
            Assert.That(response.PageCount, Is.EqualTo(1));
            return;
        }

        Assert.Fail("Expected UserPageResponse, but got a different type.");
    }

    [Test]
    public async Task Delete_RemovesUser_WhenSuccessful()
    {
        // Arrange
        User testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(testUser.Id)).ReturnsAsync(testUser);
        _userManagerMock.Setup(m => m.DeleteAsync(testUser)).ReturnsAsync(IdentityResult.Success);
        await Context.SaveChangesAsync();

        // Act
        IActionResult result = await _controller.Delete(testUser.Id);

        // Assert
        OkObjectResult? okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
    }

    [Test]
    public async Task Update_ReturnsSuccess_WhenUpdateIsValid()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { Id = userId, FirstName = "Old", LastName = "Name", Email = "old@example.com", UserName = "old@example.com" };
        Context.AppUsers.Add(user);
        await Context.SaveChangesAsync();

        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GenerateChangeEmailTokenAsync(user, "new@example.com")).ReturnsAsync("token");
        _userManagerMock.Setup(m => m.ChangeEmailAsync(user, "new@example.com", "token")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.SetUserNameAsync(user, "new@example.com")).ReturnsAsync(IdentityResult.Success);

        var controller = new UserController(Context, _userManagerMock.Object);
        SetUserIdentity(controller, userId);

        var dto = new UpdateUserDto { FirstName = "New", LastName = "Name", Email = "new@example.com" };

        // Act
        IActionResult result = await controller.Update(dto);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        var response = okResult?.Value as ApiResponse;
        Assert.That(response?.Success, Is.True);
        Assert.That(response?.Message, Is.EqualTo("User updated successfully."));
    }

    [Test]
    public async Task Update_ReturnsFailure_WhenUpdateAsyncFails()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { Id = userId, Email = "old@example.com" };
        Context.AppUsers.Add(user);
        await Context.SaveChangesAsync();

        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Failed());

        var controller = new UserController(Context, _userManagerMock.Object);
        SetUserIdentity(controller, userId);

        var dto = new UpdateUserDto { FirstName = "New", LastName = "Name", Email = "new@example.com" };

        // Act
        var result = await controller.Update(dto);

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult?.Value as ApiResponse;
        Assert.That(response?.Success, Is.False);
        Assert.That(response?.Message, Does.Contain("Failed to update"));
    }

    [Test]
    public async Task Update_ReturnsFailure_WhenChangeEmailFails()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { Id = userId, Email = "old@example.com" };
        Context.AppUsers.Add(user);
        await Context.SaveChangesAsync();

        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GenerateChangeEmailTokenAsync(user, "new@example.com")).ReturnsAsync("token");
        _userManagerMock.Setup(m => m.ChangeEmailAsync(user, "new@example.com", "token")).ReturnsAsync(IdentityResult.Failed());

        var controller = new UserController(Context, _userManagerMock.Object);
        SetUserIdentity(controller, userId);

        var dto = new UpdateUserDto { FirstName = "New", LastName = "Name", Email = "new@example.com" };

        // Act
        var result = await controller.Update(dto);

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult?.Value as ApiResponse;
        Assert.That(response?.Success, Is.False);
        Assert.That(response?.Message, Does.Contain("email already exists"));
    }

    [Test]
    public async Task Update_ReturnsFailure_WhenSetUserNameFails()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();
        var user = new User { Id = userId, Email = "old@example.com" };
        Context.AppUsers.Add(user);
        await Context.SaveChangesAsync();

        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(m => m.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.GenerateChangeEmailTokenAsync(user, "new@example.com")).ReturnsAsync("token");
        _userManagerMock.Setup(m => m.ChangeEmailAsync(user, "new@example.com", "token")).ReturnsAsync(IdentityResult.Success);
        _userManagerMock.Setup(m => m.SetUserNameAsync(user, "new@example.com")).ReturnsAsync(IdentityResult.Failed());

        var controller = new UserController(Context, _userManagerMock.Object);
        SetUserIdentity(controller, userId);

        var dto = new UpdateUserDto { FirstName = "New", LastName = "Name", Email = "new@example.com" };

        // Act
        var result = await controller.Update(dto);

        // Assert
        var okResult = result as OkObjectResult;
        var response = okResult?.Value as ApiResponse;
        Assert.That(response?.Success, Is.False);
        Assert.That(response?.Message, Does.Contain("email already exists"));
    }

    [Test]
    public async Task Update_ReturnsNotFound_WhenUserNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ReturnsAsync((User)null!);

        var controller = new UserController(Context, _userManagerMock.Object);
        SetUserIdentity(controller, userId);

        var dto = new UpdateUserDto { FirstName = "New", LastName = "Name", Email = "new@example.com" };

        // Act
        var result = await controller.Update(dto);

        // Assert
        var objectResult = result as NotFoundObjectResult;
        Assert.That(objectResult?.StatusCode, Is.EqualTo(404));
        Assert.That(objectResult?.Value, Is.EqualTo("User not found."));
    }

    [Test]
    public async Task Update_Returns500_WhenUnexpectedErrorOccurs()
    {
        // Arrange
        var userId = Guid.NewGuid().ToString();

        _userManagerMock.Setup(m => m.FindByIdAsync(userId)).ThrowsAsync(new Exception("Database failure"));

        var controller = new UserController(Context, _userManagerMock.Object);
        SetUserIdentity(controller, userId);

        var dto = new UpdateUserDto { FirstName = "New", LastName = "Name", Email = "new@example.com" };

        // Act
        var result = await controller.Update(dto);

        // Assert
        var objectResult = result as ObjectResult;
        Assert.That(objectResult?.StatusCode, Is.EqualTo(500));
        Assert.That(objectResult?.Value, Is.EqualTo("Internal server error."));
    }

}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


