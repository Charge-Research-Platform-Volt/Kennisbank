using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Moq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using KnowledgeBank.Controllers;
using KnowledgeBank.Responses;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class WebsiteUploadControllerTests : TestBase
{
    private ResourceManager _resourceManager;
    private WebsiteUploadController _controller;


    [SetUp]
    public void SetupController()
    {
        _resourceManager = new ResourceManager(Context);
        _controller = new WebsiteUploadController(_resourceManager);
    }

    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        // Enable extension for text-search-vectors
        await DatabaseSeeder.SeedTemplate(context);
    }

    [TestCase("test", false)]
    [TestCase("www.test.nl", true)]
    [TestCase("https://www.bol.com", true)]
    public async Task Upload_Website_Test(string input, bool output)
    {
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        WebsiteCreateDto test = new()
        {
            Title = "Test website",
            Description = "amazing test website",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "AA",
            PublicationDate = DateTime.UtcNow,
            PublicationCode = "test",
            License = "test",
            Note = "test",
            Url = input,
        };

        // Add the website
        IActionResult uploadWebsite = await _controller.AddWebsite(test);

        if(output)
        {
            // Check that website is uploaded correctly
            OkObjectResult? uploadResult = uploadWebsite as OkObjectResult;
            Assert.That(uploadResult.StatusCode, Is.EqualTo(200));

            // Get ID of uploaded website
            object? uploadValue = uploadResult.Value;
            object? uploadID = uploadValue.GetType().GetProperties().First(prop => prop.Name == "id").GetValue(uploadValue , null);

            // Check if website is in resource table
            OkObjectResult? getResult = await _controller.Get_Website(uploadID.ToString()) as OkObjectResult;
            Assert.That(Context.Resources.Count, Is.EqualTo(1));
            Assert.That(getResult.StatusCode, Is.EqualTo(200));

            // Check if website url is in website metadata table
            Assert.That(Context.WebsiteMetadata.Count, Is.EqualTo(1));
            Assert.That(Context.WebsiteMetadata.First(prop => prop.ResourceId == Guid.Parse(uploadID.ToString())).Url, Is.EqualTo(input));

        }
        else
        {
            // Check that website has failed uploading due to invalid url
            BadRequestObjectResult? badResult = uploadWebsite as BadRequestObjectResult;
            Assert.That(badResult.StatusCode, Is.EqualTo(400));
        }
    }

    [TestCase("www.tester.nl")]
    public async Task Delete_Website_Test(string input)
    {
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        WebsiteCreateDto test = new()
        {
            Title = "Test website",
            Description = "amazing test website",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "AA",
            PublicationDate = DateTime.UtcNow,
            PublicationCode = "test",
            License = "test",
            Note = "test",
            Url = input,
        };

        // Add the website
        IActionResult uploadWebsite = await _controller.AddWebsite(test);
        OkObjectResult? uploadResult = uploadWebsite as OkObjectResult;

        // Get ID of uploaded website
        object? uploadValue = uploadResult.Value;
        object? uploadID = uploadValue.GetType().GetProperties().First(prop => prop.Name == "id").GetValue(uploadValue , null);

        // Delete website
        await _controller.DeleteWebsite(uploadID.ToString());

        // Check that both resources and website metadata are cleared
        Assert.That(Context.WebsiteMetadata.Count, Is.EqualTo(0));
        Assert.That(Context.Resources.Count, Is.EqualTo(0));
    }

    [TestCase("www.test.nl", "www.woah.nl", WebsiteColumn.Url)]
    //[TestCase("www.newtest.nl", "Better Test Website", "Title")]
    public async Task Change_Website_Test(string website, string newAttribute, WebsiteColumn attribute)
    {
        ResourceType resourceType = await Context.ResourceTypes.FirstAsync();

        WebsiteCreateDto test = new()
        {
            Title = "Test website",
            Description = "amazing test website",
            TypeId = resourceType.Id.ToString(),
            LanguageCode = "AA",
            PublicationDate = DateTime.UtcNow,
            PublicationCode = "test",
            License = "test",
            Note = "test",
            Url = website,
        };

        // Add the website
        IActionResult uploadWebsite = await _controller.AddWebsite(test);
        OkObjectResult? uploadResult = uploadWebsite as OkObjectResult;

        // Get ID of uploaded website
        object? uploadValue = uploadResult.Value;
        object? uploadID = uploadValue.GetType().GetProperties().First(prop => prop.Name == "id").GetValue(uploadValue , null);

        // Change website
        await _controller.ChangeWebsite(uploadID.ToString(), newAttribute, attribute);
        await Context.SaveChangesAsync();

        // Check that if URL is changed, the metadata table is changed, and if the title is changed the resources table is changed
        if(attribute == WebsiteColumn.Url)
        {
            Assert.That(Context.WebsiteMetadata.Where(prop => prop.Url == website).Count, Is.EqualTo(0));
            Assert.That(Context.WebsiteMetadata.Where(prop => prop.Url == newAttribute), Is.Not.Null);
        }
        
        else
        {
            Assert.That(Context.Resources.Where(prop => prop.Title == test.Title).Count, Is.EqualTo(0));
            Assert.That(Context.Resources.First(prop => prop.Title == newAttribute), Is.Not.Null);
        }

    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


