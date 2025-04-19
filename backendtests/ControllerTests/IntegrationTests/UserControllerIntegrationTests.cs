using Moq;
using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using backend.Tests.Infrastructure;
using KnowledgeBank.Controllers;
using KnowledgeBank.Responses;

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

    [Test]
    public async Task GetAllUsers_ReturnsResults_WhenDataExists()
    {
        // Arrange
        var testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        Context.AppUsers.Add(testUser);
        await Context.SaveChangesAsync();
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync((User user) => {
            return user.Email == "admin@admin.nl" ? new List<string> { "admin" } : new List<string> { "user" };
        });

        // Act
        var result = await _controller.GetAllUsers();

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var response = okResult.Value as UserResponse[];
        Assert.That(response.Length, Is.GreaterThan(0));
    }

    [Test]
    public async Task GetUsersPaged_ReturnsResults_WhenDataExists()
    {
        // Arrange
        var testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        Context.AppUsers.Add(testUser);
        await Context.SaveChangesAsync();
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync((User user) => {
            return user.Email == "admin@admin.nl" ? new List<string> { "admin" } : new List<string> { "user" };
        });

        // Act
        var result = await _controller.GetUsersPaged(1, 10);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        if(okResult.Value is UserPageResponse response)
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
        var testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(testUser.Id)).ReturnsAsync(testUser);
        _userManagerMock.Setup(m => m.DeleteAsync(testUser)).ReturnsAsync(IdentityResult.Success);
        await Context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(testUser.Id);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


