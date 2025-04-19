using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using KnowledgeBank.Controllers;
using KnowledgeBank.Responses;

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

    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        // Enable extension for text-search-vectors
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await context.Database.ExecuteSqlRawAsync(@"ALTER TABLE ""resource-vectors"" ALTER COLUMN vector SET DATA TYPE tsvector USING vector::tsvector;");
        await DatabaseSeeder.SeedTemplate(context);
    }

    [Test]
    public async Task SearchByName_ReturnsResults_WhenDataExists()
    {
        Resource testFile = new()
        {
            Id = new Guid(),
            Title = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
            
        };

        // Add the file to the database.
        await Context.Resources.AddAsync(testFile);
        await Context.SaveResourceChangesAsync();


        var savedFile = await Context.Resources.FirstOrDefaultAsync(f => f.Title == "Integration Test File");
        Assert.That(savedFile, Is.Not.Null, "Test file was not saved in the database");

        // Perform a full-text search on the file name
        var result = await _controller.SearchByTitle("Integration Test", 1, 100);

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
        Assert.That(((Resource)pageResponse.Resources.First()).Title, Is.EqualTo("Integration Test File"));
    }

    [TestCase("2001-11-11T12:00:00Z", "2010-11-11T12:00:00Z", "2012-12-04T12:00:00Z", "2008-12-04T12:00:00Z")]
    [TestCase("2005-11-11T12:00:00Z", "2018-01-05T12:00:00Z", "2020-03-07T12:00:00Z", "2012-04-04T12:00:00Z")]
    [TestCase("1965-03-12T12:00:00Z", "1972-04-15T12:00:00Z", "2021-05-18T12:00:00Z", "1968-02-25T12:00:00Z")]
    [Description("Checks if filtering on end date works correctly. Input are 3 datetime stamps and the output should always be that the approved test file is the only one returned.")]
    public async Task Filter_Date_Test(string startDate, string endDate, string failDate, string passDate)
    {
        DateTime start = DateTime.ParseExact(startDate, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        DateTime end = DateTime.ParseExact(endDate, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        DateTime fail = DateTime.ParseExact(failDate, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
        DateTime pass = DateTime.ParseExact(passDate, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);

        Resource approvedTestFile = new()
        {
            Id = new Guid(),
            Title = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            CreationDate = DateTime.UtcNow,
            PublicationDate = pass,
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "??",
        };

        Resource filteredTestFile = new()
        {
            Id = new Guid(),
            Title = "Extraordinary file",
            Description = "This is a test file that checks if it is created correctly",
            FileType = "text",
            CreationDate = DateTime.UtcNow,
            PublicationDate = fail,
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "??",
        };

        // Filter with endDate
        FilterDto filter = new();
        filter.StartDate = start;
        filter.EndDate = end;

        // Add the file to the database.
        await Context.Resources.AddAsync(approvedTestFile);
        await Context.Resources.AddAsync(filteredTestFile);
        await Context.SaveResourceChangesAsync();

        // Perform a full-text search on the file name
        var result = await _controller.SearchByTitle("Integration Test", 1, 100, filter);

        Assert.That(result, Is.Not.Null, "The search result is null");

        var testResult = result as ObjectResult;

        // Check the result status code
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        // Assert: Check the results returned from the search
        var pageResponse = okResult.Value as PageResponse;
        Assert.That(pageResponse, Is.Not.Null);
        // Should only be one result which is the approved test file
        Assert.That(pageResponse.Resources.Count, Is.EqualTo(1));
        Assert.That((pageResponse.Resources.First() as Resource).Title, Is.EqualTo("Integration Test File"));
    }

    [TestCase("test")]
    [Description("Checks if filtering on end date works correctly. Input are 3 datetime stamps and the output should always be that the approved test file is the only one returned.")]
    public async Task Filter_Tag_Test(string tag)
    {
        Guid tagGuid = Guid.NewGuid();
        Guid docGuid = Guid.NewGuid();


        Tag newTag = new()
        {
            Id = tagGuid,
            Name = tag,
            IsStandardized = true,
            CreatedBy = Guid.NewGuid(),
            CreatedOn = DateTime.UtcNow,
        };
        //Add tag to database
        await Context.Tags.AddAsync(newTag);
        await Context.SaveChangesAsync();

        Resource approvedTestFile = new()
        {
            Id = docGuid,
            Title = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            CreationDate = DateTime.UtcNow,
            PublicationDate = DateTime.UtcNow,
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "??",
        };

        Resource filteredTestFile = new()
        {
            Id = new Guid(),
            Title = "Extraordinary file",
            Description = "This is a test file that checks if it is created correctly",
            FileType = "text",
            CreationDate = DateTime.UtcNow,
            PublicationDate = DateTime.UtcNow,
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "??",
        };

        ResourceTagRelation ftl = new()
        {
            ResourceId = docGuid,
            TagId = tagGuid
        };

        // Filter with endDate
        FilterDto filter = new()
        {
            TagFilters = new string[] {tagGuid.ToString()}
        };


        // Add the file to the database.
        await Context.Resources.AddAsync(approvedTestFile);
        await Context.Resources.AddAsync(filteredTestFile);
        await Context.SaveResourceChangesAsync();

        // Link file and tag
        await Context.ResourceTagRelations.AddAsync(ftl);
        await Context.SaveChangesAsync();

        // Perform a full-text search on the file name
        var result = await _controller.SearchByTitle("Integration Test", 1, 100, filter);

        Assert.That(result, Is.Not.Null, "The search result is null");

        var testResult = result as ObjectResult;

        // Check the result status code
        var okResult = result as OkObjectResult;
        Assert.That(okResult, Is.Not.Null);
        Assert.That(okResult.StatusCode, Is.EqualTo(200));

        // Assert: Check the results returned from the search
        var pageResponse = okResult.Value as PageResponse;
        Assert.That(pageResponse, Is.Not.Null);
        // Should only be one result which is the approved test file
        Assert.That(pageResponse.Resources.Count, Is.EqualTo(1));
        Assert.That((pageResponse.Resources.First() as Resource).Title, Is.EqualTo("Integration Test File"));
    }


    [Test]
    public async Task FullTextSearch_ReturnsResults_WhenDataExists()
    {
        Resource testFile = new()
        {
            Id = new Guid(),
            Title = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
            
        };

        // Add the file to the database.
        await Context.Resources.AddAsync(testFile);
        await Context.SaveResourceChangesAsync();

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
        Assert.That(pageResponse.Resources.Count, Is.EqualTo(1));
        Assert.That(((Resource)pageResponse.Resources.First()).Title, Is.EqualTo("Integration Test File"));
    }

    [Test]
    public async Task FullTextSearch_ReturnsEmptyResult_WhenDataDoesNotExist()
    {
        Resource testFile = new()
        {
            Id = new Guid(),
            Title = "Integration Test File",
            Description = "This is a test file about AI Ohmega",
            FileType = "text",
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "??",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };

        // Add the file to the database.
        await Context.Resources.AddAsync(testFile);
        await Context.SaveResourceChangesAsync();


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
        Assert.That(pageResponse.Resources.Count, Is.EqualTo(0));
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


