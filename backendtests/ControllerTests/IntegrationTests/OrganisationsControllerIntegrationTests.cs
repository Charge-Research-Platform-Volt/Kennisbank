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
    private Guid _existingPersonId;
    private Guid _existingResourceId;


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

        // Also seed the blob storage
        Resource fileResource = new Resource
        {
            Id = Guid.NewGuid(),
            Title = "Existing File Resource",
            FileType = "text",
            Hash = "testhash123",
            CreationDate = DateTime.UtcNow,
            PublicationDate = DateTime.UtcNow,
            TypeId = Guid.Parse(DatabaseSeeder.UnknownResourceTypeId),
            LanguageCode = "en"
        };

        // Create a test person
        Person testPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Test Person",
            Description = "This is a test person",
            Occupation = "Developer",
            CreationDate = DateTime.UtcNow,
        };

        await context.Organisations.AddAsync(testOrganisation);
        await context.Resources.AddAsync(fileResource);
        await context.Persons.AddAsync(testPerson);
        await context.SaveChangesAsync();
        
        _existingOrganisationId = testOrganisation.Id;
        _existingResourceId = fileResource.Id;
        _existingPersonId = testPerson.Id;
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
            Website = "https://newtestorg.example.com",
            CreationDate = DateTime.UtcNow,
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
            Website = "https://noname.example.com",
            CreationDate = DateTime.UtcNow,
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

    [Test]
    [Description("Info returns organisation details when requested with properties parameter")]
    public async Task Info_WithPropertiesParameter_ReturnsOrganisationPartialDetails()
    {
        // Arrange
        string organisationId = _existingOrganisationId.ToString();
        string properties = "Id";

        // Act
        OkObjectResult? result = await _controller.Info(organisationId, properties) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Organisation was found"));
        Assert.That(response.Body, Is.Not.Null);
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
                Website = $"https://paged{i}.example.com",
                CreationDate = DateTime.UtcNow,
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

    [Test]
    [Description("List returns organisations with selected properties")]
    public async Task List_WithPropertiesParameter_ReturnsOrganisationsWithPartialProperties()
    {
        // Arrange
        string properties = "Name,Id";

        // Act
        OkObjectResult? result = await _controller.List(null, null, properties, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Does.Contain("Found"));

        object[]? organisations = response.Body as object[];
        Assert.That(organisations, Is.Not.Null);
    }

    [Test]
    [Description("List returns organisations filtered by search query")]
    public async Task List_WithSearchQueryParameter_ReturnsFilteredOrganisations()
    {
        // Arrange
        string searchQuery = "Test"; // Assuming at least one seeded organisation has "Test" in its name

        // Act
        OkObjectResult? result = await _controller.List(null, null, null, searchQuery) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Does.Contain("Found"));

        object[]? organisations = response.Body as object[];
        Assert.That(organisations, Is.Not.Null);
    }

    #endregion

    #region Relation fetches

    [Test]
    [Description("Relations returns OK for valid 'direct-resources' relation without properties")]
    public async Task Relations_ValidDirectResourcesWithoutProperties_ReturnsOk()
    {
        string organisationId = _existingOrganisationId.ToString();
        string relation = "direct-resources";

        OkObjectResult? result = await _controller.Relations(relation, organisationId, null) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns OK for valid 'persons' relation with projection")]
    public async Task Relations_ValidPersonsWithProperties_ReturnsOk()
    {
        string organisationId = _existingOrganisationId.ToString();
        string relation = "persons";
        string properties = "PersonId,Person.Name";

        OkObjectResult? result = await _controller.Relations(relation, organisationId, properties) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns BadRequest when relation is empty")]
    public async Task Relations_EmptyRelation_ReturnsBadRequest()
    {
        string organisationId = _existingOrganisationId.ToString();
        string relation = "";

        BadRequestObjectResult? result = await _controller.Relations(relation, organisationId, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("Relations returns BadRequest when ID is invalid")]
    public async Task Relations_InvalidId_ReturnsBadRequest()
    {
        string organisationId = "invalid-guid";
        string relation = "persons";

        BadRequestObjectResult? result = await _controller.Relations(relation, organisationId, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("Relations returns NotFound for unsupported relation")]
    public async Task Relations_UnsupportedRelation_ReturnsNotFound()
    {
        string organisationId = _existingOrganisationId.ToString();
        string relation = "unsupported-relation";

        NotFoundObjectResult? result = await _controller.Relations(relation, organisationId, null) as NotFoundObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("ID or relation not found"));
    }

    #endregion

    #region Relation Add

    [Test]
    [Description("AddRelation returns OK when adding 'direct-resources' relation successfully")]
    public async Task AddRelation_ValidDirectResources_ReturnsOk()
    {
        string organisationId = _existingOrganisationId.ToString();
        string resourceId = _existingResourceId.ToString();
        string relation = "direct-resources";

        var result = await _controller.AddRelation(organisationId, relation, resourceId, "info") as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
        Assert.That(response.Body, Is.Null);
    }

    [Test]
    [Description("AddRelation returns OK when adding 'persons' relation with null info")]
    public async Task AddRelation_ValidPersonsRelationWithoutInfo_ReturnsOk()
    {
        string organisationId = _existingOrganisationId.ToString();
        string personId = _existingPersonId.ToString();
        string relation = "persons";

        var result = await _controller.AddRelation(organisationId, relation, personId, null) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns BadRequest when relation is empty")]
    public async Task AddRelation_EmptyRelation_ReturnsBadRequest()
    {
        var result = await _controller.AddRelation(_existingOrganisationId.ToString(), "", _existingPersonId.ToString(), null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("AddRelation returns BadRequest when organisation ID is invalid")]
    public async Task AddRelation_InvalidOrganisationId_ReturnsBadRequest()
    {
        var result = await _controller.AddRelation("bad-guid", "persons", _existingPersonId.ToString(), null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("AddRelation returns BadRequest when target ID is invalid")]
    public async Task AddRelation_InvalidTargetId_ReturnsBadRequest()
    {
        var result = await _controller.AddRelation(_existingOrganisationId.ToString(), "persons", "not-a-guid", null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid target ID"));
    }

    [Test]
    [Description("AddRelation returns BadRequest for unsupported relation type")]
    public async Task AddRelation_InvalidRelationType_ReturnsBadRequest()
    {
        var result = await _controller.AddRelation(_existingOrganisationId.ToString(), "aliens", _existingPersonId.ToString(), null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    #endregion

    #region Relation Remove

    [Test]
    [Description("RemoveRelation returns OK when removing 'direct-resources' relation successfully")]
    public async Task RemoveRelation_ValidDirectResource_ReturnsOk()
    {
        var result = await _controller.RemoveRelation(
            _existingOrganisationId.ToString(),
            "direct-resources",
            _existingResourceId.ToString()
        ) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation removed successfully"));
        Assert.That(response.Body, Is.Null);
    }

    [Test]
    [Description("RemoveRelation returns OK when removing 'persons' relation successfully")]
    public async Task RemoveRelation_ValidPersonRelation_ReturnsOk()
    {
        var result = await _controller.RemoveRelation(
            _existingOrganisationId.ToString(),
            "persons",
            _existingPersonId.ToString()
        ) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation removed successfully"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest when relation is empty")]
    public async Task RemoveRelation_EmptyRelation_ReturnsBadRequest()
    {
        var result = await _controller.RemoveRelation(
            _existingOrganisationId.ToString(),
            "",
            _existingResourceId.ToString()
        ) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest when organisation ID is invalid")]
    public async Task RemoveRelation_InvalidOrganisationId_ReturnsBadRequest()
    {
        var result = await _controller.RemoveRelation(
            "invalid-guid",
            "persons",
            _existingPersonId.ToString()
        ) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest when target ID is invalid")]
    public async Task RemoveRelation_InvalidTargetId_ReturnsBadRequest()
    {
        var result = await _controller.RemoveRelation(
            _existingOrganisationId.ToString(),
            "persons",
            "bad-guid"
        ) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid target ID"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest for unsupported relation type")]
    public async Task RemoveRelation_InvalidRelationType_ReturnsBadRequest()
    {
        var result = await _controller.RemoveRelation(
            _existingOrganisationId.ToString(),
            "aliens",
            _existingResourceId.ToString()
        ) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        var response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    #endregion
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


