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


    [SetUp]
    public void SetupController()
    { 
        _resourceManager = new ResourceManager(Context);
        _controller = new PersonsController(_resourceManager);
    }
    
    protected override async Task SeedTestDatabase (DatabaseContext context)
    {
        await DatabaseSeeder.SeedTemplate(context, _resourceManager);
        
        // Create a test person
        Person testPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Test Person",
            Description = "This is a test person",
            Occupation = "Developer"
        };
        
        await context.Persons.AddAsync(testPerson);
        await context.SaveChangesAsync();
        
        _existingPersonId = testPerson.Id;
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
            Occupation = "Developer"
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
        JObject? json = JObject.FromObject(response.Body);
        Guid personId = Guid.Parse(json["id"].ToString());
        
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
            Occupation = "Developer"
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
            Occupation = "Developer"
        };
        
        OkObjectResult? createResult = await _controller.New(dto) as OkObjectResult;
        Assert.That(createResult, Is.Not.Null);
        ApiResponse? createResponse = createResult.Value as ApiResponse;
        Assert.That(createResponse, Is.Not.Null);
        Assert.That(createResponse.Body, Is.Not.Null);
        
        // Deserialize the response's data
        JObject? json = JObject.FromObject(createResponse.Body);
        Guid personId = Guid.Parse(json["id"].ToString());
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
        OkObjectResult? result = await _controller.Info(personId) as OkObjectResult;
        
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
    [Description("Info returns NotFound for non-existent person")]
    public async Task Info_NonExistentPerson_ReturnsNotFound()
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
        Assert.That(response.Message, Is.EqualTo("The person does not exist"));
    }
    
    #endregion
    
    #region List Tests
    
    [Test]
    [Description("List returns all persons when no paging parameters are provided")]
    public async Task List_NoPagingParameters_ReturnsAllPersons()
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
                Occupation = $"Developer{i}"
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