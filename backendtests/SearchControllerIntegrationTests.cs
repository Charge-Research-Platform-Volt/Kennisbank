using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using backend.Controllers;
using backend.Data;
using backend.Responses;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore.Storage;
namespace backend.Tests;

[TestFixture]
[Category("IntegrationTest")]
public class SearchControllerIntegrationTests
{
    private DbContextOptions<DatabaseContext> _options;
    private DatabaseContext _context;
    private SearchController _controller;
    private Mock<IAzureBlobService> _mockBlobService;
    private IDbContextTransaction _transaction;


    [SetUp]
    public void SetUp()
    {
        _mockBlobService = new Mock<IAzureBlobService>();

        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql("Host=localhost;Database=postgres;Username=postgres;Password=postgres")
            .Options;

        _context = new DatabaseContext(_options);

        _context.Database.ExecuteSqlRaw("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

        // Start a transaction for rollback after each test
        _transaction = _context.Database.BeginTransaction();

        _context.Database.UseTransaction(_transaction.GetDbTransaction());

        _controller = new SearchController(_mockBlobService.Object, _context);
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
    public async Task SearchByName_ReturnsResults_WhenDataExists()
    {
        FileItem testFile = new()
        {
            Id = new Guid(),
            Name = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
            
        };

        // Add the file to the database.
        await _context.Files.AddAsync(testFile);
        await _context.SaveFileChangesAsync();


        // Perform a full-text search on the file name
        var result = await _controller.FullTextSearch("Ohmega", 1, 10);

        // Check the result status code
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        // Assert: Check the results returned from the search
        var pageResponse = okResult.Value as PageResponse;
        Assert.That(pageResponse, Is.Not.Null);
        // Should only be one result
        Assert.That(pageResponse.Files.Count, Is.EqualTo(1));
        Assert.That(pageResponse.Files.First().Name, Is.EqualTo("Integration Test File"));
    }

    [Test]
    public async Task SearchByName_ReturnsEmptyResult_WhenDataDoesNotExist()
    {
        FileItem testFile = new()
        {
            Id = new Guid(),
            Name = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };

        // Add the file to the database.
        await _context.Files.AddAsync(testFile);
        await _context.SaveFileChangesAsync();


        // Perform a full-text search on the file name with a query that 
        var result = await _controller.FullTextSearch("eajfkdjlejifa_ThisQueryShouldFail", 1, 10);

        // Check the result status code
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        // Assert: Check the results returned from the search
        var pageResponse = okResult.Value as PageResponse;
        Assert.That(pageResponse, Is.Not.Null);
        // Should be zero results
        Assert.That(pageResponse.Files.Count, Is.EqualTo(0));
    }
}
