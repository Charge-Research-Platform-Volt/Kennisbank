using Microsoft.AspNetCore.Mvc;
using backend.Data;
using Microsoft.AspNetCore.StaticFiles;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using backend.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Bogus;

/// <summary>
/// Controller for all sorts of functionality that 
/// might be of use during the Development stage.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IAzureBlobService blobService;
    private readonly Serilog.ILogger logger;
    private readonly DatabaseContext database;

    public TestController(IAzureBlobService blobService, DatabaseContext databaseContext)
    {
        this.blobService = blobService;
        this.logger = Log.ForContext<TestController>();
        this.database = databaseContext;
    }

    [HttpPost("generate-test-data")]
    [SwaggerOperation(
        Summary = "Generates an amount of random test files.",
        Description = "Generates the test files and uploads them to the Blob Storage and Database."
    )]
    [SwaggerResponse(200, "Files were uploaded successfully", typeof(StorageResponse))]
    [SwaggerResponse(404, "Container does not exist", typeof(StorageResponse))]
    [SwaggerResponse(409, "File already exists", typeof(StorageResponse))]
    [SwaggerResponse(400, "Invalid file", typeof(StorageResponse))]
    [SwaggerResponse(500, "Server error", typeof(StorageResponse))]
    public async Task<IActionResult> GenerateTestData(int numberOfFiles)
    {
        if (numberOfFiles <= 0)
            return BadRequest(new StorageResponse("Number of files should be greater than 0"));

        try
        {
            var faker = new Faker();
            var lorem = new Bogus.DataSets.Lorem(locale: "en");
            var containerName = "text";

            for (int i = 0; i < numberOfFiles; i++)
            {
                string fileName = faker.System.FileName();
                fileName = Path.GetFileNameWithoutExtension(fileName);
                string description = faker.Random.Words(10);
                var fileContent = lorem.Paragraph();
                string extension = ".txt";

                // Convert the content to a MemoryStream
                var tempFileStream = new MemoryStream(Encoding.UTF8.GetBytes(fileContent));

                if (string.IsNullOrEmpty(fileName))
                    return BadRequest(new StorageResponse("No name was provided."));

                if (string.IsNullOrEmpty(description))
                    return BadRequest(new StorageResponse("No description was provided."));

                if (string.IsNullOrEmpty(extension) || !Filetype.Supported(extension))
                    return BadRequest(new { message = "Filetype is not supported." });

                string fileType = Filetype.ConvertExtensionToFiletype(extension);

                Guid id = Guid.NewGuid();
                Dictionary<string, string> metadata = new Dictionary<string, string> { { "extension", extension } };


                // Upload the file to Azure Blob Storage
                var result = await blobService.UploadBlobAsync(containerName, id.ToString(), metadata, tempFileStream, overwrite: true);

                switch (result)
                {
                    case BLOB_STATUSCODE.OK:

                        Resource drive = new()
                        {
                            Id = id,
                            Title = fileName,
                            Description = description,
                            TypeId = new Guid("0cc285a8-0f07-11f0-a0a6-5600051f1387"),
                            LanguageCode = "??",
                            FileType = fileType,
                            PublicationDate = DateTime.UtcNow,
                            CreationDate = DateTime.UtcNow
                        };

                        await database.Resources.AddAsync(drive);
                        await database.SaveResourceChangesAsync();

                        break;

                    case BLOB_STATUSCODE.NOTFOUND:
                        return NotFound(new ContainerResponse("Container could not be found", fileType));

                    case BLOB_STATUSCODE.ALREADYEXISTS:
                        return Conflict(new FileResponse("File already exists and overwrite is disabled.", id.ToString(), fileType));

                    default:
                        return StatusCode(500, new StorageResponse("Error uploading file."));
                }
            }

            return Ok(new { message = $"{numberOfFiles} test files have been generated and uploaded." });

        }
        catch (Exception e)
        {
            logger.Error(e, "Error generating test data.");
            return StatusCode(500, new { message = "An error occurred while generating test data.", error = e.Message });
        }
    }
}
