using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Data;
using Microsoft.AspNetCore.Identity;
using KnowledgeBank.Models;
using KnowledgeBank.Controllers;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using Microsoft.Extensions.Options;
using backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using KnowledgeBank;
using KnowledgeBank.Responses;
using System.Reflection.Metadata;

namespace backend.Tests.Unit;

[TestFixture]
[Category("UnitTest")]
public class UserControllerUnitTests
{
    private Mock<DatabaseContext> _mockDbContext;
    private Mock<UserManager<User>> _mockUserManager;
    private Mock<IOptions<OwnerUserConfig>> ownerConfigOptionsMock;

    private Mock<IAzureBlobService> _blobMock;
    private UserController _controller;

    [SetUp]
    public void SetUp()
    {
        // Mock UserManager<User>
        Mock<IUserStore<User>> userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null!, null!, null!, null!, null!, null!, null!, null!
        );

        // Mock the OwnerUserConfig options
        var ownerConfig = new OwnerUserConfig
        {
            Email = "owner@test.com",
            Password = "TestPassword123!",
            FirstName = "Test",
            LastName = "Owner"
        };

        ownerConfigOptionsMock = new Mock<IOptions<OwnerUserConfig>>();
        ownerConfigOptionsMock.Setup(x => x.Value).Returns(ownerConfig);

        _blobMock = new Mock<IAzureBlobService>();
        _mockDbContext = new Mock<DatabaseContext>(new DbContextOptions<DatabaseContext>());
        _controller = new UserController(_mockDbContext.Object, _blobMock.Object, _mockUserManager.Object, ownerConfigOptionsMock.Object);
    }

    private void SetUserContext(bool isAuthenticated, string? userId = null)
    {
        var claims = new List<Claim>();
        if (userId != null)
            claims.Add(new Claim(ClaimTypes.NameIdentifier, userId));

        var identity = new ClaimsIdentity(claims, isAuthenticated ? "TestAuth" : null);
        var user = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Test]
    public async Task UpdateMail_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        UpdateEmailDto dto = new UpdateEmailDto { UserId = "123", Email = "newemail@example.com" };
        _mockUserManager.Setup(m => m.FindByIdAsync(dto.UserId)).ReturnsAsync((User)null!);

        // Act
        IActionResult result = await _controller.UpdateMail(dto);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Delete_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _mockUserManager.Setup(m => m.FindByIdAsync("123")).ReturnsAsync((User)null!);
        SetUserContext(true, "345");

        // Act
        IActionResult result = await _controller.Delete("123");

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }


    #region GetCurrentAccount

    // I am not going to test unauthorized because it should be handled by ASP NET

    // Ideally an impossible scenario
    [Test]
    public async Task GetCurrentAccount_Returns500_WhenAuthorizedButNoUser()
    {
        // Arrange
        SetUserContext(true);

        // Act
        var result = await _controller.GetCurrentAccount();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(500));
    }

    // Ideally an impossible scenario
    [Test]
    public async Task GetCurrentAccount_Returns500_WhenAuthorizedButUserNotFound()
    {
        // Arrange
        SetUserContext(true, "John Dough");

        // Act
        var result = await _controller.GetCurrentAccount();

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(500));
    }

    [Test]
    public async Task GetCurrentAccount_ReturnsUser_WhenUserExists()
    {
        // Arrange
        var user = new User("John", "Doe", "john@example.com") { EmailConfirmed = true };
        SetUserContext(true, "user123");
        _mockUserManager.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync(user);
        _mockUserManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "User" });

        // Act
        var result = await _controller.GetCurrentAccount();

        // Assert
        UserResponse response = (UserResponse)result.Value!;
        Assert.That(response.FirstName, Is.EqualTo("John"));
        Assert.That(response.LastName, Is.EqualTo("Doe"));
        Assert.That(response.Id, Is.EqualTo(Guid.Parse(user.Id)));
        Assert.That(response.Email, Is.EqualTo("john@example.com"));
        Assert.That(response.EmailConfirmed, Is.EqualTo(true));
        Assert.That(response.CustomAvatarVersion, Is.EqualTo(null));
        Assert.That(response.Role, Is.EqualTo("User"));        
    }

    #endregion

    #region GetCurrentAvatar

    // I am not going to test unauthorized because it should be handled by ASP NET

    // Ideally an impossible scenario

    [Test]
    public async Task GetCurrentAVatar_Returns404_WhenAvatarDoesNotExist()
    {
        // Arrange
        var userId = "user123";
        var user = new User("John", "Doe", "john@example.com") { EmailConfirmed = true };
        SetUserContext(true, "user123");
        _mockUserManager.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync(user);
        _blobMock.Setup(m => m.RetreiveUserAvatarStream(user.Id)).ReturnsAsync((BlobAvatarResponse?)null);


        // Act
        var result = (NotFoundObjectResult)await _controller.GetCurrentAvatar(userId);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(404));
        Assert.That(ObjectComparer.AreObjectsEqual(result.Value, new { message = "No avatar found for user." }));
    }

    [Test]
    public async Task GetUserAvatar_Returns500_WhenBlobServiceThrowsException()
    {
        // Arrange
        var userId = "user123";
        _blobMock.Setup(m => m.RetreiveUserAvatarStream(userId)).ThrowsAsync(new InvalidOperationException("Simulated blob service error"));

        // Act
        var result = (ObjectResult)await _controller.GetCurrentAvatar(userId);

        // Assert
        Assert.That(result.StatusCode, Is.EqualTo(500));
        Assert.That(ObjectComparer.AreObjectsEqual(result.Value, new { message = "Internal server error." }));
    }

    [Test]
    public async Task GetCurrentAVatar_ReturnsAvatar_WhenUserExists()
    {
        // Arrange
        var userId = "user123";
        var user = new User("John", "Doe", "john@example.com") { EmailConfirmed = true };
        SetUserContext(true, "user123");
        _mockUserManager.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync(user);
        var _mockStream = new Mock<Stream>();
        _blobMock.Setup(m => m.RetreiveUserAvatarStream("user123")).ReturnsAsync(new BlobAvatarResponse(_mockStream.Object, "image/png"));


        // Act
        var result = (FileStreamResult)await _controller.GetCurrentAvatar(userId);

        // Assert
        Assert.That(result.FileStream, Is.EqualTo(_mockStream.Object));
        Assert.That(result.ContentType, Is.EqualTo("image/png"));
    }

    #endregion
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


