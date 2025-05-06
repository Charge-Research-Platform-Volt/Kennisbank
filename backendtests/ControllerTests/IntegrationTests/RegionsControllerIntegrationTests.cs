using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Controllers;
using KnowledgeBank.Data;
using KnowledgeBank.Responses;
using Newtonsoft.Json.Linq;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class RegionsControllerTests : TestBase
{
    private RegionsController _controller;
    private ResourceManager _resourceManager;
    private Guid _existingRegionId;


    [SetUp]
    public void SetupController()
    { 
        _resourceManager = new ResourceManager(Context);
        _controller = new RegionsController(_resourceManager);
    }
    
    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        await DatabaseSeeder.SeedTemplate(context);
        
        // Create a test region
        Region testRegion = new Region
        {
            Id = Guid.NewGuid(),
            Name = "Test Region",
        };
        
        await context.Regions.AddAsync(testRegion);
        await context.SaveChangesAsync();
        
        _existingRegionId = testRegion.Id;
    }
        
    #region New Tests
    
    [Test]
    [Description("New creates a new region successfully")]
    public async Task New_ValidRegion_CreatesRegionSuccessfully()
    {
        // Arrange
        RegionCreateDto dto = new RegionCreateDto
        {
            Name = "New Test region",
        };

        // Act
        OkObjectResult? result = await _controller.New(dto) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Region created successfully"));
        Assert.That(response.Data, Is.Not.Null);
        
        // Deserialize the response's data, extract ID
        JObject? json = JObject.FromObject(response.Data);
        Guid regionId = Guid.Parse(json["id"].ToString());
        
        bool exists = await _resourceManager.RegionExistsAsync(regionId.ToString());
        Assert.That(exists, Is.True);
    }
    
    [Test]
    [Description("New fails when creating an region with no name")]
    public async Task New_MissingName_ReturnsBadRequest()
    {
        // Arrange
        RegionCreateDto dto = new RegionCreateDto
        {
            Name = "", // Empty name
        };

        // Act
        BadRequestObjectResult? result = await _controller.New(dto) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No name was given"));
    }
    
    #endregion
    
    #region Delete Tests
    
    [Test]
    [Description("Delete removes an region successfully")]
    public async Task Delete_ExistingRegion_DeletesRegion()
    {
        // Arrange
        // Create a new region to delete (so we don't affect other tests)
        RegionCreateDto dto = new RegionCreateDto
        {
            Name = "Region To Delete",
        };
        
        OkObjectResult? createResult = await _controller.New(dto) as OkObjectResult;
        Assert.That(createResult, Is.Not.Null);
        ApiResponse? createResponse = createResult.Value as ApiResponse;
        Assert.That(createResponse, Is.Not.Null);
        Assert.That(createResponse.Data, Is.Not.Null);
        
        // Deserialize the response's data
        JObject? json = JObject.FromObject(createResponse.Data);
        Guid regionId = Guid.Parse(json["id"].ToString());
        string regionIdStr = regionId.ToString();
        
        // Act
        OkObjectResult? result = await _controller.Delete(regionIdStr) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Region deleted successfully"));
        
        // Verify region no longer exists
        bool existsInDatabase = await _resourceManager.RegionExistsAsync(regionIdStr);
        Assert.That(existsInDatabase, Is.False);
    }
    
    [Test]
    [Description("Delete fails with invalid ID")]
    public async Task Delete_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";
        
        // Act
        BadRequestObjectResult? result = await _controller.Delete(invalidId) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }
    
    [Test]
    [Description("Delete fails with non-existent region")]
    public async Task Delete_NonExistentRegion_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();
        
        // Act
        NotFoundObjectResult? result = await _controller.Delete(nonExistentId) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Region does not exist"));
    }
    
    #endregion
    
    #region Update Tests
    
    [Test]
    [Description("Update modifies region properties successfully")]
    public async Task Update_ValidProperties_UpdatesRegionSuccessfully()
    {
        // Arrange
        string regionId = _existingRegionId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Updated Region Name" }
        };
        
        // Act
        OkObjectResult? result = await _controller.Update(regionId, updates) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Region updated successfully"));
        
        // Verify the name was updated
        Region? updatedRegion = await _resourceManager.GetRegionAsync(regionId);
        Assert.That(updatedRegion, Is.Not.Null);
        Assert.That(updatedRegion.Name, Is.EqualTo("Updated Region Name"));
    }
    
    [Test]
    [Description("Update fails with invalid ID")]
    public async Task Update_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Updated Name" }
        };
        
        // Act
        BadRequestObjectResult? result = await _controller.Update(invalidId, updates) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID."));
    }
    
    [Test]
    [Description("Update fails with non-existent region")]
    public async Task Update_NonExistentRegion_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Updated Name" }
        };
        
        // Act
        NotFoundObjectResult? result = await _controller.Update(nonExistentId, updates) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("The region does not exist"));
    }
    
    [Test]
    [Description("Update fails with empty updates")]
    public async Task Update_EmptyUpdates_ReturnsBadRequest()
    {
        // Arrange
        string regionId = _existingRegionId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>();
        
        // Act
        BadRequestObjectResult? result = await _controller.Update(regionId, updates) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No updates were provided."));
    }
    
    [Test]
    [Description("Update fails with non-existent properties")]
    public async Task Update_NonExistentProperties_ReturnsBadRequest()
    {
        // Arrange
        string regionId = _existingRegionId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "NonExistentProperty", "Some Value" }
        };
        
        // Act
        BadRequestObjectResult? result = await _controller.Update(regionId, updates) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("None of the props were found"));
    }
    
    [Test]
    [Description("Update partially updates properties")]
    public async Task Update_MixedValidAndInvalidProperties_UpdatesPartially()
    {
        // Arrange
        string regionId = _existingRegionId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Partially Updated Name" },
            { "NonExistentProperty", "Some Value" }
        };
        
        // Act
        OkObjectResult? result = await _controller.Update(regionId, updates) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Region updated partially"));
        
        // Verify that valid property was updated
        Region? updatedRegion = await _resourceManager.GetRegionAsync(regionId);
        Assert.That(updatedRegion, Is.Not.Null);
        Assert.That(updatedRegion.Name, Is.EqualTo("Partially Updated Name"));
    }
    
    #endregion
    
    #region Exists Tests
    
    [Test]
    [Description("Exists returns true for name of existing region")]
    public async Task Exists_ExistingRegionName_ReturnsTrue()
    {
        // Arrange
        string name = "Test Region"; // Name from the seeded region
        
        // Act
        OkObjectResult? result = await _controller.Exists(name) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Region already exists."));
        Assert.That(response.Data, Is.Not.Null);
        
        // Deserialize the response's data, extract ID
        JObject? json = JObject.FromObject(response.Data);
        Guid regionId = Guid.Parse(json["id"].ToString());
        
        // Check the response data
        Assert.That(regionId.ToString(), Is.EqualTo(_existingRegionId.ToString()));
    }
    
    [Test]
    [Description("Exists returns false for non-existent region name")]
    public async Task Exists_NonExistentRegionName_ReturnsFalse()
    {
        // Arrange
        string nonExistentName = "Non-Existent Region";
        
        // Act
        OkObjectResult? result = await _controller.Exists(nonExistentName) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Region does not exist"));
        Assert.That(response.Data, Is.Not.Null);

        // Deserialize the response's data, extract ID
        JObject? json = JObject.FromObject(response.Data);
        string regionId = json["id"].ToString();
        
        // Check the response data
        Assert.That(regionId, Is.Empty);
    }
    
    [Test]
    [Description("Exists returns BadRequest for empty name")]
    public async Task Exists_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        string emptyName = "";
        
        // Act
        BadRequestObjectResult? result = await _controller.Exists(emptyName) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No value given"));
    }
    
    #endregion
    
    #region Info Tests
    
    [Test]
    [Description("Info returns region details for valid ID")]
    public async Task Info_ValidId_ReturnsRegionDetails()
    {
        // Arrange
        string regionId = _existingRegionId.ToString();
        
        // Act
        OkObjectResult? result = await _controller.Info(regionId) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Region was found"));
        
        // Verify the returned region
        Region? region = response.Data as Region;
        Assert.That(region, Is.Not.Null);
        Assert.That(region.Id.ToString(), Is.EqualTo(regionId));
        Assert.That(region.Name, Is.EqualTo("Test Region"));
    }
    
    [Test]
    [Description("Info returns BadRequest for invalid ID")]
    public async Task Info_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";
        
        // Act
        BadRequestObjectResult? result = await _controller.Info(invalidId) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("ID is invalid"));
    }
    
    [Test]
    [Description("Info returns NotFound for non-existent region")]
    public async Task Info_NonExistentRegion_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();
        
        // Act
        NotFoundObjectResult? result = await _controller.Info(nonExistentId) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("The region does not exist"));
    }
    
    #endregion
    
    #region List Tests
    
    [Test]
    [Description("List returns all regions when no paging parameters are provided")]
    public async Task List_NoPagingParameters_ReturnsAllRegions()
    {
        // Arrange - We already have one organization in the database from seed
        
        // Act
        OkObjectResult? result = await _controller.List(null, null) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Does.Contain("Found"));
        
        Region[]? regions = response.Data as Region[];
        Assert.That(regions, Is.Not.Null);
        Assert.That(regions.Length, Is.GreaterThanOrEqualTo(1)); // At least our seeded region
    }
    
    [Test]
    [Description("List returns paged regions when paging parameters are provided")]
    public async Task List_WithPagingParameters_ReturnsPagedRegions()
    {
        // Arrange - Add a few more regions to test paging
        for (int i = 0; i < 5; i++)
        {
            RegionCreateDto dto = new RegionCreateDto
            {
                Name = $"Paged Region {i}",
            };
            await _controller.New(dto);
        }
        
        int pageIndex = 1;
        int pageSize = 3;
        
        // Act
        OkObjectResult? result = await _controller.List(pageIndex, pageSize) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        
        Region[]? regions = response.Data as Region[];
        Assert.That(regions, Is.Not.Null);
        Assert.That(regions.Length, Is.LessThanOrEqualTo(pageSize));
    }
    
    [Test]
    [Description("List returns BadRequest for invalid pageIndex")]
    public async Task List_InvalidPageIndex_ReturnsBadRequest()
    {
        // Arrange
        int invalidPageIndex = 0;
        int pageSize = 10;
        
        // Act
        BadRequestObjectResult? result = await _controller.List(invalidPageIndex, pageSize) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Page index cannot be lower than 1."));
    }
    
    [Test]
    [Description("List returns BadRequest for invalid pageSize")]
    public async Task List_InvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        int pageIndex = 1;
        int invalidPageSize = 0;
        
        // Act
        BadRequestObjectResult? result = await _controller.List(pageIndex, invalidPageSize) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Page size cannot be lower than 1"));
    }
    
    #endregion
}