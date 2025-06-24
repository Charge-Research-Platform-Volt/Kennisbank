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
public class PersonsControllerTests : TestBase
{
    private PersonsController _controller;
    private ResourceManager _resourceManager;
    private Guid _existingPersonId;
    private Guid _existingOrganisationId;
    private Guid _existingRegionId;
    private Guid _existingResourceId;


    [SetUp]
    public void SetupController()
    { 
        _resourceManager = new ResourceManager(Context);
        _controller = new PersonsController(_resourceManager);
    }
    
    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        await DatabaseSeeder.SeedTemplate(context);
        
        // Create a test person
        Person testPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Test Person",
            Description = "This is a test person",
            Occupation = "Developer",
            CreationDate = DateTime.UtcNow,
        };

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

        // Create a test organisation
        Organisation testOrganisation = new Organisation
        {
            Id = Guid.NewGuid(),
            Name = "Test Organisation",
            Description = "This is a test organisation",
            Website = "https://testorg.example.com",
            CreationDate = DateTime.UtcNow,
        };

        // Create a test region
        Region testRegion = new Region
        {
            Id = Guid.NewGuid(),
            Name = "Test Region",
        };

        await context.Persons.AddAsync(testPerson);
        await context.Regions.AddAsync(testRegion);
        await context.Organisations.AddAsync(testOrganisation);
        await context.Resources.AddAsync(fileResource);
        await context.SaveChangesAsync();
        
        _existingPersonId = testPerson.Id;
        _existingResourceId = fileResource.Id;
        _existingOrganisationId = testOrganisation.Id;
        _existingRegionId = testRegion.Id;
    }
        
    #region New Tests
    
    [Test]
    [Description("New creates a new person successfully")]
    public async Task New_ValidPerson_CreatesPersonSuccessfully()
    {
        // Arrange
        PersonCreateDto dto = new PersonCreateDto
        {
            Name = "New Test person",
            Description = "This is a new test person",
            Occupation = "Developer",
        };

        // Act
        OkObjectResult? result = await _controller.New(dto) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person created successfully"));
        Assert.That(response.Body, Is.Not.Null);

        // Deserialize the response's data, extract ID
        Guid personId = (Guid)response.Body;
        
        bool exists = await _resourceManager.PersonExistsAsync(personId.ToString());
        Assert.That(exists, Is.True);
    }
    
    [Test]
    [Description("New fails when creating an person with no name")]
    public async Task New_MissingName_ReturnsBadRequest()
    {
        // Arrange
        PersonCreateDto dto = new PersonCreateDto
        {
            Name = "", // Empty name
            Description = "This person has no name",
            Occupation = "Developer",
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
    [Description("Delete removes an person successfully")]
    public async Task Delete_ExistingPerson_DeletesPerson()
    {
        // Arrange
        // Create a new person to delete (so we don't affect other tests)
        PersonCreateDto dto = new PersonCreateDto
        {
            Name = "Person To Delete",
            Description = "This person will be deleted",
            Occupation = "Developer",
        };
        
        OkObjectResult? createResult = await _controller.New(dto) as OkObjectResult;
        Assert.That(createResult, Is.Not.Null);
        ApiResponse? createResponse = createResult.Value as ApiResponse;
        Assert.That(createResponse, Is.Not.Null);
        Assert.That(createResponse.Body, Is.Not.Null);

        // Deserialize the response's data
        Guid personId = (Guid)createResponse.Body;
        string personIdStr = personId.ToString();
        
        // Act
        OkObjectResult? result = await _controller.Delete(personIdStr) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person deleted successfully"));
        
        // Verify person no longer exists
        bool existsInDatabase = await _resourceManager.PersonExistsAsync(personIdStr);
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
    [Description("Delete fails with non-existent person")]
    public async Task Delete_NonExistentPerson_ReturnsNotFound()
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
        Assert.That(response.Message, Is.EqualTo("Person does not exist"));
    }
    
    #endregion
    
    #region Update Tests
    
    [Test]
    [Description("Update modifies person properties successfully")]
    public async Task Update_ValidProperties_UpdatesPersonSuccessfully()
    {
        // Arrange
        string personId = _existingPersonId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Updated Person Name" }
        };
        
        // Act
        OkObjectResult? result = await _controller.Update(personId, updates) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person updated successfully."));
        
        // Verify the name was updated
        Person? updatedPerson = await _resourceManager.GetPersonAsync(personId);
        Assert.That(updatedPerson, Is.Not.Null);
        Assert.That(updatedPerson.Name, Is.EqualTo("Updated Person Name"));
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
    [Description("Update fails with non-existent person")]
    public async Task Update_NonExistentPerson_ReturnsNotFound()
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
        Assert.That(response.Message, Is.EqualTo("The person does not exist"));
    }
    
    [Test]
    [Description("Update fails with empty updates")]
    public async Task Update_EmptyUpdates_ReturnsBadRequest()
    {
        // Arrange
        string personId = _existingPersonId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>();
        
        // Act
        BadRequestObjectResult? result = await _controller.Update(personId, updates) as BadRequestObjectResult;
        
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
        string personId = _existingPersonId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "NonExistentProperty", "Some Value" }
        };
        
        // Act
        BadRequestObjectResult? result = await _controller.Update(personId, updates) as BadRequestObjectResult;
        
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
        string personId = _existingPersonId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Name", "Partially Updated Name" },
            { "NonExistentProperty", "Some Value" }
        };
        
        // Act
        OkObjectResult? result = await _controller.Update(personId, updates) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person updated partially."));
        
        // Verify that valid property was updated
        Person? updatedPerson = await _resourceManager.GetPersonAsync(personId);
        Assert.That(updatedPerson, Is.Not.Null);
        Assert.That(updatedPerson.Name, Is.EqualTo("Partially Updated Name"));
    }
    
    #endregion
    
    #region Exists Tests
    
    [Test]
    [Description("Exists returns true for name of existing person")]
    public async Task Exists_ExistingPersonName_ReturnsTrue()
    {
        // Arrange
        string name = "Test Person"; // Name from the seeded person
        
        // Act
        OkObjectResult? result = await _controller.Exists(name) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person already exists."));
        Assert.That(response.Body, Is.Not.Null);
        
        // Deserialize the response's data, extract ID
        JObject? json = JObject.FromObject(response.Body);
        Guid personId = Guid.Parse(json["id"].ToString());
        
        // Check the response data
        Assert.That(personId.ToString(), Is.EqualTo(_existingPersonId.ToString()));
    }
    
    [Test]
    [Description("Exists returns false for non-existent person name")]
    public async Task Exists_NonExistentPersonName_ReturnsFalse()
    {
        // Arrange
        string nonExistentName = "Non-Existent Person";
        
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
        string personId = json["id"].ToString();
        
        // Check the response data
        Assert.That(personId, Is.Empty);
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
    [Description("Info returns person details for valid ID")]
    public async Task Info_ValidId_ReturnsPersonDetails()
    {
        // Arrange
        string personId = _existingPersonId.ToString();
        
        // Act
        OkObjectResult? result = await _controller.Info(personId, null) as OkObjectResult;
        
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person was found"));
        
        // Verify the returned person
        Person? person = response.Body as Person;
        Assert.That(person, Is.Not.Null);
        Assert.That(person.Id.ToString(), Is.EqualTo(personId));
        Assert.That(person.Name, Is.EqualTo("Test Person"));
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
    [Description("Info returns NotFound for non-existent person")]
    public async Task Info_NonExistentPerson_ReturnsNotFound()
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
        Assert.That(response.Message, Is.EqualTo("The person does not exist"));
    }

    [Test]
    [Description("Info returns person details when requested with properties parameter")]
    public async Task Info_WithPropertiesParameter_ReturnsPersonPartialDetails()
    {
        // Arrange
        string personId = _existingPersonId.ToString();
        string properties = "Id";

        // Act
        OkObjectResult? result = await _controller.Info(personId, properties) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Person was found"));
        Assert.That(response.Body, Is.Not.Null);
    }

    #endregion

    #region List Tests

    [Test]
    [Description("List returns all persons when no paging parameters are provided")]
    public async Task List_NoPagingParameters_ReturnsAllPersons()
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
        
        Person[]? persons = response.Body as Person[];
        Assert.That(persons, Is.Not.Null);
        Assert.That(persons.Length, Is.GreaterThanOrEqualTo(1)); // At least our seeded person
    }
    
    [Test]
    [Description("List returns paged persons when paging parameters are provided")]
    public async Task List_WithPagingParameters_ReturnsPagedPersons()
    {
        // Arrange - Add a few more persons to test paging
        for (int i = 0; i < 5; i++)
        {
            PersonCreateDto dto = new PersonCreateDto
            {
                Name = $"Paged Person {i}",
                Description = $"This is paged person {i}",
                Occupation = $"Developer{i}",
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
        
        Person[]? persons = response.Body as Person[];
        Assert.That(persons, Is.Not.Null);
        Assert.That(persons.Length, Is.LessThanOrEqualTo(pageSize));
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
    [Description("List returns persons with selected properties")]
    public async Task List_WithPropertiesParameter_ReturnsPersonsWithPartialProperties()
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

        object[]? persons = response.Body as object[];
        Assert.That(persons, Is.Not.Null);
    }

    [Test]
    [Description("List returns persons filtered by search query")]
    public async Task List_WithSearchQueryParameter_ReturnsFilteredPersons()
    {
        // Arrange
        string searchQuery = "Test"; // Assuming at least one seeded person has "Test" in their name

        // Act
        OkObjectResult? result = await _controller.List(null, null, null, searchQuery) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Does.Contain("Found"));

        object[]? persons = response.Body as object[];
        Assert.That(persons, Is.Not.Null);
    }

    #endregion

    #region Relation fetches

    [Test]
    [Description("Relations returns OK for valid 'authored-resources' relation without properties")]
    public async Task PersonRelations_ValidDirectResourcesWithoutProperties_ReturnsOk()
    {
        string personID = _existingPersonId.ToString();
        string relation = "authored-resources";

        OkObjectResult? result = await _controller.Relations(relation, personID, null) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns OK for valid 'person-related-persons' relation with projection")]
    public async Task PersonRelations_ValidPersonsWithProperties_ReturnsOk()
    {
        string personID = _existingPersonId.ToString();
        string relation = "person-related-persons";
        string properties = "TargetPersonId as targetid,TargetPerson.Name as targetname,SourcePersonId as sourceid,SourcePerson.Name as sourcename";

        OkObjectResult? result = await _controller.Relations(relation, personID, properties) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns OK for valid 'organisations' relation without properties")]
    public async Task PersonRelations_ValidOrganisationRelationsWithoutProperties_ReturnsOk()
    {
        string personID = _existingPersonId.ToString();
        string relation = "organisations";

        OkObjectResult? result = await _controller.Relations(relation, personID, null) as OkObjectResult;

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
    public async Task PersonRelations_EmptyRelation_ReturnsBadRequest()
    {
        string personID = _existingPersonId.ToString();
        string relation = "";

        BadRequestObjectResult? result = await _controller.Relations(relation, personID, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("Relations returns BadRequest when ID is invalid")]
    public async Task PersonRelations_InvalidId_ReturnsBadRequest()
    {
        string personID = "not-a-guid";
        string relation = "persons";

        BadRequestObjectResult? result = await _controller.Relations(relation, personID, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("Relations returns NotFound for unsupported relation")]
    public async Task PersonRelations_UnsupportedRelation_ReturnsNotFound()
    {
        string personID = _existingPersonId.ToString();
        string relation = "some-unsupported-relation";

        NotFoundObjectResult? result = await _controller.Relations(relation, personID, null) as NotFoundObjectResult;

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
    [Description("AddRelation returns OK for valid 'authored-resources' relation")]
    public async Task AddRelation_ValidAuthoredResources_ReturnsOk()
    {
        string personId = _existingPersonId.ToString();
        string resourceId = _existingResourceId.ToString();
        string relation = "authored-resources";

        OkObjectResult? result = await _controller.AddRelation(personId, relation, resourceId, null) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns OK for valid 'organisations' relation with relationInfo")]
    public async Task AddRelation_ValidOrganisationsWithInfo_ReturnsOk()
    {
        string personId = _existingPersonId.ToString();
        string organisationId = _existingOrganisationId.ToString();
        string relation = "organisations";
        string relationInfo = "member";

        OkObjectResult? result = await _controller.AddRelation(personId, relation, organisationId, relationInfo) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns BadRequest when relation is empty")]
    public async Task AddRelation_EmptyRelation_ReturnsBadRequest()
    {
        string personId = _existingPersonId.ToString();
        string relation = "";
        string targetId = _existingOrganisationId.ToString();

        BadRequestObjectResult? result = await _controller.AddRelation(personId, relation, targetId, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("AddRelation returns BadRequest when ID is invalid")]
    public async Task AddRelation_InvalidId_ReturnsBadRequest()
    {
        string personId = "invalid-guid";
        string relation = "organisations";
        string targetId = _existingOrganisationId.ToString();

        BadRequestObjectResult? result = await _controller.AddRelation(personId, relation, targetId, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("AddRelation returns BadRequest when target ID is invalid")]
    public async Task AddRelation_InvalidTargetId_ReturnsBadRequest()
    {
        string personId = _existingPersonId.ToString();
        string relation = "organisations";
        string targetId = "invalid-guid";

        BadRequestObjectResult? result = await _controller.AddRelation(personId, relation, targetId, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid target ID"));
    }

    [Test]
    [Description("AddRelation returns BadRequest for unsupported relation")]
    public async Task AddRelation_UnsupportedRelation_ReturnsBadRequest()
    {
        string personId = _existingPersonId.ToString();
        string targetId = _existingOrganisationId.ToString();
        string relation = "unsupported-relation";

        BadRequestObjectResult? result = await _controller.AddRelation(personId, relation, targetId, null) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    #endregion

    #region Relation Remove

    [Test]
    [Description("RemoveRelation returns OK for valid 'authored-resources' relation")]
    public async Task RemoveRelation_ValidAuthoredResources_ReturnsOk()
    {
        string personId = _existingPersonId.ToString();
        string resourceId = _existingResourceId.ToString();
        string relation = "authored-resources";

        OkObjectResult? result = await _controller.RemoveRelation(personId, relation, resourceId) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation removed successfully"));
    }

    [Test]
    [Description("RemoveRelation returns OK for valid 'organisations' relation")]
    public async Task RemoveRelation_ValidOrganisations_ReturnsOk()
    {
        string personId = _existingPersonId.ToString();
        string organisationId = _existingOrganisationId.ToString();
        string relation = "organisations";

        OkObjectResult? result = await _controller.RemoveRelation(personId, relation, organisationId) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation removed successfully"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest when relation is empty")]
    public async Task RemoveRelation_EmptyRelation_ReturnsBadRequest()
    {
        string personId = _existingPersonId.ToString();
        string relation = "";
        string targetId = _existingOrganisationId.ToString();

        BadRequestObjectResult? result = await _controller.RemoveRelation(personId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest when ID is invalid")]
    public async Task RemoveRelation_InvalidId_ReturnsBadRequest()
    {
        string personId = "invalid-guid";
        string relation = "organisations";
        string targetId = _existingOrganisationId.ToString();

        BadRequestObjectResult? result = await _controller.RemoveRelation(personId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest when target ID is invalid")]
    public async Task RemoveRelation_InvalidTargetId_ReturnsBadRequest()
    {
        string personId = _existingPersonId.ToString();
        string relation = "organisations";
        string targetId = "invalid-guid";

        BadRequestObjectResult? result = await _controller.RemoveRelation(personId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid target ID"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest for unsupported relation")]
    public async Task RemoveRelation_UnsupportedRelation_ReturnsBadRequest()
    {
        string personId = _existingPersonId.ToString();
        string targetId = _existingOrganisationId.ToString();
        string relation = "unsupported-relation";

        BadRequestObjectResult? result = await _controller.RemoveRelation(personId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    #endregion
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


