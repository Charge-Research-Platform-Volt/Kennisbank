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
public class OrganisationsControllerTests : TestBase
{
    private OrganisationsController _controller;
    private ResourceManager _resourceManager;
    private Guid _existingOrganisationId;


    [SetUp]
    public void SetupController()
    { 
        _resourceManager = new ResourceManager(Context);
        _controller = new OrganisationsController(_resourceManager);
    }
    
    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        await DatabaseSeeder.SeedTemplate(context);
        
        // Create a test organisation
        Organisation testOrganisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Organisation",
            Description = "This is a test organisation",
            Website = "https://testorg.example.com",
            CreationDate = DateTime.UtcNow,
        };
        
        await context.Organisations.AddAsync(testOrganisation);
        await context.SaveChangesAsync();
        
        _existingOrganisationId = testOrganisation.Id;
    }
        
    #region New Tests
    
    [Test]
    [Description("New creates a new organisation successfully")]
    public async Task New_ValidOrganisation_CreatesOrganisationSuccessfully()
    {
        // Arrange
        OrganisationCreateDto dto = new OrganisationCreateDto
        {
            Name = "New Test Organisation",
            Description = "This is a new test organisation",
            Website = "https://newtestorg.example.com"
        };

        // Act
        OkObjectResult? result = await _controller.New(dto) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Organisation created successfully"));
        Assert.That(response.Body, Is.Not.Null);

        // Deserialize the response's data, extract ID
        Guid organisationId = (Guid)response.Body;
        
        bool exists = await _resourceManager.OrganisationExistsAsync(organisationId.ToString());
        Assert.That(exists, Is.True);
    }
    
    [Test]
    [Description("New fails when creating an organisation with no name")]
    public async Task New_MissingName_ReturnsBadRequest()
    {
        // Arrange
        OrganisationCreateDto dto = new OrganisationCreateDto
        {
            Name = "", // Empty name
            Description = "This organisation has no name",
            Website = "https://noname.example.com"
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
    [Description("Delete removes an organisation successfully")]
    public async Task Delete_ExistingOrganisation_DeletesOrganisation()
    {
        // Arrange
        // Create a new organisation to delete (so we don't affect other tests)
        OrganisationCreateDto dto = new OrganisationCreateDto
        {
            Name = "Organisation To Delete",
            Description = "This organisation will be deleted",
            Website = "https://delete.example.com",
            CreationDate = DateTime.UtcNow,
        };
        
        OkObjectResult? createResult = await _controller.New(dto) as OkObjectResult;
        Assert.That(createResult, Is.Not.Null);
        ApiResponse? createResponse = createResult.Value as ApiResponse;
        Assert.That(createResponse, Is.Not.Null);
        Assert.That(createResponse.Body, Is.Not.Null);

        // Deserialize the response's data
        Guid organisationId = (Guid)createResponse.Body;
        string organisationIdStr = organisationId.ToString();
        
        // Act
        OkObjectResult? result = await _controller.Delete(organisationIdStr) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Organisation deleted successfully"));
        
        // Verify organisation no longer exists
        bool existsInDatabase = await _resourceManager.OrganisationExistsAsync(organisationIdStr);
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
    [Description("Delete fails with non-existent organisation")]
    public async Task Delete_NonExistentOrganisation_ReturnsNotFound()
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
        Assert.That(response.Message, Is.EqualTo("Organisation does not exist"));
    }
    
    #endregion
    
    #region Update Tests
    
    [Test]
    [Description("Update modifies organisation properties successfully")]
    public async Task Update_ValidProperties_UpdatesOrganisationSuccessfully()
    {
        // Arrange
        string organisationId = _existingOrganisationId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Updated Organisation Name" }
        };
        
        // Act
        OkObjectResult? result = await _controller.Update(organisationId, updates) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person updated successfully."));
        
        // Verify the name was updated
        Organisation? updatedOrganisation = await _resourceManager.GetOrganisationAsync(organisationId);
        Assert.That(updatedOrganisation, Is.Not.Null);
        Assert.That(updatedOrganisation.Name, Is.EqualTo("Updated Organisation Name"));
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
    [Description("Update fails with non-existent organisation")]
    public async Task Update_NonExistentOrganisation_ReturnsNotFound()
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
        Assert.That(response.Message, Is.EqualTo("The organisation does not exist"));
    }
    
    [Test]
    [Description("Update fails with empty updates")]
    public async Task Update_EmptyUpdates_ReturnsBadRequest()
    {
        // Arrange
        string organisationId = _existingOrganisationId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>();
        
        // Act
        BadRequestObjectResult? result = await _controller.Update(organisationId, updates) as BadRequestObjectResult;
        
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
        string organisationId = _existingOrganisationId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "NonExistentProperty", "Some Value" }
        };
        
        // Act
        BadRequestObjectResult? result = await _controller.Update(organisationId, updates) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("None of the props were found."));
    }
    
    [Test]
    [Description("Update partially updates properties")]
    public async Task Update_MixedValidAndInvalidProperties_UpdatesPartially()
    {
        // Arrange
        string organisationId = _existingOrganisationId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Partially Updated Name" },
            { "NonExistentProperty", "Some Value" }
        };
        
        // Act
        OkObjectResult? result = await _controller.Update(organisationId, updates) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person updated partially."));
        
        // Verify that valid property was updated
        Organisation? updatedOrganisation = await _resourceManager.GetOrganisationAsync(organisationId);
        Assert.That(updatedOrganisation, Is.Not.Null);
        Assert.That(updatedOrganisation.Name, Is.EqualTo("Partially Updated Name"));
    }
    
    #endregion
    
    #region Exists Tests
    
    [Test]
    [Description("Exists returns true for name of existing organisation")]
    public async Task Exists_ExistingOrganisationName_ReturnsTrue()
    {
        // Arrange
        string name = "Test Organisation"; // Name from the seeded organisation
        
        // Act
        OkObjectResult? result = await _controller.Exists(name) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person already exists"));
        Assert.That(response.Body, Is.Not.Null);
        
        // Deserialize the response's data, extract ID
        JObject? json = JObject.FromObject(response.Body);
        Guid organisationId = Guid.Parse(json["id"].ToString());
        
        // Check the response data
        Assert.That(organisationId.ToString(), Is.EqualTo(_existingOrganisationId.ToString()));
    }
    
    [Test]
    [Description("Exists returns false for non-existent organisation name")]
    public async Task Exists_NonExistentOrganisationName_ReturnsFalse()
    {
        // Arrange
        string nonExistentName = "Non-Existent Organisation";
        
        // Act
        OkObjectResult? result = await _controller.Exists(nonExistentName) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person does not exist"));
        Assert.That(response.Body, Is.Not.Null);

        // Deserialize the response's data, extract ID
        JObject? json = JObject.FromObject(response.Body);
        string organisationId = json["id"].ToString();
        
        // Check the response data
        Assert.That(organisationId, Is.Empty);
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
    [Description("Info returns organisation details for valid ID")]
    public async Task Info_ValidId_ReturnsOrganisationDetails()
    {
        // Arrange
        string organisationId = _existingOrganisationId.ToString();
        
        // Act
        OkObjectResult? result = await _controller.Info(organisationId, null) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Organisation was found"));
        
        // Verify the returned organisation
        Organisation? organisation = response.Body as Organisation;
        Assert.That(organisation, Is.Not.Null);
        Assert.That(organisation.Id.ToString(), Is.EqualTo(organisationId));
        Assert.That(organisation.Name, Is.EqualTo("Test Organisation"));
    }
    
    [Test]
    [Description("Info returns BadRequest for invalid ID")]
    public async Task Info_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";
        
        // Act
        BadRequestObjectResult? result = await _controller.Info(invalidId, null) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("ID is invalid"));
    }
    
    [Test]
    [Description("Info returns NotFound for non-existent organisation")]
    public async Task Info_NonExistentOrganisation_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();
        
        // Act
        NotFoundObjectResult? result = await _controller.Info(nonExistentId, null) as NotFoundObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("The organisation was not found"));
    }
    
    #endregion
    
    #region List Tests
    
    [Test]
    [Description("List returns all organisations when no paging parameters are provided")]
    public async Task List_NoPagingParameters_ReturnsAllOrganisations()
    {
        // Arrange - We already have one organization in the database from seed
        
        // Act
        OkObjectResult? result = await _controller.List(null, null, null, null) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Does.Contain("Found"));
        
        Organisation[]? organisations = response.Body as Organisation[];
        Assert.That(organisations, Is.Not.Null);
        Assert.That(organisations.Length, Is.GreaterThanOrEqualTo(1)); // At least our seeded organisation
    }
    
    [Test]
    [Description("List returns paged organisations when paging parameters are provided")]
    public async Task List_WithPagingParameters_ReturnsPagedOrganisations()
    {
        // Arrange - Add a few more organisations to test paging
        for (int i = 0; i < 5; i++)
        {
            OrganisationCreateDto dto = new OrganisationCreateDto
            {
                Name = $"Paged Organisation {i}",
                Description = $"This is paged organisation {i}",
                Website = $"https://paged{i}.example.com"
            };
            await _controller.New(dto);
        }
        
        int pageIndex = 1;
        int pageSize = 3;
        
        // Act
        OkObjectResult? result = await _controller.List(pageIndex, pageSize, null, null) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        
        Organisation[]? organisations = response.Body as Organisation[];
        Assert.That(organisations, Is.Not.Null);
        Assert.That(organisations.Length, Is.LessThanOrEqualTo(pageSize));
    }
    
    [Test]
    [Description("List returns BadRequest for invalid pageIndex")]
    public async Task List_InvalidPageIndex_ReturnsBadRequest()
    {
        // Arrange
        int invalidPageIndex = 0;
        int pageSize = 10;
        
        // Act
        BadRequestObjectResult? result = await _controller.List(invalidPageIndex, pageSize, null, null) as BadRequestObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Page index cannot be lower than 1"));
    }
    
    [Test]
    [Description("List returns BadRequest for invalid pageSize")]
    public async Task List_InvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        int pageIndex = 1;
        int invalidPageSize = 0;
        
        // Act
        BadRequestObjectResult? result = await _controller.List(pageIndex, invalidPageSize, null, null) as BadRequestObjectResult;
        
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