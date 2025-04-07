using backend.Controllers;
using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using backend.Data;
using backend.Responses;
using Microsoft.EntityFrameworkCore.Storage;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class SearchControllerTests : TestBase
{
    private SearchController _controller;
    private Mock<IAzureBlobService> _mockBlobService;


    [SetUp]
    public void SetupController()
    {
        _mockBlobService = new Mock<IAzureBlobService>();
        _controller = new SearchController(_mockBlobService.Object, Context);
    }

    protected override async Task SeedTemplateDatabase(DatabaseContext context)
    {
        // Enable extension for text-search-vectors
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await context.Database.ExecuteSqlRawAsync("ALTER TABLE file_vectors ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
            
        };

        // Add the file to the database.
        await Context.Files.AddAsync(testFile);
        await Context.SaveFileChangesAsync();


        var savedFile = await Context.Files.FirstOrDefaultAsync(f => f.Name == "Integration Test File");
        Assert.That(savedFile, Is.Not.Null, "Test file was not saved in the database");

        // Perform a full-text search on the file name
        var result = await _controller.SearchByName("Integration Test", 1, 100);

        Assert.That(result, Is.Not.Null, "The search result is null");

        var testResult = result as ObjectResult;

        // Check the result status code
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        // Assert: Check the results returned from the search
        var pageResponse = okResult.Value as PageResponse;
        Assert.That(pageResponse, Is.Not.Null);
        // Should only be one result
        Assert.That(pageResponse.Files.First().Name, Is.EqualTo("Integration Test File"));
    }

    [Test]
    public async Task FullTextSearch_ReturnsResults_WhenDataExists()
    {
        FileItem testFile = new()
        {
            Id = new Guid(),
            Name = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
            
        };

        // Add the file to the database.
        await Context.Files.AddAsync(testFile);
        await Context.SaveFileChangesAsync();

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
    public async Task FullTextSearch_ReturnsEmptyResult_WhenDataDoesNotExist()
    {
        FileItem testFile = new()
        {
            Id = new Guid(),
            Name = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Add the file to the database.
        await Context.Files.AddAsync(testFile);
        await Context.SaveFileChangesAsync();


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
