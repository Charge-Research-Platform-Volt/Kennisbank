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

namespace backend.Tests.Unit;

[TestFixture]
[Category("UnitTest")]
public class UserControllerUnitTests
{
    private Mock<DatabaseContext> _mockDbContext;
    private Mock<UserManager<User>> _mockUserManager;
    private UserController _controller;

    [SetUp]
    public void SetUp()
    {
        // Mock UserManager<User>
        Mock<IUserStore<User>> userStoreMock = new Mock<IUserStore<User>>();
        _mockUserManager = new Mock<UserManager<User>>(
            userStoreMock.Object, null, null, null, null, null, null, null, null
        );

        _mockDbContext = new Mock<DatabaseContext>(new DbContextOptions<DatabaseContext>());
        _controller = new UserController(_mockDbContext.Object, _mockUserManager.Object);
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
        _mockUserManager.Setup(m => m.FindByIdAsync(dto.UserId)).ReturnsAsync((User)null);

        // Act
        IActionResult result = await _controller.UpdateMail(dto);

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task Delete_ReturnsNotFound_WhenUserDoesNotExist()
    {
        // Arrange
        _mockUserManager.Setup(m => m.FindByIdAsync("123")).ReturnsAsync((User)null);
        SetUserContext(true, "345");
        
        // Act
        IActionResult result = await _controller.Delete("123");

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public async Task GetCurrentUserName_ReturnsEmpty_WhenUnauthenticated()
    {
        // Arrange
        SetUserContext(false);

        // Act
        var result = await _controller.GetCurrentUserName() as OkObjectResult;

        // Assert
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json).RootElement;
        Assert.That(doc.GetProperty("name").GetString(), Is.EqualTo(""));
        Assert.That(doc.GetProperty("isAuthenticated").GetBoolean(), Is.False);
    }

    [Test]
    public async Task GetCurrentUserName_ReturnsEmpty_WhenNoUserIdClaim()
    {
        // Arrange
        SetUserContext(true); // Authenticated, but no NameIdentifier

        // Act
        var result = await _controller.GetCurrentUserName() as OkObjectResult;

        // Assert
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json).RootElement;
        Assert.That(doc.GetProperty("name").GetString(), Is.EqualTo(""));
        Assert.That(doc.GetProperty("isAuthenticated").GetBoolean(), Is.True);
    }

    [Test]
    public async Task GetCurrentUserName_ReturnsEmpty_WhenUserNotFound()
    {
        // Arrange
        SetUserContext(true, "user123");
        _mockUserManager.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync((User)null);

        // Act
        var result = await _controller.GetCurrentUserName() as OkObjectResult;

        // Assert
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json).RootElement;
        Assert.That(doc.GetProperty("name").GetString(), Is.EqualTo(""));
        Assert.That(doc.GetProperty("isAuthenticated").GetBoolean(), Is.True);
    }

    [Test]
    public async Task GetCurrentUserName_ReturnsFullName_WhenUserExists()
    {
        // Arrange
        var user = new User { FirstName = "John", LastName = "Doe" };
        SetUserContext(true, "user123");
        _mockUserManager.Setup(m => m.FindByIdAsync("user123")).ReturnsAsync(user);

        // Act
        var result = await _controller.GetCurrentUserName() as OkObjectResult;

        // Assert
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json).RootElement;
        Assert.That(doc.GetProperty("name").GetString(), Is.EqualTo("John Doe"));
        Assert.That(doc.GetProperty("isAuthenticated").GetBoolean(), Is.True);
    }

    [Test]
    public async Task GetCurrentUserFirstName_ReturnsFirstName_WhenUserExists()
    {
        // Arrange
        var user = new User { FirstName = "Jane", LastName = "Doe" };
        SetUserContext(true, "user456");
        _mockUserManager.Setup(m => m.FindByIdAsync("user456")).ReturnsAsync(user);

        // Act
        var result = await _controller.GetCurrentUserFirstName() as OkObjectResult;

        // Assert
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json).RootElement;
        Assert.That(doc.GetProperty("firstName").GetString(), Is.EqualTo("Jane"));
        Assert.That(doc.GetProperty("isAuthenticated").GetBoolean(), Is.True);
    }

    [Test]
    public async Task GetCurrentUserLastName_ReturnsLastName_WhenUserExists()
    {
        // Arrange
        var user = new User { FirstName = "Max", LastName = "Verstappen" };
        SetUserContext(true, "f1champ");
        _mockUserManager.Setup(m => m.FindByIdAsync("f1champ")).ReturnsAsync(user);

        // Act
        var result = await _controller.GetCurrentUserLastName() as OkObjectResult;

        // Assert
        var json = JsonSerializer.Serialize(result.Value);
        var doc = JsonDocument.Parse(json).RootElement;
        Assert.That(doc.GetProperty("lastName").GetString(), Is.EqualTo("Verstappen"));
        Assert.That(doc.GetProperty("isAuthenticated").GetBoolean(), Is.True);
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


