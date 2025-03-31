using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Storage;
using KnowledgeBank.Controllers;

namespace backend.Tests;

[TestFixture]
[Category("IntegrationTest")]
public class TagControllerIntegrationTests
{
    private DbContextOptions<DatabaseContext> _options;
    private DatabaseContext _context;
    private TagController _controller;
    private IDbContextTransaction _transaction;

    [SetUp]
    public void Setup()
    {
        // Determine the host based on runtime environment
        string dbHost = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
            ? "database"   // To run test in CI/CD
            : "localhost"; // To run test locally

        _options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql($"Host={dbHost};Database=postgres;Username=postgres;Password=postgres")
            .Options;

        _context = new DatabaseContext(_options);

        _transaction = _context.Database.BeginTransaction();

        _context.Database.UseTransaction(_transaction.GetDbTransaction());

        _controller = new TagController(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _transaction.Rollback();
        _transaction.Dispose();
        _context.Dispose();
    }

    [TestCase("test")]
    [TestCase("bla")]
    [Description("Simple test for adding tags")]
    public async Task AddTagTest(string input)
    {
        // Clear all Tags so we can assert on Count later
        _context.Tags.RemoveRange(_context.Tags);
        await _context.SaveChangesAsync();
        
        // Add tag
        OkObjectResult addResponse = (await _controller.AddTag(input)) as OkObjectResult;

        // Check that statuscode is correct, count of tags is 1 and the name is correct
        Assert.That(addResponse.StatusCode, Is.EqualTo(200));
        Assert.That(_context.Tags.Count(), Is.EqualTo(1));
        Assert.That(_context.Tags.First().Name, Is.EqualTo(input));

        // Check that adding multiple tags works
        await _controller.AddTag("test123");
        Assert.That(_context.Tags.Count(), Is.EqualTo(2));
    }

    [TestCase("test")]
    [Description("Simple test for deleting tags")]
    public async Task DeleteTagTest(string input)
    {
        // Clear all Tags so we can assert on Count later
        _context.Tags.RemoveRange(_context.Tags);
        await _context.SaveChangesAsync();

        // add tag as before
        await _controller.AddTag(input);
        string addedTagGUID = _context.Tags.First().Id.ToString();

        // Now we delete and test if the database is empty again
        OkObjectResult delResponse = (await _controller.DeleteTag(addedTagGUID)) as OkObjectResult;

        Assert.That(delResponse.StatusCode, Is.EqualTo(200));
        Assert.That(_context.Tags.Count(), Is.EqualTo(0));

    }

    [TestCase("cd34f056-c81a-4906-9f38-315233e83126")]
    [Description("Tests if deleting a tag fails if the tag is not in the database")]
    public async Task FailDeleteTagTest(string input)
    {
        // Try to delete tag in an empty database => should fail
        NotFoundObjectResult failedDelResponse = (await _controller.DeleteTag(input)) as NotFoundObjectResult;

        // Assert that error code is 404 (tag not found)
        Assert.That(failedDelResponse.StatusCode, Is.EqualTo(404));
    }

    [TestCase("original", "new")]
    [TestCase("1", "2")]
    [Description("Tests if you can successfully change the name of a tag")]
    public async Task ChangeTagTest(string orgName, string newName)
    {
        // Clear all Tags so we can assert on Count later
        _context.Tags.RemoveRange(_context.Tags);
        await _context.SaveChangesAsync();
        
        // Add tag as before and get the GUID
        await _controller.AddTag(orgName);
        string addedTagGUID = _context.Tags.First().Id.ToString();

        // Change tag name to new name and check if the database contains 1 element
        OkObjectResult changeResponse = (await _controller.ChangeTagName(addedTagGUID, newName)) as OkObjectResult;
        Assert.That(changeResponse.StatusCode, Is.EqualTo(200));
        Assert.That(_context.Tags.Count(), Is.EqualTo(1));

        // Fetch changed tag and double check if the tag is correctly changed
        OkObjectResult allTags = _controller.Get() as OkObjectResult;
        List<Tag> tagList = allTags.Value as List<Tag>;

        Assert.That(tagList.Count, Is.EqualTo(1));
        Assert.That(tagList[0].Name, Is.EqualTo(newName));
    }
}
