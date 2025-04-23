using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Data;
using System.Net;
using KnowledgeBank.Controllers;


namespace backend.Tests.Unit;


[TestFixture]
[Category("UnitTest")]
public class SearchControllerUnitTests
{
    private Mock<IAzureBlobService> _mockBlobService;
    private Mock<DatabaseContext> _mockDbContext;
    private SearchController _controller;

    [SetUp]
    public void SetUp()
    {
        _mockBlobService = new Mock<IAzureBlobService>();
        _mockDbContext = new Mock<DatabaseContext>(new DbContextOptions<DatabaseContext>());
        _controller = new SearchController(_mockBlobService.Object, _mockDbContext.Object);
    }

    [Test]
    public async Task FullTextSearch_ReturnsBadRequest_WhenPageIndexIsLessThan1()
    {
        // Arrange
        string query = "test";
        int pageIndex = 0;  // Invalid page index
        int pageSize = 20;

        // Act
        IActionResult result = await _controller.FullTextSearch(query, pageIndex, pageSize);

        // Assert
        BadRequestObjectResult? badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task FullTextSearch_ReturnsBadRequest_WhenPageSizeIsLessThan1()
    {
        // Arrange
        string query = "test";
        int pageIndex = 1;
        int pageSize = 0;  // Invalid page size

        // Act
        IActionResult result = await _controller.FullTextSearch(query, pageIndex, pageSize);

        // Assert
        BadRequestObjectResult? badRequestResult = result as BadRequestObjectResult;
        Assert.That(badRequestResult, Is.Not.Null);
        Assert.That(badRequestResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
    }


    // Todo: test that all the files are returned when the query is empty
    // [Test]
    // public async Task FullTextSearch_ReturnsBadRequest_WhenQueryIsEmpty()
    // {
    //     // Arrange
    //     var query = "";  // Invalid query
    //     var pageIndex = 1;
    //     var pageSize = 20;

    //     // Act
    //     var result = await _controller.FullTextSearch(query, pageIndex, pageSize);

    //     // Assert
    //     var badRequestResult = result as BadRequestObjectResult;
    //     Assert.That(badRequestResult, Is.Not.Null);
    //     Assert.That(badRequestResult?.StatusCode, Is.EqualTo((int)HttpStatusCode.BadRequest));
    // }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


