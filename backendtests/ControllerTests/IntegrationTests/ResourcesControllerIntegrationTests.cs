using Microsoft.AspNetCore.Mvc;
using backend.Tests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Controllers;
using KnowledgeBank.Data;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using System.Text;
using System.Text.Json;
using Azure.Storage.Blobs.Specialized;
using System.Security.Claims;

namespace backend.Tests.Integration;

[TestFixture]
[Category("IntegrationTest")]
public class ResourcesControllerTests : TestBaseBlob
{
    private ResourcesController _controller;
    private ResourceManager _resourceManager;
    private Guid _existingResourceId;
    private Guid _existingFileResourceId;
    private Guid _existingWebsiteResourceId;
    private Guid _existingOrganisationId;
    private Guid _existingRegionId;
    private Guid _existingPersonId;
    private ClaimsPrincipal _regularUser;
    private ClaimsPrincipal _adminUser;
    private Guid _regularUserId;
    private Guid _adminUserId;

    [SetUp]
    public void SetupController()
    {
        _regularUserId = Guid.NewGuid();
        _adminUserId = Guid.NewGuid();

        // Create a regular user
        _regularUser = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, _regularUserId.ToString())],
            "mock"));

        // Create an admin user
        _adminUser = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, _adminUserId.ToString()),
                new Claim(ClaimTypes.Role, "admin")
            ],
            "mock"));

        _resourceManager = new ResourceManager(Context);
        _controller = new ResourcesController(_resourceManager, BlobService);
    }

    protected override async Task SeedTestDatabase(DatabaseContext context)
    {
        // Enable extension for text-search-vectors
        await context.Database.ExecuteSqlRawAsync("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
        await DatabaseSeeder.SeedTemplate(context);

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

        Resource websiteResource = new Resource
        {
            Id = Guid.NewGuid(),
            Title = "Existing Website Resource",
            FileType = "website",
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

        // Create a test person
        Person testPerson = new Person
        {
            Id = Guid.NewGuid(),
            Name = "Test Person",
            Description = "This is a test person",
            Occupation = "Developer",
            CreationDate = DateTime.UtcNow,
        };

        // Create a test region
        Region testRegion = new Region
        {
            Id = Guid.NewGuid(),
            Name = "Test Region",
        };

        await context.Organisations.AddAsync(testOrganisation);
        await context.Resources.AddAsync(fileResource);
        await context.Resources.AddAsync(websiteResource);
        await context.Persons.AddAsync(testPerson);
        await context.Regions.AddAsync(testRegion);

        WebsiteMetadata websiteMetadata = new WebsiteMetadata
        {
            ResourceId = websiteResource.Id,
            Url = "https://example.com"
        };

        await context.WebsiteMetadata.AddAsync(websiteMetadata);
        await context.SaveChangesAsync();

        _existingResourceId = fileResource.Id;
        _existingFileResourceId = fileResource.Id;
        _existingWebsiteResourceId = websiteResource.Id;
        _existingPersonId = testPerson.Id;
        _existingOrganisationId = testOrganisation.Id;
        _existingRegionId = testRegion.Id;

        // Upload a test file to blob storage
        MemoryStream fileStream = new MemoryStream(Encoding.UTF8.GetBytes("This is test content"));
        Dictionary<string, string> metadata = new Dictionary<string, string> { { "extension", ".txt" } };
        await BlobService.UploadBlobAsync("text", _existingFileResourceId.ToString(), metadata, fileStream);
        UploadedBlobs.Add(_existingFileResourceId.ToString());
    }
    
    // Helper method to create a valid ResourceUploadDto for initialization
    private ResourceUploadDto CreateValidResourceUploadDto(string uploadType = "document", string title = "Test Large File")
    {
        DocumentCreateDto dtoDetails = new DocumentCreateDto
        {
            Title = title,
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };

        FormFile formFile = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("dummy file content")), 0, 0, "file", "test.txt");
        
        if (uploadType == "website") 
        {
            WebsiteCreateDto websiteDto = new WebsiteCreateDto
            {
                Title = title,
                TypeId = DatabaseSeeder.UnknownResourceTypeId,
                LanguageCode = "en",
                PublicationDate = DateTime.UtcNow,
                CreationDate = DateTime.UtcNow,
                Url = "https://example.com/large"
            };
                return new ResourceUploadDto
            {
                Dto = JsonSerializer.Serialize(websiteDto),
                UploadType = uploadType
            };
        }

        return new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dtoDetails),
            UploadType = uploadType,
            File = formFile
        };
    }

    // Mocks switching between users. Need this because some endpoints manually check user
    private void SetControllerUser(ClaimsPrincipal user)
    {
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region InitLargeFileUpload Tests

    [Test]
    [Description("InitLargeFileUpload initializes a session successfully for a document")]
    public async Task InitLargeFileUpload_ValidDtoDocument_ReturnsOk()
    {
        // Arrange
        ResourceUploadDto uploadDto = CreateValidResourceUploadDto("document");

        // Act
        ObjectResult? result = await _controller.InitLargeFileUpload(uploadDto) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Upload session initialized"));
        Assert.That(response.Body, Is.InstanceOf<Guid>());
        Guid resourceId = (Guid)response.Body;
        Assert.That(resourceId, Is.Not.EqualTo(Guid.Empty));
    }
    
    [Test]
    [Description("InitLargeFileUpload initializes a session successfully for a website")]
    public async Task InitLargeFileUpload_ValidDtoWebsite_ReturnsOk()
    {
        // Arrange
        ResourceUploadDto uploadDto = CreateValidResourceUploadDto("website");
        uploadDto.File = null;

        // Act
        ObjectResult? result = await _controller.InitLargeFileUpload(uploadDto) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Upload session initialized"));
        Assert.That(response.Body, Is.InstanceOf<Guid>());
        Guid resourceId = (Guid)response.Body;
        Assert.That(resourceId, Is.Not.EqualTo(Guid.Empty));
    }
    
    [Test]
    [Description("InitLargeFileUpload returns BadRequest if file is null for non-website upload")]
    public async Task InitLargeFileUpload_NullFileForNonWebsite_ReturnsBadRequest()
    {
        // Arrange
        DocumentCreateDto dtoDetails = new DocumentCreateDto 
        {
            Title = "Test Title",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };
        ResourceUploadDto uploadDto = new ResourceUploadDto 
        { 
            UploadType = "document", 
            Dto = JsonSerializer.Serialize(dtoDetails),
            File = null
        };

        // Act
        BadRequestObjectResult? result = await _controller.InitLargeFileUpload(uploadDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid DTO sent"));
    }

    [Test]
    [Description("InitLargeFileUpload returns BadRequest when Title is missing")]
    public async Task InitLargeFileUpload_MissingTitle_ReturnsBadRequest()
    {
        // Arrange
        DocumentCreateDto dtoDetails = new DocumentCreateDto 
        { 
            Title = null!, 
            TypeId = "type1", 
            LanguageCode = "en", 
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };
        ResourceUploadDto uploadDto = new ResourceUploadDto 
        {
            Dto = JsonSerializer.Serialize(dtoDetails),
            UploadType = "document",
            File = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("dummy")), 0, 0, "file", "test.txt")
        };
        
        // Act
        BadRequestObjectResult? result = await _controller.InitLargeFileUpload(uploadDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No name was provided."));
    }

    [Test]
    [Description("InitLargeFileUpload returns BadRequest when TypeId is missing")]
    public async Task InitLargeFileUpload_MissingTypeId_ReturnsBadRequest()
    {
        // Arrange
        DocumentCreateDto? dtoDetails = new DocumentCreateDto 
        { 
            Title = "Test Title", 
            TypeId = null!, 
            LanguageCode = "en", 
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };
        ResourceUploadDto uploadDto = new ResourceUploadDto 
        {
            Dto = JsonSerializer.Serialize(dtoDetails),
            UploadType = "document",
            File = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("dummy")), 0, 0, "file", "test.txt")
        };

        // Act
        BadRequestObjectResult? result = await _controller.InitLargeFileUpload(uploadDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No type ID was provided."));
    }


    [Test]
    [Description("InitLargeFileUpload returns BadRequest when LanguageCode is missing")]
    public async Task InitLargeFileUpload_MissingLanguageCode_ReturnsBadRequest()
    {
        // Arrange
        DocumentCreateDto dtoDetails = new DocumentCreateDto 
        { 
            Title = "Test Title", 
            TypeId = "type1", 
            LanguageCode = null!, 
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow
        };
        ResourceUploadDto uploadDto = new ResourceUploadDto 
        {
            Dto = JsonSerializer.Serialize(dtoDetails),
            UploadType = "document",
            File = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("dummy")), 0, 0, "file", "test.txt")
        };
        
        // Act
        BadRequestObjectResult? result = await _controller.InitLargeFileUpload(uploadDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No language code was provided"));
    }

    [Test]
    [Description("InitLargeFileUpload returns BadRequest when PublicationDate is missing")]
    public async Task InitLargeFileUpload_MissingPublicationDate_ReturnsBadRequest()
    {
        // Arrange
        DocumentCreateDto dtoDetails = new DocumentCreateDto 
        { 
            Title = "Test Title", 
            TypeId = "type1", 
            LanguageCode = "en", 
            PublicationDate = DateTime.MinValue,
            CreationDate = DateTime.UtcNow,
        };
        ResourceUploadDto uploadDto = new ResourceUploadDto 
        {
            Dto = JsonSerializer.Serialize(dtoDetails),
            UploadType = "document",
            File = new FormFile(new MemoryStream(Encoding.UTF8.GetBytes("dummy")), 0, 0, "file", "test.txt")
        };

        // Act
        BadRequestObjectResult? result = await _controller.InitLargeFileUpload(uploadDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No publication date was provided"));
    }

    #endregion

    #region UploadChunk Tests

    [Test]
    [Description("UploadChunk uploads a chunk successfully")]
    public async Task UploadChunk_ValidChunk_ReturnsOk()
    {
        // Arrange
        ResourceUploadDto? initDto = CreateValidResourceUploadDto();
        ObjectResult? initResult = await _controller.InitLargeFileUpload(initDto) as ObjectResult;
        Assert.That(initResult, Is.Not.Null);
        ApiResponse? initResponse = initResult.Value as ApiResponse;
        Assert.That(initResponse, Is.Not.Null);
        Assert.That(initResponse.Body, Is.Not.Null);
        Guid resourceId = (Guid)initResponse.Body;

        string fileType = Filetype.ConvertExtensionToFiletype(".txt"); // Assuming a helper or direct value
        string blockId = "block001";
        byte[] chunkData = Encoding.UTF8.GetBytes("This is a file chunk.");
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.Request.Body = new MemoryStream(chunkData);
        _controller.Request.ContentLength = chunkData.Length;


        // Act
        ObjectResult? result = await _controller.UploadChunk(resourceId.ToString(), fileType, blockId) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Chunk uploaded successfully"));
    }

    [Test]
    [Description("UploadChunk returns BadRequest if no chunk data provided")]
    public async Task UploadChunk_NoChunkData_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = Guid.NewGuid().ToString();
        string fileType = "text";
        string blockId = "block001";
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.Request.Body = null!;

        // Act
        BadRequestObjectResult? result = await _controller.UploadChunk(resourceId, fileType, blockId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No chunk data was provided."));
    }
    
    [Test]
    [Description("UploadChunk returns BadRequest for invalid Resource ID")]
    public async Task UploadChunk_InvalidResourceId_ReturnsBadRequest()
    {
        // Arrange
        string invalidResourceId = "invalid-guid";
        string fileType = "text";
        string blockId = "block001";
        byte[] chunkData = Encoding.UTF8.GetBytes("This is a file chunk.");
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.Request.Body = new MemoryStream(chunkData);


        // Act
        BadRequestObjectResult? result = await _controller.UploadChunk(invalidResourceId, fileType, blockId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid resource ID."));
    }

    [Test]
    [Description("UploadChunk returns BadRequest if no FileType provided")]
    public async Task UploadChunk_NoFileType_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = Guid.NewGuid().ToString();
        string blockId = "block001";
        byte[] chunkData = Encoding.UTF8.GetBytes("This is a file chunk.");
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.Request.Body = new MemoryStream(chunkData);

        // Act
        BadRequestObjectResult? result = await _controller.UploadChunk(resourceId, "", blockId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No file type was provided."));
    }

    [Test]
    [Description("UploadChunk returns BadRequest if no Block ID provided")]
    public async Task UploadChunk_NoBlockId_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = Guid.NewGuid().ToString();
        string fileType = "text";
        byte[] chunkData = Encoding.UTF8.GetBytes("This is a file chunk.");
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.Request.Body = new MemoryStream(chunkData);

        // Act
        BadRequestObjectResult? result = await _controller.UploadChunk(resourceId, fileType, "") as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No block ID was provided."));
    }

    #endregion

    #region FinalizeLargeFileUpload Tests

    [Test]
    [Description("FinalizeLargeFileUpload finalizes upload successfully")]
    public async Task FinalizeLargeFileUpload_ValidData_ReturnsOk()
    {
        // Arrange: Initialize and upload a chunk first
        ResourceUploadDto? initDto = CreateValidResourceUploadDto(title: "Finalize Test File");
        ObjectResult? initResult = await _controller.InitLargeFileUpload(initDto) as ObjectResult;
        Assert.That(initResult, Is.Not.Null);
        ApiResponse? initResponse = initResult.Value as ApiResponse;
        Assert.That(initResponse, Is.Not.Null);
        Assert.That(initResponse.Body, Is.Not.Null);
        Guid resourceId = (Guid)initResponse.Body;

        string fileTypeForChunk = Filetype.ConvertExtensionToFiletype(".txt");
        string blockId1 = "finalBlock001";
        string base64BlockId1 = Convert.ToBase64String(Encoding.UTF8.GetBytes(blockId1));
        byte[] chunkData = Encoding.UTF8.GetBytes("Final chunk.");
        
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.Request.Body = new MemoryStream(chunkData);
        _controller.Request.ContentLength = chunkData.Length;
        await _controller.UploadChunk(resourceId.ToString(), fileTypeForChunk, blockId1); 

        LargeFileFinalizeDto finalizeDto = new LargeFileFinalizeDto
        {
            ResourceId = resourceId.ToString(),
            FileType = fileTypeForChunk, 
            FileName = "finalized-test-file.txt",
            BlockIds = new List<string> { base64BlockId1 }
        };

        // Act
        ObjectResult? result = await _controller.FinalizeLargeFileUpload(finalizeDto) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("File upload finalized successfully"));
        Assert.That(response.Body, Is.EqualTo(resourceId));
        
        // Verify blob exists after finalize
        UploadedBlobs.Add(resourceId.ToString()); // Add for cleanup in TestBaseBlob
        BLOB_STATUSCODE blobExists = await BlobService.BlobExistsAsync(fileTypeForChunk, resourceId.ToString());
        Assert.That(blobExists, Is.EqualTo(BLOB_STATUSCODE.OK));
    }

    [Test]
    [Description("FinalizeLargeFileUpload returns BadRequest for invalid Resource ID")]
    public async Task FinalizeLargeFileUpload_InvalidResourceId_ReturnsBadRequest()
    {
        // Arrange
        LargeFileFinalizeDto finalizeDto = new LargeFileFinalizeDto
        {
            ResourceId = "invalid-guid",
            FileType = "text",
            FileName = "test.txt",
            BlockIds = new List<string> { "block1" }
        };

        // Act
        BadRequestObjectResult? result = await _controller.FinalizeLargeFileUpload(finalizeDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid resource ID."));
    }
    
    [Test]
    [Description("FinalizeLargeFileUpload returns BadRequest if no FileType provided")]
    public async Task FinalizeLargeFileUpload_NoFileType_ReturnsBadRequest()
    {
        // Arrange
        LargeFileFinalizeDto finalizeDto = new LargeFileFinalizeDto
        {
            ResourceId = Guid.NewGuid().ToString(),
            FileType = "",
            FileName = "test.txt",
            BlockIds = new List<string> { "block1" }
        };

        // Act
        BadRequestObjectResult? result = await _controller.FinalizeLargeFileUpload(finalizeDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No file type was provided."));
    }

    [Test]
    [Description("FinalizeLargeFileUpload returns BadRequest if no FileName provided")]
    public async Task FinalizeLargeFileUpload_NoFileName_ReturnsBadRequest()
    {
        // Arrange
        LargeFileFinalizeDto finalizeDto = new LargeFileFinalizeDto
        {
            ResourceId = Guid.NewGuid().ToString(),
            FileType = "text",
            FileName = "",
            BlockIds = new List<string> { "block1" }
        };

        // Act
        BadRequestObjectResult? result = await _controller.FinalizeLargeFileUpload(finalizeDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No file name was provided."));
    }

    [Test]
    [Description("FinalizeLargeFileUpload returns BadRequest if no BlockIds provided")]
    public async Task FinalizeLargeFileUpload_NoBlockIds_ReturnsBadRequest()
    {
        // Arrange
        LargeFileFinalizeDto finalizeDto = new LargeFileFinalizeDto
        {
            ResourceId = Guid.NewGuid().ToString(),
            FileType = "text",
            FileName = "test.txt",
            BlockIds = new List<string>() // Empty BlockIds
        };

        // Act
        BadRequestObjectResult? result = await _controller.FinalizeLargeFileUpload(finalizeDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No block IDs were provided."));
    }
    
    [Test]
    [Description("FinalizeLargeFileUpload returns BadRequest if BlockIds is null")]
    public async Task FinalizeLargeFileUpload_NullBlockIds_ReturnsBadRequest()
    {
        // Arrange
        LargeFileFinalizeDto finalizeDto = new LargeFileFinalizeDto
        {
            ResourceId = Guid.NewGuid().ToString(),
            FileType = "text",
            FileName = "test.txt",
            BlockIds = null!
        };

        // Act
        BadRequestObjectResult? result = await _controller.FinalizeLargeFileUpload(finalizeDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No block IDs were provided."));
    }


    #endregion

    #region CleanupFailedUpload Tests

    [Test]
    [Description("CleanupFailedUpload removes database entry for an initialized upload")]
    public async Task CleanupFailedUpload_ExistingInitializedResource_ReturnsOkAndDeletesEntry()
    {
        ResourceUploadDto? initDto = CreateValidResourceUploadDto(title: "Cleanup Test File");
        ObjectResult? initResult = await _controller.InitLargeFileUpload(initDto) as ObjectResult;
        Assert.That(initResult, Is.Not.Null);
        ApiResponse? initResponse = initResult.Value as ApiResponse;
        Assert.That(initResponse, Is.Not.Null);
        Assert.That(initResponse.Success, Is.True);
        Assert.That(initResponse.Body, Is.Not.Null);
        Guid resourceId = (Guid)initResponse.Body;

        string fileType = Filetype.ConvertExtensionToFiletype(".txt"); // Assuming a helper or direct value
        string blockId = "block001";
        byte[] chunkData = Encoding.UTF8.GetBytes("This is a file chunk.");
        _controller.ControllerContext.HttpContext = new DefaultHttpContext();
        _controller.Request.Body = new MemoryStream(chunkData);
        _controller.Request.ContentLength = chunkData.Length;
        await _controller.UploadChunk(resourceId.ToString(), fileType, blockId);

        bool existsBeforeCleanup = await _resourceManager.ResourceExistsAsync(resourceId.ToString());
        Assert.That(existsBeforeCleanup, Is.True, "Resource should exist in DB after Init for cleanup test");
        var blockBlobClient = (await BlobService.GetOrCreateContainerAsync(fileType)).GetBlockBlobClient(resourceId.ToString());
        Assert.That(blockBlobClient.GetBlockList().Value.UncommittedBlocks, Is.Not.Empty, "Blob should have uncommited blocks before cleanup");

        // Act
        ObjectResult? result = await _controller.CleanupFailedUpload(resourceId.ToString()) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Upload cleaned up successfully"));

        // Verify database entry is removed, and that there is no blob or staged blocks for the blob after cleanup
        bool existsAfterCleanup = await _resourceManager.ResourceExistsAsync(resourceId.ToString());
        Assert.That(existsAfterCleanup, Is.False);
        Assert.That(await BlobService.BlobExistsAsync(fileType, resourceId.ToString()), Is.EqualTo(BLOB_STATUSCODE.NOTFOUND), "There should be no blob after cleanup"); // If there is no blob there are no uncommited blocks
    }

    [Test]
    [Description("CleanupFailedUpload returns Ok if resource not found (already cleaned or invalid)")]
    public async Task CleanupFailedUpload_NonExistentResource_ReturnsOk()
    {
        // Arrange
        string nonExistentResourceId = Guid.NewGuid().ToString();

        // Act
        ObjectResult? result = await _controller.CleanupFailedUpload(nonExistentResourceId) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200)); // As per controller logic
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource not found, but cleanup completed"));
    }

    [Test]
    [Description("CleanupFailedUpload returns BadRequest for invalid Resource ID format")]
    public async Task CleanupFailedUpload_InvalidResourceIdFormat_ReturnsBadRequest()
    {
        // Arrange
        string invalidFormatResourceId = "not-a-guid";

        // Act
        BadRequestObjectResult? result = await _controller.CleanupFailedUpload(invalidFormatResourceId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid resource ID format.").Or.EqualTo("Invalid resource ID."));
    }
    
    [Test]
    [Description("CleanupFailedUpload returns BadRequest for empty Resource ID")]
    public async Task CleanupFailedUpload_EmptyResourceId_ReturnsBadRequest()
    {
        // Arrange
        string emptyResourceId = "";

        // Act
        BadRequestObjectResult? result = await _controller.CleanupFailedUpload(emptyResourceId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid resource ID."));
    }

    #endregion
        
    #region New Website Tests

    [Test]
    [Description("New creates a new website resource successfully")]
    public async Task New_ValidWebsiteResource_CreatesResourceSuccessfully()
    {
        // Arrange
        WebsiteCreateDto dto = new WebsiteCreateDto
        {
            Title = "Test Website",
            TypeId = "0cc285a8-0f07-11f0-a0a6-5600051f1387",
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
            Url = "https://www.example.com"
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "website",
        };

        // Act
        ObjectResult? result = await _controller.New(uDto) as ObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource created successfully."));
        Assert.That(response.Body, Is.Not.Null);

        // Verify the resource exists in the database
        Guid resourceId = (Guid)response.Body;
        bool exists = await _resourceManager.ResourceExistsAsync(resourceId.ToString());
        Assert.That(exists, Is.True);
    }

    [Test]
    [Description("New fails when creating a website resource with invalid URL")]
    public async Task New_InvalidWebsiteUrl_ReturnsBadRequest()
    {
        // Arrange
        WebsiteCreateDto dto = new WebsiteCreateDto
        {
            Title = "Invalid Website",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
            Url = "not-a-valid-url"
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "website"
        };

        // Act
        BadRequestObjectResult? result = await _controller.New(uDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("The URL was invalid."));
    }

    [Test]
    [Description("New fails when creating a website resource with missing URL")]
    public async Task New_MissingWebsiteUrl_ReturnsBadRequest()
    {
        // Arrange
        WebsiteCreateDto dto = new WebsiteCreateDto
        {
            Title = "Website Missing URL",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
            Url = ""
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "website"
        };

        // Act
        BadRequestObjectResult? result = await _controller.New(uDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("The URL was empty."));
    }

    [Test]
    [Description("New fails when creating a resource with missing required fields")]
    public async Task New_MissingRequiredFields_ReturnsBadRequest()
    {
        // Arrange - Missing title
        WebsiteCreateDto dto = new WebsiteCreateDto
        {
            Title = "",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
            Url = "https://example.com"
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "website"
        };

        // Act
        BadRequestObjectResult? result = await _controller.New(uDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("No name was provided."));
    }

    #endregion

    #region New File Tests

    [Test]
    [Description("New creates a new file resource successfully")]
    public async Task New_ValidFileResource_CreatesResourceSuccessfully()
    {
        // Arrange
        string fileName = "test-file.txt";
        MemoryStream fileStream = new MemoryStream(Encoding.UTF8.GetBytes("This is a test file"));

        FileResourceCreateDto dto = new FileResourceCreateDto
        {
            Title = "Test File",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "document",
            File = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
        };

        // Act
        OkObjectResult? result = await _controller.New(uDto) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource created successfully."));
        Assert.That(response.Body, Is.Not.Null);

        // Verify the resource exists in the database
        Guid resourceId = (Guid)response.Body;
        bool existsInDatabase = await _resourceManager.ResourceExistsAsync(resourceId.ToString());

        BLOB_STATUSCODE existsInBlob = await BlobService.BlobExistsAsync(Filetype.UploadType.Document, resourceId.ToString());
        UploadedBlobs.Add(resourceId.ToString());

        Assert.That(existsInDatabase, Is.True);
        Assert.That(existsInBlob, Is.EqualTo(BLOB_STATUSCODE.OK));
    }

    [Test]
    [Description("New fails when creating a file resource with no file")]
    public async Task New_NoFile_ReturnsBadRequest()
    {
        // Arrange
        FileResourceCreateDto dto = new FileResourceCreateDto
        {
            Title = "Test File No File",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };

        string sDto = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = true });

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = sDto,
            UploadType = "document",
            File = null
        };

        // Act
        BadRequestObjectResult? result = await _controller.New(uDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid DTO sent"));
    }

    [Test]
    [Description("New fails when creating a file resource with empty file")]
    public async Task New_EmptyFile_ReturnsBadRequest()
    {
        // Arrange
        string fileName = "empty-file.txt";
        MemoryStream fileStream = new MemoryStream(new byte[0]);

        FileResourceCreateDto dto = new FileResourceCreateDto
        {
            Title = "Test Empty File",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "document",
            File = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
        };

        // Act
        BadRequestObjectResult? result = await _controller.New(uDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("The uploaded file was empty."));
    }

    [Test]
    [Description("New fails when creating a file resource with unsupported file type")]
    public async Task New_UnsupportedFileType_ReturnsBadRequest()
    {
        // Arrange
        string fileName = "test-file.nonexistingfiletype";
        MemoryStream fileStream = new MemoryStream(Encoding.UTF8.GetBytes("This is test content"));

        FileResourceCreateDto dto = new FileResourceCreateDto
        {
            Title = "Test Unsupported File",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "document",
            File = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
        };


        // Act
        BadRequestObjectResult? result = await _controller.New(uDto) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Filetype is not supported."));
    }

    #endregion

    #region Download Tests

    [Test]
    [Description("Download returns the file successfully")]
    public async Task Download_ExistingFileResource_ReturnsFile()
    {
        // Arrange
        string resourceId = _existingFileResourceId.ToString();

        // Act
        FileStreamResult? result = await _controller.Download(resourceId) as FileStreamResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        //Assert.That(result.ContentType, Is.EqualTo("application/octet-stream"));
        Assert.That(result.FileDownloadName, Does.EndWith(".txt"));

        // Check file content
        MemoryStream memoryStream = new MemoryStream();
        await result.FileStream.CopyToAsync(memoryStream);
        string content = Encoding.UTF8.GetString(memoryStream.ToArray());
        Assert.That(content, Is.EqualTo("This is test content"));
    }

    [Test]
    [Description("Download fails with invalid ID")]
    public async Task Download_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";

        // Act
        BadRequestObjectResult? result = await _controller.Download(invalidId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID."));
    }

    [Test]
    [Description("Download fails with non-existent resource")]
    public async Task Download_NonExistentResource_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();

        // Act
        NotFoundObjectResult? result = await _controller.Download(nonExistentId) as NotFoundObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Does.Contain("does not exist"));
    }

    [Test]
    [Description("Download fails when trying to download a website resource")]
    public async Task Download_WebsiteResource_ReturnsBadRequest()
    {
        // Arrange
        string websiteResourceId = _existingWebsiteResourceId.ToString();

        // Act
        BadRequestObjectResult? result = await _controller.Download(websiteResourceId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Cannot download website."));
    }

    #endregion

    #region Delete Tests

    [Test]
    [Description("Delete removes a file resource successfully")]
    public async Task Delete_ExistingFileResource_DeletesResource()
    {
        // Arrange
        // Create a file resource to delete (so we don't affect other tests)
        string fileName = "file-to-delete.txt";
        MemoryStream fileStream = new MemoryStream(Encoding.UTF8.GetBytes("This file will be deleted"));

        // Set user to admin
        SetControllerUser(_adminUser);

        FileResourceCreateDto dto = new FileResourceCreateDto
        {
            Title = "Delete Test File",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "document",
            File = new FormFile(fileStream, 0, fileStream.Length, "file", fileName)
        };

        OkObjectResult? createResult = await _controller.New(uDto) as OkObjectResult;
        Assert.That(createResult, Is.Not.Null);
        ApiResponse? createResponse = createResult.Value as ApiResponse;
        Assert.That(createResponse, Is.Not.Null);
        Assert.That(createResponse.Body, Is.Not.Null);
        Guid resourceId = (Guid)createResponse.Body;
        string resourceIdStr = resourceId.ToString();
        UploadedBlobs.Add(resourceIdStr); // To ensure cleanup even if test fails

        // Act
        OkObjectResult? result = await _controller.Delete(resourceIdStr) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource deleted successfully."));

        // Verify resource no longer exists
        bool existsInDatabase = await _resourceManager.ResourceExistsAsync(resourceIdStr);
        BLOB_STATUSCODE existsInBlob = await BlobService.BlobExistsAsync("text", resourceIdStr);

        Assert.That(existsInDatabase, Is.False);
        Assert.That(existsInBlob, Is.EqualTo(BLOB_STATUSCODE.NOTFOUND));
    }

    [Test]
    [Description("Delete fails with invalid ID")]
    public async Task Delete_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";

        // Set user to admin
        SetControllerUser(_adminUser);

        // Act
        BadRequestObjectResult? result = await _controller.Delete(invalidId) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID."));
    }

    [Test]
    [Description("Delete fails with non-existent resource")]
    public async Task Delete_NonExistentResource_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();


        // Set user to admin
        SetControllerUser(_adminUser);

        // Act
        NotFoundObjectResult? result = await _controller.Delete(nonExistentId) as NotFoundObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Does.Contain("does not exist"));
    }

    [Test]
    [Description("Delete removes a website resource successfully")]
    public async Task Delete_ExistingWebsiteResource_DeletesResource()
    {

        // Set user to admin
        SetControllerUser(_adminUser);

        // Arrange
        // Create a website resource to delete
        WebsiteCreateDto dto = new WebsiteCreateDto
        {
            Title = "Website To Delete",
            TypeId = DatabaseSeeder.UnknownResourceTypeId,
            LanguageCode = "en",
            PublicationDate = DateTime.UtcNow,
            CreationDate = DateTime.UtcNow,
            Url = "https://example.org/delete-me"
        };

        ResourceUploadDto uDto = new ResourceUploadDto
        {
            Dto = JsonSerializer.Serialize(dto),
            UploadType = "website"
        };

        OkObjectResult? createResult = await _controller.New(uDto) as OkObjectResult;
        Assert.That(createResult, Is.Not.Null);
        ApiResponse? createResponse = createResult.Value as ApiResponse;
        Assert.That(createResponse, Is.Not.Null);
        Assert.That(createResponse.Body, Is.Not.Null);
        Guid resourceId = (Guid)createResponse.Body;
        string resourceIdStr = resourceId.ToString();

        // Act
        OkObjectResult? result = await _controller.Delete(resourceIdStr) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource deleted successfully."));

        // Verify resource no longer exists
        bool existsInDatabase = await _resourceManager.ResourceExistsAsync(resourceIdStr);
        Assert.That(existsInDatabase, Is.False);
    }

    #endregion

    #region Update Tests

    [Test]
    [Description("Update modifies resource properties successfully")]
    public async Task Update_ValidProperties_UpdatesResourceSuccessfully()
    {
        // Arrange
        string resourceId = _existingFileResourceId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Title", "Updated Title" }
        };

        // Act
        OkObjectResult? result = await _controller.Update(resourceId, updates) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource updated successfully."));

        // Verify the title was updated
        string updatedTitle = await _resourceManager.GetResourcePropertyAsync(resourceId, "Title");
        Assert.That(updatedTitle, Is.EqualTo("Updated Title"));
    }

    [Test]
    [Description("Update fails with invalid ID")]
    public async Task Update_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Title", "Updated Title" }
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
    [Description("Update fails with non-existent resource")]
    public async Task Update_NonExistentResource_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Title", "Updated Title" }
        };

        // Act
        NotFoundObjectResult? result = await _controller.Update(nonExistentId, updates) as NotFoundObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("The resource does not exist"));
    }

    [Test]
    [Description("Update fails with empty updates")]
    public async Task Update_EmptyUpdates_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>();

        // Act
        BadRequestObjectResult? result = await _controller.Update(resourceId, updates) as BadRequestObjectResult;

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
        string resourceId = _existingResourceId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "NonExistentProperty", "Some Value" }
        };

        // Act
        BadRequestObjectResult? result = await _controller.Update(resourceId, updates) as BadRequestObjectResult;

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
        string resourceId = _existingFileResourceId.ToString();
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { "Title", "Partially Updated Title" },
            { "NonExistentProperty", "Some Value" }
        };

        // Act
        OkObjectResult? result = await _controller.Update(resourceId, updates) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource updated partially."));

        // Verify that valid property was updated
        string updatedTitle = await _resourceManager.GetResourcePropertyAsync(resourceId, "Title");
        Assert.That(updatedTitle, Is.EqualTo("Partially Updated Title"));
    }

    #endregion

    #region Exists Tests

    [Test]
    [Description("Exists returns true for hash of existing file resource")]
    public async Task Exists_ExistingFileHash_ReturnsTrue()
    {
        // Arrange
        string hash = "testhash123"; // Hash from the seeded resource

        // Act
        OkObjectResult? result = await _controller.Exists(hash, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource already exists."));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Exists returns true for URL of existing website resource")]
    public async Task Exists_ExistingWebsiteUrl_ReturnsTrue()
    {
        // Arrange
        string url = "https://example.com"; // URL from the seeded resource

        // Act
        OkObjectResult? result = await _controller.Exists(null, url) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource already exists."));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Exists returns false for non-existent hash")]
    public async Task Exists_NonExistentHash_ReturnsFalse()
    {
        // Arrange
        string hash = "nonexistenthash";

        // Act
        OkObjectResult? result = await _controller.Exists(hash, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource does not exist"));
        Assert.That(response.Body, Is.Not.Null);
    }

    #endregion

    #region Info Tests

    [Test]
    [Description("Info returns resource details for valid ID")]
    public async Task Info_ValidId_ReturnsResourceDetails()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();

        // Act
        OkObjectResult? result = await _controller.Info(resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource was found."));

        // Verify the returned resource
        Resource? resource = response.Body as Resource;
        Assert.That(resource, Is.Not.Null);
        Assert.That(resource.Id.ToString(), Is.EqualTo(resourceId));
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
        Assert.That(response.Message, Is.EqualTo("ID is invalid."));
    }

    [Test]
    [Description("Info returns NotFound for non-existent resource")]
    public async Task Info_NonExistentResource_ReturnsNotFound()
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
        Assert.That(response.Message, Is.EqualTo("The resource was not found."));
    }

    [Test]
    [Description("Info returns specified properties when properties parameter is provided")]
    public async Task Info_WithProperties_ReturnsSpecifiedProperties()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string properties = "Id,Title";

        // Act
        OkObjectResult? result = await _controller.Info(resourceId, properties) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Resource was found."));

        // The body should be the projected anonymous object
        Assert.That(response.Body, Is.Not.Null);
    }

    #endregion

    #region List Tests

    [Test]
    [Description("List returns all resources when no paging parameters are provided")]
    public async Task List_NoPagingParameters_ReturnsAllResources()
    {
        // Arrange
        int expectedResourceCount = 2;

        // Act
        OkObjectResult? result = await _controller.List(null, null, null, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo($"Found {expectedResourceCount} resources"));

        Resource[]? resources = response.Body as Resource[];
        Assert.That(resources, Is.Not.Null);
        Assert.That(resources.Length, Is.EqualTo(expectedResourceCount));
    }

    [Test]
    [Description("List returns resources projected with specified properties")]
    public async Task List_WithProperties_ReturnsProjectedResources()
    {
        string properties = "Id,Title";

        OkObjectResult? result = await _controller.List(null, null, properties, null) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);

        // Since projection is dynamic, resource type is anonymous, just check body is an array and has elements
        var resources = response.Body as Array;
        Assert.That(resources, Is.Not.Null);
        Assert.That(resources.Length, Is.GreaterThan(0));
    }

    [Test]
    [Description("List filters resources based on searchQuery")]
    public async Task List_WithSearchQuery_FiltersResources()
    {
        string searchQuery = "Test";

        OkObjectResult? result = await _controller.List(null, null, null, searchQuery) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);

        Resource[]? resources = response.Body as Resource[];
        Assert.That(resources, Is.Not.Null);

        // Check that all returned resources match the search query in Title (simplified check)
        foreach (var resource in resources)
        {
            Assert.That(resource.Title, Does.Contain(searchQuery).IgnoreCase.Or.Contains(searchQuery));
        }
    }

    [Test]
    [Description("List returns paged resources when paging parameters are provided")]
    public async Task List_WithPagingParameters_ReturnsPagedResources()
    {
        // Arrange
        int pageIndex = 1;
        int pageSize = 2;

        // Act
        OkObjectResult? result = await _controller.List(pageIndex, pageSize, null, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);

        Resource[]? resources = response.Body as Resource[];
        Assert.That(resources, Is.Not.Null);
        Assert.That(resources.Length, Is.LessThanOrEqualTo(pageSize));
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
    [Description("List returns Unauthorized when requesting trashed resources without admin role")]
    public async Task List_TrashTrueWithoutAdminRole_ReturnsUnauthorized()
    {
        // Arrange: Set user to non-admin (e.g. _regularUser)
        SetControllerUser(_regularUser);

        // Act
        UnauthorizedObjectResult? result = await _controller.List(null, null, null, null, true) as UnauthorizedObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(401));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("You are not authorized to view trashed resources."));
    }

    [Test]
    [Description("List returns trashed resources when user is admin and trash is true")]
    public async Task List_TrashTrueWithAdminRole_ReturnsTrashedResources()
    {
        // Arrange: Set user to admin
        SetControllerUser(_adminUser);

        // Act
        OkObjectResult? result = await _controller.List(null, null, null, null, true) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));

        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);

        Resource[]? resources = response.Body as Resource[];
        Assert.That(resources, Is.Not.Null);

        // Optionally verify that all resources have Trashed == true if accessible
    }

    #endregion

    #region Relation fetches

    [Test]
    [Description("Relations returns authors for valid ID")]
    public async Task Relations_ValidId_Authors_ReturnsAuthors()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "authors";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns organisations for valid ID")]
    public async Task Relations_ValidId_Organisations_ReturnsOrganisations()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "organisations";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns regions for valid ID")]
    public async Task Relations_ValidId_Regions_ReturnsRegions()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "regions";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns related organisations for valid ID")]
    public async Task Relations_ValidId_RelatedOrganisations_ReturnsRelatedOrganisations()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "related-organisations";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns related persons for valid ID")]
    public async Task Relations_ValidId_RelatedPersons_ReturnsRelatedPersons()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "related-persons";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns sources for valid ID")]
    public async Task Relations_ValidId_Sources_ReturnsSources()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "sources";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns related sources for valid ID")]
    public async Task Relations_ValidId_RelatedSources_ReturnsRelatedSources()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "related-sources";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns tags for valid ID")]
    public async Task Relations_ValidId_Tags_ReturnsTags()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "tags";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns website metadata for valid ID")]
    public async Task Relations_ValidId_Website_ReturnsWebsiteMetadata()
    {
        // Arrange
        string resourceId = _existingWebsiteResourceId.ToString();
        string relation = "website";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns projected authors with properties parameter")]
    public async Task Relations_ValidId_Authors_WithProperties_ReturnsProjectedData()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "authors";
        string properties = "PersonId,Person.Name";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, properties) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns projected organisations with properties parameter")]
    public async Task Relations_ValidId_Organisations_WithProperties_ReturnsProjectedData()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "organisations";
        string properties = "OrganisationId,Organisation.Name";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, properties) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns projected tags with properties parameter")]
    public async Task Relations_ValidId_Tags_WithProperties_ReturnsProjectedData()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "tags";
        string properties = "TagId,Tag.Name";

        // Act
        OkObjectResult? result = await _controller.Relations(relation, resourceId, properties) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Successfully retrieved relations"));
        Assert.That(response.Body, Is.Not.Null);
    }

    [Test]
    [Description("Relations returns BadRequest for empty ID")]
    public async Task Relations_EmptyId_ReturnsBadRequest()
    {
        // Arrange
        string emptyId = "";
        string relation = "authors";

        // Act
        BadRequestObjectResult? result = await _controller.Relations(relation, emptyId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("Relations returns BadRequest for invalid GUID format")]
    public async Task Relations_InvalidGuidFormat_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-valid-guid";
        string relation = "authors";

        // Act
        BadRequestObjectResult? result = await _controller.Relations(relation, invalidId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("Relations returns BadRequest for empty relation")]
    public async Task Relations_EmptyRelation_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string emptyRelation = "";

        // Act
        BadRequestObjectResult? result = await _controller.Relations(emptyRelation, resourceId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("Relations returns BadRequest for null relation")]
    public async Task Relations_NullRelation_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string? nullRelation = null;

        // Act
        BadRequestObjectResult? result = await _controller.Relations(nullRelation, resourceId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("Relations returns NotFound for invalid relation type")]
    public async Task Relations_InvalidRelationType_ReturnsNotFound()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string invalidRelation = "invalid-relation-type";

        // Act
        NotFoundObjectResult? result = await _controller.Relations(invalidRelation, resourceId, null) as NotFoundObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(404));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("ID or relation not found"));
    }

    [Test]
    [Description("Relations returns NotFound for unsupported relation")]
    public async Task Relations_UnsupportedRelation_ReturnsNotFound()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string unsupportedRelation = "unsupported-relation";

        // Act
        NotFoundObjectResult? result = await _controller.Relations(unsupportedRelation, resourceId, null) as NotFoundObjectResult;

        // Assert
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
    [Description("AddRelation returns success for valid author relation")]
    public async Task AddRelation_ValidIds_Authors_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "authors";
        string targetId = _existingPersonId.ToString();

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns success for valid organisation relation with relationInfo")]
    public async Task AddRelation_ValidIds_Organisations_WithRelationInfo_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "organisations";
        string targetId = _existingOrganisationId.ToString();
        string relationInfo = "Primary contributor";

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, relationInfo) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns success for valid region relation")]
    public async Task AddRelation_ValidIds_Regions_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "regions";
        string targetId = _existingRegionId.ToString();

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns success for valid related organisation relation")]
    public async Task AddRelation_ValidIds_RelatedOrganisations_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "related-organisations";
        string targetId = _existingOrganisationId.ToString();
        string relationInfo = "Partner organization";

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, relationInfo) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns success for valid source relation with URL")]
    public async Task AddRelation_ValidIds_Sources_WithUrl_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "sources";
        string targetId = "https%3A%2F%2Fexample.com%2Fsource"; // URL encoded

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns success for valid related source relation")]
    public async Task AddRelation_ValidIds_RelatedSources_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "related-sources";
        string targetId = "https%3A%2F%2Fexample.com%2Frelated-source"; // URL encoded

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation returns BadRequest for empty relation")]
    public async Task AddRelation_EmptyRelation_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "";
        string targetId = _existingPersonId.ToString();

        // Act
        BadRequestObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("AddRelation returns BadRequest for null relation")]
    public async Task AddRelation_NullRelation_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string? relation = null;
        string targetId = _existingPersonId.ToString();

        // Act
        BadRequestObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("AddRelation returns BadRequest for invalid resource ID")]
    public async Task AddRelation_InvalidResourceId_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = "invalid-id";
        string relation = "authors";
        string targetId = _existingPersonId.ToString();

        // Act
        BadRequestObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("AddRelation returns BadRequest for invalid target ID")]
    public async Task AddRelation_InvalidTargetId_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "authors";
        string targetId = "invalid-target-id";

        // Act
        BadRequestObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid target ID/URL"));
    }

    [Test]
    [Description("AddRelation returns BadRequest for invalid relation type")]
    public async Task AddRelation_InvalidRelationType_ReturnsBadRequest()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "invalid-relation";
        string targetId = _existingPersonId.ToString();

        // Act
        BadRequestObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, null) as BadRequestObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }



    [Test]
    [Description("AddRelation handles URL decoding correctly for sources")]
    public async Task AddRelation_Sources_HandlesUrlDecoding()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "sources";
        string encodedUrl = "https%3A%2F%2Fexample.com%2Fpath%3Fparam%3Dvalue";

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, encodedUrl, null) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    [Test]
    [Description("AddRelation handles empty relationInfo parameter")]
    public async Task AddRelation_EmptyRelationInfo_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "organisations";
        string targetId = _existingOrganisationId.ToString();
        string relationInfo = "";

        // Act
        OkObjectResult? result = await _controller.AddRelation(resourceId, relation, targetId, relationInfo) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation added successfully"));
    }

    #endregion

    #region Relation Remove

    [Test]
    [Description("RemoveRelation returns success for valid author relation")]
    public async Task RemoveRelation_ValidIds_Authors_ReturnsSuccess()
    {
        // Arrange
        string resourceId = _existingResourceId.ToString();
        string relation = "authors";
        string targetId = _existingPersonId.ToString();

        // Act
        OkObjectResult? result = await _controller.RemoveRelation(resourceId, relation, targetId) as OkObjectResult;

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation removed successfully"));
    }

    [Test]
    [Description("RemoveRelation returns success for valid organisation relation")]
    public async Task RemoveRelation_ValidIds_Organisations_ReturnsSuccess()
    {
        string resourceId = _existingResourceId.ToString();
        string relation = "organisations";
        string targetId = _existingOrganisationId.ToString();

        OkObjectResult? result = await _controller.RemoveRelation(resourceId, relation, targetId) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation removed successfully"));
    }

    [Test]
    [Description("RemoveRelation handles URL decoding correctly for sources")]
    public async Task RemoveRelation_Sources_HandlesUrlDecoding()
    {
        string resourceId = _existingResourceId.ToString();
        string relation = "sources";
        string encodedUrl = "https%3A%2F%2Fexample.com%2Fsource";

        OkObjectResult? result = await _controller.RemoveRelation(resourceId, relation, encodedUrl) as OkObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(200));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.True);
        Assert.That(response.Message, Is.EqualTo("Relation removed successfully"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest for empty relation")]
    public async Task RemoveRelation_EmptyRelation_ReturnsBadRequest()
    {
        string resourceId = _existingResourceId.ToString();
        string relation = "";
        string targetId = _existingPersonId.ToString();

        BadRequestObjectResult? result = await _controller.RemoveRelation(resourceId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest for null relation")]
    public async Task RemoveRelation_NullRelation_ReturnsBadRequest()
    {
        string resourceId = _existingResourceId.ToString();
        string? relation = null;
        string targetId = _existingPersonId.ToString();

        BadRequestObjectResult? result = await _controller.RemoveRelation(resourceId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest for invalid resource ID")]
    public async Task RemoveRelation_InvalidResourceId_ReturnsBadRequest()
    {
        string resourceId = "invalid-id";
        string relation = "authors";
        string targetId = _existingPersonId.ToString();

        BadRequestObjectResult? result = await _controller.RemoveRelation(resourceId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest for invalid target ID")]
    public async Task RemoveRelation_InvalidTargetId_ReturnsBadRequest()
    {
        string resourceId = _existingResourceId.ToString();
        string relation = "authors";
        string targetId = "invalid-target-id";

        BadRequestObjectResult? result = await _controller.RemoveRelation(resourceId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid target ID/URL"));
    }

    [Test]
    [Description("RemoveRelation returns BadRequest for invalid relation type")]
    public async Task RemoveRelation_InvalidRelationType_ReturnsBadRequest()
    {
        string resourceId = _existingResourceId.ToString();
        string relation = "invalid-relation";
        string targetId = _existingPersonId.ToString();

        BadRequestObjectResult? result = await _controller.RemoveRelation(resourceId, relation, targetId) as BadRequestObjectResult;

        Assert.That(result, Is.Not.Null);
        Assert.That(result.StatusCode, Is.EqualTo(400));
        ApiResponse? response = result.Value as ApiResponse;
        Assert.That(response, Is.Not.Null);
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid relation"));
    }

    #endregion

    #region Trash Tests
    [Test]
    [Description("Trashing fails with invalid ID format")]
    public async Task Trashing_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-guid";

        // Act
        IActionResult result = await _controller.Trash(invalidId);

        // Assert
        BadRequestObjectResult badResult = result as BadRequestObjectResult;
        Assert.That(badResult, Is.Not.Null);
        Assert.That(badResult.StatusCode, Is.EqualTo(400));

        ApiResponse response = badResult.Value as ApiResponse;
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID."));
    }

    [Test]
    [Description("Trashing fails when resource does not exist")]
    public async Task Trashing_NonExistentResource_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();

        // Act
        IActionResult result = await _controller.Trash(nonExistentId);

        // Assert
        NotFoundObjectResult notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));

        ApiResponse response = notFoundResult.Value as ApiResponse;
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Does.Contain("does not exist"));
    }
    
    [Test]
    [Description("Untrashing fails with invalid ID format")]
    public async Task Untrashing_InvalidId_ReturnsBadRequest()
    {
        // Arrange
        string invalidId = "not-a-guid";

        // Act
        IActionResult result = await _controller.Untrash(invalidId);

        // Assert
        BadRequestObjectResult badResult = result as BadRequestObjectResult;
        Assert.That(badResult, Is.Not.Null);
        Assert.That(badResult.StatusCode, Is.EqualTo(400));

        ApiResponse response = badResult.Value as ApiResponse;
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Is.EqualTo("Invalid ID."));
    }

    [Test]
    [Description("Untrashing fails when resource does not exist")]
    public async Task Untrashing_NonExistentResource_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = Guid.NewGuid().ToString();

        // Act
        IActionResult result = await _controller.Untrash(nonExistentId);

        // Assert
        NotFoundObjectResult notFoundResult = result as NotFoundObjectResult;
        Assert.That(notFoundResult, Is.Not.Null);
        Assert.That(notFoundResult.StatusCode, Is.EqualTo(404));

        ApiResponse response = notFoundResult.Value as ApiResponse;
        Assert.That(response.Success, Is.False);
        Assert.That(response.Message, Does.Contain("does not exist"));
    }

    #endregion

    #region Archive grid

    //Add Archive grid tests here

    #endregion

    #region Trash grid

    //Add Trash grid tests here

    #endregion
}