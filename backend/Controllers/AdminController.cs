using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("admin")]
[Authorize]
public class AdminController(
    IBackgroundTaskQueue taskQueue,
    IServiceScopeFactory serviceScopeFactory,
    EnvironmentConfig environmentConfig) : AppControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<AdminController>();
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    [HttpPost("reembed")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Re-embed all resources, persons, and organisations")]
    [SwaggerResponse(202, "Queued")]
    public IActionResult ReEmbed()
    {
        taskQueue.QueueBackgroundWorkItem(async token =>
        {
            using var scope = serviceScopeFactory.CreateScope();
            var ingestionService = scope.ServiceProvider.GetRequiredService<IngestionService>();
            var resourceService = scope.ServiceProvider.GetRequiredService<ResourceService>();
            var personService = scope.ServiceProvider.GetRequiredService<PersonService>();
            var organisationService = scope.ServiceProvider.GetRequiredService<OrganisationService>();
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();

            var resources = await resourceService.GetAllAsync();
            logger.Information("Re-embedding {Count} resources", resources.Length);
            foreach (var resource in resources)
            {
                try
                {
                    if (resource.FileType == "document" && resource.FileExt != null)
                    {
                        var dlResponse = await storageService.DownloadObjectAsync(bucketName, resource.Id.ToString());
                        using var memStream = new MemoryStream();
                        await dlResponse.Stream.CopyToAsync(memStream, token);
                        memStream.Position = 0;
                        await ingestionService.RunResourcePipelineAsync(resource.Id, resource.FileExt, memStream);
                    }
                    else
                    {
                        await ingestionService.RunResourcePipelineAsync(resource.Id);
                    }
                }
                catch (Exception ex) { logger.Error(ex, "Failed to re-embed resource {Id}", resource.Id); }
            }

            var persons = await personService.GetAllAsync();
            logger.Information("Re-embedding {Count} persons", persons.Length);
            foreach (var person in persons)
            {
                try { await ingestionService.RunPersonEntityPipelineAsync(person.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to re-embed person {Id}", person.Id); }
            }

            var organisations = await organisationService.GetAllAsync();
            logger.Information("Re-embedding {Count} organisations", organisations.Length);
            foreach (var organisation in organisations)
            {
                try { await ingestionService.RunOrganisationEntityPipelineAsync(organisation.Id); }
                catch (Exception ex) { logger.Error(ex, "Failed to re-embed organisation {Id}", organisation.Id); }
            }

            logger.Information("Re-embedding complete");
        });

        return Accepted();
    }
}