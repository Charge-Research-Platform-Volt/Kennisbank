using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Controllers;
using backend.Data;
using backend.Responses;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
namespace backend.Tests;

[TestFixture]
[Category("IntegrationTest")]
public class UserControllerIntegrationTests
{
    private DbContextOptions<DatabaseContext> _options;
    private DatabaseContext _context;
    private UserController _controller;
    private IDbContextTransaction _transaction;
    private Mock<UserManager<User>> _userManagerMock;

    [SetUp]
    public void SetUp()
    {
        // Determine the host based on runtime environment
        string dbHost = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" 
            ? "database"   // To run test in CI/CD
            : "localhost"; // To run test locally
        
        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql($"Host={dbHost};Database=postgres;Username=postgres;Password=postgres")
            .Options;
        
        // mock the UserManager<User> dependency
         _userManagerMock = new Mock<UserManager<User>>(
            Mock.Of<IUserStore<User>>(), 
            null, null, null, null, null, null, null, null
        );

        _context = new DatabaseContext(_options);

        // file_vectors table needs this extension
        _context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        _context.Database.ExecuteSqlRaw("ALTER TABLE file_vectors ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");

        // Start a transaction for rollback after each test
        _transaction = _context.Database.BeginTransaction();

        _context.Database.UseTransaction(_transaction.GetDbTransaction());

        _controller = new UserController(_context, _userManagerMock.Object);
    }

    [TearDown]
    public void TearDown()
    {
        // Rollback the transaction so DB state remains unchanged
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [Test]
    public async Task GetAllUsers_ReturnsResults_WhenDataExists()
    {
        // Arrange
        var testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        _context.AppUsers.Add(testUser);
        await _context.SaveChangesAsync();
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
        _context.AppUsers.Add(testUser);
        await _context.SaveChangesAsync();
        _userManagerMock.Setup(m => m.GetRolesAsync(It.IsAny<User>())).ReturnsAsync((User user) => {
            return user.Email == "admin@admin.nl" ? new List<string> { "admin" } : new List<string> { "user" };
        });

        // Act
        var result = await _controller.GetUsersPaged(1, 10);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        var response = okResult.Value as UserPageResponse;
        Assert.That(response.Users.Length, Is.GreaterThan(0));
        Assert.That(response.PageCount, Is.EqualTo(1));
    }

    [Test]
    public async Task Delete_RemovesUser_WhenSuccessful()
    {
        // Arrange
        var testUser = new User { Id = Guid.NewGuid().ToString(), UserName = "testuser", Email = "test@example.com" };
        _userManagerMock.Setup(m => m.FindByIdAsync(testUser.Id)).ReturnsAsync(testUser);
        _userManagerMock.Setup(m => m.DeleteAsync(testUser)).ReturnsAsync(IdentityResult.Success);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.Delete(testUser.Id);

        // Assert
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));
    }
}
