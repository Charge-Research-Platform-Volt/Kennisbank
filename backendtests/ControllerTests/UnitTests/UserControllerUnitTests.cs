using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Data;
using Microsoft.AspNetCore.Identity;
using KnowledgeBank.Models;
using KnowledgeBank.Controllers;

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

        // Act
        IActionResult result = await _controller.Delete("123");

        // Assert
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
    }
}